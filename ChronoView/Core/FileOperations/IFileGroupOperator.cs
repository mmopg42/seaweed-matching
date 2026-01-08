using ChronoView.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Operational engine for structural file group operations (Move/Quarantine).
/// </summary>
public interface IFileGroupOperator
{
    Task<OperationResult> ExecuteOpAsync(
        FileGroup group,
        string targetBase,
        OpType opType,
        PathSchema schema,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes specific components of a file group.
    /// </summary>
    Task<OperationResult> DeleteComponentsAsync(
        FileGroup group,
        IEnumerable<string> components,
        string quarantinePath,
        string? subject = null,
        CancellationToken ct = default);
}

public enum OpType { Move, Delete }
public enum PathSchema { MoveSchema, QuarantineSchema }
