using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChronoView.Core.NIR.Shared;
using ChronoView.Helpers;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.FileWatching
{
    public interface IInitialScanner
    {
        Task<List<(string FilePath, DataType DataType, DateTime Timestamp)>> ScanAndSortFilesAsync(
            ApplicationConfiguration config, 
            CancellationToken cancellationToken);
    }

    public class InitialScanner : IInitialScanner
    {
        private readonly ILogger<InitialScanner> _logger;
        private readonly INirFileResolver _nirFileResolver;

        public InitialScanner(ILogger<InitialScanner> logger, INirFileResolver nirFileResolver)
        {
            _logger = logger;
            _nirFileResolver = nirFileResolver;
        }

        public async Task<List<(string FilePath, DataType DataType, DateTime Timestamp)>> ScanAndSortFilesAsync(
            ApplicationConfiguration config,
            CancellationToken cancellationToken)
        {
            var orderedTypes = config?.DataSequenceSettings?.GetAllOrderedTypes();
            if (orderedTypes == null || orderedTypes.Count == 0)
            {
                _logger.LogWarning("No ordered types configured in DataSequenceSettings");
                return new List<(string FilePath, DataType DataType, DateTime Timestamp)>();
            }

            _logger.LogInformation("Scanning {Count} data types", orderedTypes.Count);

            var allFilesWithTimestamps = new List<(string FilePath, DataType DataType, DateTime Timestamp)>();

            foreach (var dataType in orderedTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var files = await ScanFilesForDataTypeAsync(config!.MatchingSettings!, dataType, cancellationToken);
                _logger.LogInformation("Found {Count} {DataType} files", files.Count, dataType);

                foreach (var filePath in files)
                {
                    try
                    {
                        var fileTypeString = DataTypeToFileTypeString(dataType);
                        var timestamp = FileNamingHelper.ExtractTimestamp(filePath, fileTypeString);

                        if (timestamp.HasValue && timestamp.Value != DateTime.MinValue)
                        {
                            allFilesWithTimestamps.Add((filePath, dataType, timestamp.Value));
                        }
                        else
                        {
                            _logger.LogWarning("Could not extract timestamp from {DataType} file: {Path}", dataType, filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract timestamp from {DataType} file: {Path}", dataType, filePath);
                    }
                }
            }

            _logger.LogInformation("Total files with valid timestamps: {Count}", allFilesWithTimestamps.Count);
            
            // Sort by timestamp (chronological order)
            return allFilesWithTimestamps.OrderBy(f => f.Timestamp).ToList();
        }

        private async Task<List<string>> ScanFilesForDataTypeAsync(MatchingSettings config, DataType dataType, CancellationToken cancellationToken)
        {
            var files = new List<string>();

            if (config == null)
            {
                _logger.LogWarning("MatchingSettings configuration is null");
                return files;
            }

            await Task.Run(() =>
            {
                switch (dataType)
                {
                    case DataType.NIR:
                        if (!string.IsNullOrEmpty(config.Nir1Path) && Directory.Exists(config.Nir1Path))
                        {
                            var scanPattern = _nirFileResolver.GetScanPattern();
                            var nirFiles = Directory.GetFiles(config.Nir1Path, scanPattern, SearchOption.AllDirectories);
                            files.AddRange(nirFiles);
                        }
                        if (!string.IsNullOrEmpty(config.Nir2Path) && Directory.Exists(config.Nir2Path))
                        {
                            var scanPattern = _nirFileResolver.GetScanPattern();
                            var nirFiles = Directory.GetFiles(config.Nir2Path, scanPattern, SearchOption.AllDirectories);
                            files.AddRange(nirFiles);
                        }
                        break;

                    case DataType.Normal:
                        // Line 1: only _0 suffix when UseFolderSuffix=true
                        if (!string.IsNullOrEmpty(config.Normal1Path) && Directory.Exists(config.Normal1Path))
                        {
                            var folders = Directory.GetDirectories(config.Normal1Path);
                            foreach (var folder in folders)
                            {
                                var folderName = Path.GetFileName(folder);
                                // When suffix mode is enabled, skip _1 folders in Line 1
                                if (config.UseFolderSuffix && folderName.EndsWith("_1"))
                                    continue;
                                if (NormalFolderHelper.IsValidNormalFolder(folderName, config.UseFolderSuffix, expectedLine: 1))
                                {
                                    files.Add(folder);
                                }
                            }
                        }
                        // Line 2: only _1 suffix when UseFolderSuffix=true
                        if (!string.IsNullOrEmpty(config.Normal2Path) && Directory.Exists(config.Normal2Path))
                        {
                            var folders = Directory.GetDirectories(config.Normal2Path);
                            foreach (var folder in folders)
                            {
                                var folderName = Path.GetFileName(folder);
                                // When suffix mode is enabled, skip _0 folders in Line 2
                                if (config.UseFolderSuffix && folderName.EndsWith("_0"))
                                    continue;
                                if (NormalFolderHelper.IsValidNormalFolder(folderName, config.UseFolderSuffix, expectedLine: 2))
                                {
                                    files.Add(folder);
                                }
                            }
                        }
                        break;

                    case DataType.Cam1:
                        AddCameraFilesForScan(files, config.Camera1Path);
                        AddCameraFilesForScan(files, config.Camera4Path);  // Line 2
                        break;
                    case DataType.Cam2:
                        AddCameraFilesForScan(files, config.Camera2Path);
                        AddCameraFilesForScan(files, config.Camera5Path);  // Line 2
                        break;
                    case DataType.Cam3:
                        AddCameraFilesForScan(files, config.Camera3Path);
                        AddCameraFilesForScan(files, config.Camera6Path);  // Line 2
                        break;
                    case DataType.Cam4: AddCameraFilesForScan(files, config.Camera4Path); break;
                    case DataType.Cam5: AddCameraFilesForScan(files, config.Camera5Path); break;
                    case DataType.Cam6: AddCameraFilesForScan(files, config.Camera6Path); break;
                }
            }, cancellationToken);

            return files;
        }

        private void AddCameraFilesForScan(List<string> files, string? cameraPath)
        {
            if (string.IsNullOrEmpty(cameraPath) || !Directory.Exists(cameraPath))
                return;

            var cameraFiles = Directory.GetFiles(cameraPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in cameraFiles)
            {
                var fileName = Path.GetFileName(file);
                if (FileNamingHelper.ExtractTimestampFromCameraFileName(fileName).HasValue)
                {
                    files.Add(file);
                }
            }
        }

        private string DataTypeToFileTypeString(DataType dataType)
        {
            return dataType switch
            {
                DataType.NIR => "Nir",
                DataType.Normal => "Normal",
                _ => "Camera"
            };
        }
    }
}
