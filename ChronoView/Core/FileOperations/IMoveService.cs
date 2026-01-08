using ChronoView.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Specialized service for moving file groups with business logic (NIR balancing, etc.).
/// </summary>
public interface IMoveService
{
    Task<OperationResult> BatchMoveAsync(
        IEnumerable<FileGroup> groups,
        string destinationPath,
        int totalLimit,
        int nirLimit,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default);
}
