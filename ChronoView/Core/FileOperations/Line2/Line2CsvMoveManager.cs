using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileOperations.Line2;

/// <summary>
/// Manages CSV file moving operations for Line2 BukiKye NIR data.
/// Distinct from Nir2CsvManager which handles CSV writing from API data.
/// </summary>
public class Line2CsvMoveManager : ICsvMoveManager
{
    private readonly ILogger<Line2CsvMoveManager> _logger;
    private readonly IPathBuilder _pathBuilder;

    /// <summary>
    /// Initializes a new instance of the Line2CsvMoveManager.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="pathBuilder">Path builder for determining CSV destination folder.</param>
    public Line2CsvMoveManager(ILogger<Line2CsvMoveManager> logger, IPathBuilder pathBuilder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
    }

    /// <summary>
    /// Moves CSV files associated with file groups to the designated CSV folder.
    /// Collects unique CSV file paths from groups and moves them to "뷰키nir csv파일" folder.
    /// </summary>
    /// <param name="groups">File groups that may have associated CSV files.</param>
    /// <param name="destinationBase">Base destination path for the move operation.</param>
    /// <param name="subject">Subject/sample name for grouping.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>Operation result indicating success or failure with error details.</returns>
    public System.Threading.Tasks.Task<OperationResult> MoveCsvFilesAsync(
        System.Collections.Generic.IEnumerable<FileGroup> groups,
        string destinationBase,
        string subject,
        System.Threading.CancellationToken ct = default)
    {
        return System.Threading.Tasks.Task.Run(() =>
        {
            // Collect unique CSV file paths
            var uniqueCsvPaths = groups
                .Where(g => !string.IsNullOrEmpty(g.CsvFilePath) && System.IO.File.Exists(g.CsvFilePath))
                .Select(g => g.CsvFilePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (uniqueCsvPaths.Count == 0)
            {
                _logger.LogDebug("[Line2CsvMoveManager] No CSV files to move");
                return new OperationResult { Success = true };
            }

            // Build CSV destination folder path using Line2PathBuilder
            var csvDestFolder = _pathBuilder.BuildNirFolderPath(
                destinationBase, subject, new FileGroup { LineNumber = 2 }, PathSchema.MoveSchema);

            // Ensure destination directory exists
            System.IO.Directory.CreateDirectory(csvDestFolder);

            // Move each CSV file
            var movedCount = 0;
            foreach (var csvPath in uniqueCsvPaths)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var destFile = System.IO.Path.Combine(csvDestFolder, System.IO.Path.GetFileName(csvPath));
                    MoveFileAtomic(csvPath, destFile);
                    movedCount++;
                    _logger.LogInformation("[Line2CsvMoveManager] Moved CSV file: {Src} -> {Dest}", csvPath, destFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Line2CsvMoveManager] Failed to move CSV file: {CsvPath}", csvPath);
                    return new OperationResult { Success = false, ErrorMessage = $"CSV 파일 이동 실패: {ex.Message}" };
                }
            }

            _logger.LogInformation("[Line2CsvMoveManager] Moved {Count} CSV files to {DestFolder}", movedCount, csvDestFolder);
            return new OperationResult { Success = true };
        }, ct);
    }

    /// <summary>
    /// Moves a file atomically by copying then deleting the source.
    /// </summary>
    /// <param name="srcPath">Source file path.</param>
    /// <param name="destPath">Destination file path.</param>
    private static void MoveFileAtomic(string srcPath, string destPath)
    {
        System.IO.File.Copy(srcPath, destPath, true);
        System.IO.File.Delete(srcPath);
    }
}
