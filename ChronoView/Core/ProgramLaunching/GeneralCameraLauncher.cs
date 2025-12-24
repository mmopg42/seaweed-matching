using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ChronoView.Models;

namespace ChronoView.Core.ProgramLaunching;

/// <summary>
/// General Camera 프로그램 실행 및 상태 추적 담당
/// </summary>
public class GeneralCameraLauncher : IDisposable
{
    private readonly ILogger<GeneralCameraLauncher> _logger;
    private readonly ApplicationConfiguration _config;
    private Process? _process;
    private CancellationTokenSource? _monitorCts;

    public GeneralCameraLauncher(
        ILogger<GeneralCameraLauncher> logger,
        ApplicationConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// 프로그램 활성화 상태 변경 이벤트
    /// </summary>
    public event EventHandler<bool>? StatusChanged;

    /// <summary>
    /// 현재 프로그램 실행 상태
    /// </summary>
    public bool IsActive => _process != null && !_process.HasExited;

    /// <summary>
    /// General Camera 프로그램 실행
    /// </summary>
    /// <returns>성공 여부와 메시지를 포함한 결과</returns>
    public async Task<(bool Success, string Message)> LaunchAsync()
    {
        try
        {
            var programName = "General Camera";
            var programPath = _config?.ExternalProgramSettings?.GeneralCameraProgramPath ?? string.Empty;
            
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
                // 상태를 Active로 변경
                StatusChanged?.Invoke(this, true);
                _logger.LogInformation("{ProgramName} activated (PID: {ProcessId})", programName, _process.Id);

                // 프로세스 종료 모니터링 시작
                StartMonitoring();
            }

            _logger.LogInformation("{ProgramName} launched successfully from {Path}", programName, programPath);
            return (true, $"{programName} launched successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch General Camera");
            return (false, $"Error launching General Camera: {ex.Message}");
        }
    }

    /// <summary>
    /// 프로세스 종료 모니터링
    /// </summary>
    private void StartMonitoring()
    {
        _monitorCts?.Cancel();
        _monitorCts = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                if (_process != null)
                {
                    await _process.WaitForExitAsync(_monitorCts.Token);
                    
                    // 프로세스 종료 시 상태를 Deactivate로 변경
                    StatusChanged?.Invoke(this, false);
                    _logger.LogInformation("General Camera deactivated (process exited)");
                }
            }
            catch (OperationCanceledException)
            {
                // 정상적인 취소
            }
        }, _monitorCts.Token);
    }

    public void Dispose()
    {
        _monitorCts?.Cancel();
        _monitorCts?.Dispose();
        _process?.Dispose();
    }
}
