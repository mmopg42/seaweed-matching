using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

public class MoveService : IMoveService
{
    private readonly IFileGroupOperator _operator;
    private readonly ILogger<MoveService> _logger;

    public MoveService(IFileGroupOperator op, ILogger<MoveService> logger)
    {
        _operator = op; _logger = logger;
    }

    public async Task<OperationResult> BatchMoveAsync(
        IEnumerable<FileGroup> groups,
        string destinationPath,
        int totalLimit,
        int nirLimit,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default)
    {
        var allSorted = groups.OrderBy(g => g.CreatedAt).ToList();

        // 1단계: 사전 NIR 초과분 삭제 (전체 데이터에서)
        if (nirLimit > 0)
        {
            var nirGroups = allSorted.Where(g => g.HasNir).ToList();
            var excessNirCount = nirGroups.Count - nirLimit;

            if (excessNirCount > 0)
            {
                // CreatedAt 오름차순 정렬된 상태에서 TakeLast() = 최신 NIR 선택
                var excessNirGroups = nirGroups.TakeLast(excessNirCount).ToList();

                // 사용자 명시적 경고 로그
                _logger.LogWarning("NIR 제한({Limit}개) 초과분 {Count}개를 격리 폴더로 이동합니다", nirLimit, excessNirCount);

                foreach (var group in excessNirGroups)
                {
                    ct.ThrowIfCancellationRequested();

                    // NIR만 삭제 (Normal/Cam은 그대로 유지)
                    // DeleteComponentsAsync는 destinationPath를 quarantinePath로 사용하여 격리 폴더로 이동
                    var nirOnly = new List<string> { "Nir" };
                    await _operator.DeleteComponentsAsync(group, nirOnly, destinationPath, subject, ct);
                    _logger.LogInformation("NIR 격리 완료: {GroupId}", group.GroupId);
                }
            }
        }

        // 2단계: totalLimit 적용하여 candidates 선택
        var candidates = totalLimit > 0 ? allSorted.Take(totalLimit).ToList() : allSorted;

        // 3단계: 메인 이동 루프
        var result = new OperationResult { Success = true };
        int processedCount = 0;
        int normalCount = 0;
        int camCount = 0;

        foreach (var group in candidates)
        {
            ct.ThrowIfCancellationRequested();

            // Normal/Cam 카운트
            if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder))
                normalCount++;
            if (group.CameraFiles.Any(kv => !string.IsNullOrEmpty(kv.Value) && File.Exists(kv.Value)))
                camCount++;

            // 전체 그룹 이동
            var opRes = await _operator.ExecuteOpAsync(group, destinationPath, OpType.Move, PathSchema.MoveSchema, subject, null, ct);

            if (!opRes.Success) { result.Success = false; result.ErrorMessage += opRes.ErrorMessage + "; "; }

            processedCount++;

            // UI 로그
            string status;
            if (processedCount % 10 == 0 || processedCount == candidates.Count)
            {
                status = $"[{processedCount}/{candidates.Count}] 진행 중... (Normal: {normalCount}, Cam: {camCount})";
            }
            else
            {
                status = $"[{processedCount}/{candidates.Count}] {group.GroupId}...";
            }

            progress?.Report(new OperationProgress {
                ProcessedFiles = processedCount,
                TotalFiles = candidates.Count,
                Status = status
            });
        }

        return result;
    }
}
