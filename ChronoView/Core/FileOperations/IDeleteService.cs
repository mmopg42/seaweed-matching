using ChronoView.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Specialized service for deleting file groups or components with quarantine logic.
/// </summary>
public interface IDeleteService
{
    Task<OperationResult> DeleteGroupAsync(
        FileGroup group,
        string quarantinePath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default);

    Task<OperationResult> DeleteComponentsAsync(
        FileGroup group,
        IEnumerable<string> components,
        string quarantinePath,
        string? subject = null,
        CancellationToken ct = default);
}
