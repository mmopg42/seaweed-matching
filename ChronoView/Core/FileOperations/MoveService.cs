using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        var candidates = totalLimit > 0 ? allSorted.Take(totalLimit).ToList() : allSorted;
        
        int currentNirInBatch = 0;
        var result = new OperationResult { Success = true };
        int processedCount = 0;

        foreach (var group in candidates)
        {
            ct.ThrowIfCancellationRequested();

            bool shouldKeepNir = true;
            if (group.HasNir)
            {
                if (nirLimit > 0 && currentNirInBatch >= nirLimit)
                {
                    shouldKeepNir = false;
                    _logger.LogInformation("NIR Limit reached ({Limit}). Stripping NIR from {GroupId}", nirLimit, group.GroupId);
                }
                else
                {
                    currentNirInBatch++;
                }
            }

            OperationResult opRes;
            if (!shouldKeepNir && group.HasNir)
            {
                var components = new List<string> { "Normal", "Cam1", "Cam2", "Cam3", "Cam4", "Cam5", "Cam6" };
                opRes = await _operator.DeleteComponentsAsync(group, components, destinationPath, subject, ct);
            }
            else
            {
                opRes = await _operator.ExecuteOpAsync(group, destinationPath, OpType.Move, PathSchema.MoveSchema, subject, null, ct);
            }

            if (!opRes.Success) { result.Success = false; result.ErrorMessage += opRes.ErrorMessage + "; "; }
            
            processedCount++;
            progress?.Report(new OperationProgress { 
                ProcessedFiles = processedCount, 
                TotalFiles = candidates.Count,
                Status = $"Moving {group.GroupId}..." 
            });
        }

        return result;
    }
}
