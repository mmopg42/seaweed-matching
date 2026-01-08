using System;
using System.IO;
using System.Linq;
using ChronoView.Core.Configuration;
using ChronoView.Models;
using Microsoft.Extensions.Logging;

namespace ChronoView.Core.Logging;

/// <summary>
/// 로그 파일 자동 삭제 서비스
/// </summary>
public class LogCleanupService
{
    private readonly IConfigurationManager _configurationManager;
    private readonly ILogger<LogCleanupService> _logger;

    public LogCleanupService(IConfigurationManager configurationManager, ILogger<LogCleanupService> logger)
    {
        _configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 로그 디렉토리 경로를 반환합니다. (중복 로직 제거)
    /// </summary>
    private static string GetLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ChronoView",
            "Logs");
    }

    /// <summary>
    /// 오래된 로그 파일을 삭제합니다.
    /// </summary>
    public void CleanupOldLogFiles()
    {
        try
        {
            var config = _configurationManager.LoadConfiguration<ApplicationConfiguration>();
            int retentionDays = config.WorkflowSettings?.LogRetentionDays ?? 30;

            if (retentionDays <= 0)
            {
                _logger.LogInformation("Log cleanup is disabled (retentionDays <= 0)");
                return;
            }

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var logDir = GetLogDirectory(); // 헬퍼 메서드 사용

            if (!Directory.Exists(logDir))
                return;

            // 날짜 폴더 검색
            var dateFolders = Directory.GetDirectories(logDir);
            int deletedCount = 0;
            int deletedFolderCount = 0;

            foreach (var dateFolderPath in dateFolders)
            {
                try
                {
                    var folderInfo = new DirectoryInfo(dateFolderPath);
                    
                    // 날짜 폴더의 LastWriteTime을 기준으로 삭제 여부 결정
                    if (folderInfo.LastWriteTime < cutoffDate)
                    {
                        // 폴더 내의 모든 파일 삭제
                        var allFiles = Directory.GetFiles(dateFolderPath);
                        foreach (var filePath in allFiles)
                        {
                            try
                            {
                                File.Delete(filePath);
                                deletedCount++;
                                _logger.LogInformation("Deleted old log file: {FilePath} (Folder LastWriteTime: {LastWriteTime})", 
                                    filePath, folderInfo.LastWriteTime);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete file: {FilePath}", filePath);
                            }
                        }
                        
                        // 폴더가 비어있으면 폴더 자체도 삭제
                        try
                        {
                            if (Directory.GetFiles(dateFolderPath).Length == 0 && 
                                Directory.GetDirectories(dateFolderPath).Length == 0)
                            {
                                Directory.Delete(dateFolderPath);
                                deletedFolderCount++;
                                _logger.LogInformation("Deleted empty date folder: {FolderPath}", dateFolderPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete date folder: {FolderPath}", dateFolderPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing date folder: {FolderPath}", dateFolderPath);
                }
            }

            if (deletedCount > 0 || deletedFolderCount > 0)
            {
                _logger.LogInformation("Log cleanup completed: {DeletedCount} files deleted, {DeletedFolderCount} folders deleted (retention: {RetentionDays} days)", 
                    deletedCount, deletedFolderCount, retentionDays);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during log cleanup");
        }
    }
}

