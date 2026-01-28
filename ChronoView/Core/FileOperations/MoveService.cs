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
    private readonly ApplicationConfiguration _config;
    private readonly ILogger<MoveService> _logger;

    public MoveService(IFileGroupOperator op, ApplicationConfiguration config, ILogger<MoveService> logger)
    {
        _operator = op;
        _config = config;
        _logger = logger;
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

        // 1단계: totalLimit 적용하여 이동 대상 candidates 먼저 선택
        var candidates = totalLimit > 0 ? allSorted.Take(totalLimit).ToList() : allSorted;

        // 2단계: candidates 내에서 NIR 초과분 격리 (선택된 그룹들 중에서만 계산)
        if (nirLimit > 0)
        {
            var nirGroups = candidates.Where(g => g.HasNir).ToList();
            var excessNirCount = nirGroups.Count - nirLimit;

            if (excessNirCount > 0)
            {
                // CreatedAt 오름차순 정렬된 상태에서 TakeLast() = 최신 NIR 선택
                var excessNirGroups = nirGroups.TakeLast(excessNirCount).ToList();

                // 격리 폴더 경로 가져오기
                var quarantinePath = _config.WorkflowSettings.DeleteQuarantinePath;
                if (string.IsNullOrEmpty(quarantinePath))
                {
                    // 설정이 없으면 기본 경로 사용
                    quarantinePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "ChronoView", "Quarantine");
                }

                // 사용자 명시적 경고 로그
                _logger.LogWarning("NIR 제한({Limit}개) 초과분 {Count}개를 격리 폴더로 이동합니다: {QuarantinePath}",
                    nirLimit, excessNirCount, quarantinePath);

                foreach (var group in excessNirGroups)
                {
                    ct.ThrowIfCancellationRequested();

                    // NIR만 격리 폴더로 이동 (Normal/Cam은 그대로 유지)
                    var nirOnly = new List<string> { "Nir" };
                    await _operator.DeleteComponentsAsync(group, nirOnly, quarantinePath, subject, ct);
                    _logger.LogInformation("NIR 격리 완료: {GroupId} -> {QuarantinePath}", group.GroupId, quarantinePath);
                }
            }
        }

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
