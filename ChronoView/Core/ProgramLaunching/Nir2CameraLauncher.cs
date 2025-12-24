using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ChronoView.Models;
using ChronoView.Core.Nir;

namespace ChronoView.Core.ProgramLaunching;

/// <summary>
/// NIR2 필터링 서비스: NIR spectrum 파일을 모니터링하고 필터링하여 조건에 따라 이동/삭제 처리
/// </summary>
public class Nir2CameraLauncher : IDisposable
{
    private readonly ILogger<Nir2CameraLauncher> _logger;
    private readonly ApplicationConfiguration _config;
    private Process? _process;
    private CancellationTokenSource? _monitorCts;
    private FileSystemWatcher? _fileWatcher;
    private bool _isFilteringActive;

    public Nir2CameraLauncher(
        ILogger<Nir2CameraLauncher> logger,
        ApplicationConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// 필터링 활성화 상태 변경 이벤트 (프로그램 실행과 별개)
    /// </summary>
    public event EventHandler<bool>? StatusChanged;

    /// <summary>
    /// 현재 프로그램 실행 상태
    /// </summary>
    public bool IsActive => _process != null && !_process.HasExited;

    /// <summary>
    /// 현재 필터링 활성화 상태
    /// </summary>
    public bool IsFilteringActive => _isFilteringActive;

    /// <summary>
    /// NIR Camera 2 프로그램 실행
    /// </summary>
    /// <returns>성공 여부와 메시지를 포함한 결과</returns>
    public async Task<(bool Success, string Message)> LaunchAsync()
    {
        try
        {
            var programName = "NIR Camera 2";
            var programPath = _config?.ExternalProgramSettings?.Nir2ProgramPath ?? string.Empty;
            
            _logger.LogInformation("Attempting to launch {ProgramName}", programName);

            // 경로 검증
            if (string.IsNullOrWhiteSpace(programPath))
            {
                var msg = $"Path not set for {programName}";
                _logger.LogWarning("Path not configured for {ProgramName}", programName);
                return (false, msg);
            }

            // 파일 존재 확인
            if (!File.Exists(programPath))
            {
                var msg = $"File not found: {programPath}";
                _logger.LogWarning("Program file not found for {ProgramName}: {Path}", programName, programPath);
                return (false, msg);
            }

            // 프로그램 실행
            _process = await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = programPath,
                    UseShellExecute = true
                };
                return Process.Start(startInfo);
            });

            if (_process != null)
            {
                _logger.LogInformation("{ProgramName} launched successfully (PID: {ProcessId})", programName, _process.Id);

                // 프로세스 종료 모니터링 시작
                StartProcessMonitoring();
            }

