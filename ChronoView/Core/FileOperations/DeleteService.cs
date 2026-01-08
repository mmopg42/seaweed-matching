using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

public class DeleteService : IDeleteService
{
    private readonly IFileGroupOperator _operator;
    private readonly ILogger<DeleteService> _logger;

    public DeleteService(IFileGroupOperator op, ILogger<DeleteService> logger)
    {
        _operator = op ?? throw new ArgumentNullException(nameof(op));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperationResult> DeleteGroupAsync(
        FileGroup group,
        string quarantinePath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default)
    {
        return await _operator.ExecuteOpAsync(group, quarantinePath, OpType.Delete, PathSchema.QuarantineSchema, subject, progress, ct);
    }

    public async Task<OperationResult> DeleteComponentsAsync(
        FileGroup group,
        IEnumerable<string> components,
        string quarantinePath,
        string? subject = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting partial components for group {GroupId}", group.GroupId);
        return await _operator.DeleteComponentsAsync(group, components, quarantinePath, subject, ct);
    }
}
