using ChronoView.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Core.FileOperations;

/// <summary>
/// Specialized coordinator service for file operations.
/// Delegating core logic to sub-services for maintainability.
/// </summary>
public class FileOperationService : IFileOperationService
{
    private readonly IMoveService _moveService;
    private readonly IDeleteService _deleteService;
    private readonly ILogger<FileOperationService> _logger;

    public FileOperationService(
        IMoveService moveService,
        IDeleteService deleteService,
        ILogger<FileOperationService> logger)
    {
        _moveService = moveService ?? throw new ArgumentNullException(nameof(moveService));
        _deleteService = deleteService ?? throw new ArgumentNullException(nameof(deleteService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<OperationResult> MoveFileGroupAsync(
        FileGroup group,
        string destinationPath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default)
    {
        // IMoveService.BatchMoveAsync only takes int limits, no onConflict currently.
        // We pass 1 as totalLimit since we are moving a single group.
        return await _moveService.BatchMoveAsync(
            new[] { group },
            destinationPath,
            1, // totalLimit
            1, // nirLimit
            subject,
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DeleteFileGroupAsync(
        FileGroup group,
        string quarantinePath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default)
    {
        // IDeleteService.DeleteGroupAsync signature check
        return await _deleteService.DeleteGroupAsync(
            group,
            quarantinePath,
            subject,
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<OperationResult> DeleteComponentsAsync(
        FileGroup group,
        List<string> componentsToDelete,
        string quarantinePath,
        string? subject = null,
        IProgress<OperationProgress>? progress = null,
        Func<string, ConflictResolution>? onConflict = null,
        CancellationToken cancellationToken = default)
    {
        // IDeleteService.DeleteComponentsAsync signature check
        return await _deleteService.DeleteComponentsAsync(
            group,
            componentsToDelete,
            quarantinePath,
            subject,
            cancellationToken);
    }
}