            _logger.LogInformation("{ProgramName} launched successfully from {Path}", programName, programPath);
            return (true, $"{programName} launched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch NIR Camera 2");
            return (false, $"Error launching NIR Camera 2: {ex.Message}");
        }
    }

    /// <summary>
    /// NIR 필터링 시작
    /// </summary>
    public async Task<(bool Success, string Message)> StartFilteringAsync()
    {
        if (_isFilteringActive)
        {
            return (false, "NIR filtering is already active");
        }

        try
        {
            var monitorPath = _config?.ExternalProgramSettings?.Nir2FilterMonitorPath ?? string.Empty;
            var destinationPath = _config?.ExternalProgramSettings?.Nir2FilterDestinationPath ?? string.Empty;

            // 경로 검증
            if (string.IsNullOrWhiteSpace(monitorPath))
            {
                return (false, "Monitor path not configured");
            }

            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return (false, "Destination path not configured");
            }

            if (!Directory.Exists(monitorPath))
            {
                return (false, $"Monitor path does not exist: {monitorPath}");
            }

            // Destination 폴더가 없으면 생성
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
                _logger.LogInformation("Created destination directory: {Path}", destinationPath);
            }

            // FileSystemWatcher 설정
            _monitorCts = new CancellationTokenSource();
            _fileWatcher = new FileSystemWatcher(monitorPath)
            {
                Filter = "*.txt",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _fileWatcher.Created += OnFileCreated;

            _isFilteringActive = true;
            StatusChanged?.Invoke(this, true);

            _logger.LogInformation("NIR filtering started - monitoring: {MonitorPath}", monitorPath);
            
            return await Task.FromResult((true, "NIR filtering started successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start NIR filtering");
            return (false, $"Error starting NIR filtering: {ex.Message}");
        }
    }

    /// <summary>
    /// NIR 필터링 중지
    /// </summary>
    public void StopFiltering()
    {
        if (!_isFilteringActive)
        {
            return;
        }

        try
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.Created -= OnFileCreated;
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher.Dispose();
                _fileWatcher = null;
            }

            _monitorCts?.Cancel();
            _monitorCts?.Dispose();
            _monitorCts = null;

            _isFilteringActive = false;
            StatusChanged?.Invoke(this, false);

            _logger.LogInformation("NIR filtering stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping NIR filtering");
        }
    }

    /// <summary>
    /// 새 파일 생성 이벤트 핸들러
    /// </summary>
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            // 파일이 완전히 쓰여질 때까지 대기
            Task.Delay(1000).Wait();

            _logger.LogInformation("New NIR file detected: {FileName}", Path.GetFileName(e.FullPath));

            // 파일 처리
            ProcessFile(e.FullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling file creation event for {FilePath}", e.FullPath);
        }
    }

    /// <summary>
    /// NIR spectrum 파일 처리
    /// </summary>
    private void ProcessFile(string txtFilePath)
    {
        try
        {
            var fileName = Path.GetFileName(txtFilePath);
            var folderPath = Path.GetDirectoryName(txtFilePath) ?? "";

            // SPC 파일 경로 찾기 (파이썬 로직과 동일)
            var fileBase = Path.GetFileNameWithoutExtension(txtFilePath);
            if (fileBase.EndsWith("A", StringComparison.OrdinalIgnoreCase))
            {
                fileBase = fileBase.Substring(0, fileBase.Length - 1);
            }
            var spcFilePath = Path.Combine(folderPath, fileBase + ".spc");

            // SPC 파일 존재 여부 확인
            bool hasSpcFile = File.Exists(spcFilePath);

            // NIR spectrum 분석
            var result = NirSpectrumFilter.AnalyzeSpectrum(txtFilePath);

            var destinationPath = _config?.ExternalProgramSettings?.Nir2FilterDestinationPath ?? "";

            if (result.PassesFilter)
            {
                // 조건 만족: 파일 이동
                _logger.LogInformation("✓ {FileName} - {Message}", fileName, result.Message);
                
                // 상세 기준 정보 로깅
                if (result.CriteriaDetails != null && result.CriteriaDetails.Count > 0)
                {
                    foreach (var criterion in result.CriteriaDetails)
                    {
                        _logger.LogInformation("  {CriterionName}: {Value:F5} (threshold: {Threshold:F5}) - {Status}", 
                            criterion.Key, 
                            criterion.Value.Value, 
                            criterion.Value.Threshold,
                            criterion.Value.Passed ? "PASS" : "FAIL");
                    }
                }

                // TXT 파일 이동
                var destTxtPath = Path.Combine(destinationPath, fileName);
                File.Move(txtFilePath, destTxtPath, true);
                _logger.LogInformation("  → Moved: {FileName} → {DestPath}", fileName, destinationPath);

                // SPC 파일도 이동
                if (hasSpcFile)
                {
                    var spcFileName = Path.GetFileName(spcFilePath);
                    var destSpcPath = Path.Combine(destinationPath, spcFileName);
                    File.Move(spcFilePath, destSpcPath, true);
                    _logger.LogInformation("  → Paired .spc file also moved: {SpcFileName}", spcFileName);
                }
            }
            else
            {
                // 조건 불만족: 파일 삭제
                _logger.LogInformation("✗ {FileName} - Filter FAILED (criteria not met)", fileName);
                
                File.Delete(txtFilePath);
                _logger.LogInformation("  → Deleted: {FileName}", fileName);

                if (hasSpcFile)
                {
                    File.Delete(spcFilePath);
                    _logger.LogInformation("  → Paired .spc file also deleted");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing NIR file: {FilePath}", txtFilePath);
        }
    }

    /// <summary>
    /// 프로세스 종료 모니터링
    /// </summary>
    private void StartProcessMonitoring()
    {
        var cts = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            try
            {
                if (_process != null)
                {
                    await _process.WaitForExitAsync(cts.Token);
                    _logger.LogInformation("NIR Camera 2 process exited");
                }
            }
            catch (OperationCanceledException)
            {
                // 정상적인 취소
            }
        }, cts.Token);
    }

    public void Dispose()
    {
        StopFiltering();
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _process?.Dispose();
    }
}
