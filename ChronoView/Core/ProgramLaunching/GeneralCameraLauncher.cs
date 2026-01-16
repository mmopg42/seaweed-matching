using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ChronoView.Models;
using ChronoView.Core.Configuration;
using ChronoView.Helpers;

namespace ChronoView.Core.ProgramLaunching;

/// <summary>
/// General Camera 프로그램 실행 및 상태 추적 담당
/// </summary>
public class GeneralCameraLauncher : IDisposable
{
    private readonly ILogger<GeneralCameraLauncher> _logger;
    private readonly IConfigurationManager _configManager;
    private Process? _process;
    private CancellationTokenSource? _monitorCts;

    public GeneralCameraLauncher(
        ILogger<GeneralCameraLauncher> logger,
        IConfigurationManager configManager)
    {
        _logger = logger;
        _configManager = configManager;
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
    /// 현재 관리 중인 프로세스 객체
    /// </summary>
    public Process? CurrentProcess => _process;

    /// <summary>
    /// 현재 설정된 경로의 프로그램이 실행 중인지 확인하고 상태 동기화
    /// </summary>
    public async Task CheckStatusAsync()
    {
        try
        {
            var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
            var programPath = config?.ExternalProgramSettings?.GeneralCameraProgramPath;

            if (string.IsNullOrWhiteSpace(programPath))
            {
                StatusChanged?.Invoke(this, false);
                return;
            }

            // 비동기로 프로세스 검색 (UI 스레드 차단 방지)
            var existingProcess = await Task.Run(() => WindowActivationHelper.FindExistingProcess(programPath));
            
            if (existingProcess != null && !existingProcess.HasExited)
            {
                _process = existingProcess;
                _logger.LogInformation("General Camera process detected during sync (PID: {ProcessId})", _process.Id);
                StatusChanged?.Invoke(this, true);
                StartMonitoring();
            }
            else
            {
                StatusChanged?.Invoke(this, false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check status for General Camera");
        }
    }

    /// <summary>
    /// 프로그램 종료
    /// </summary>
    public async Task TerminateAsync()
    {
        if (_process == null || _process.HasExited)
            return;

        try
        {
            _logger.LogInformation("Terminating General Camera (PID: {ProcessId})", _process.Id);
            _process.Kill(true); // Recursive kill
            await _process.WaitForExitAsync();
            StatusChanged?.Invoke(this, false);
            _logger.LogInformation("General Camera terminated by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate General Camera");
        }
    }

    /// <summary>
    /// General Camera 프로그램 실행 또는 이미 실행 중인 경우 활성화
    /// </summary>
    /// <returns>성공 여부와 메시지를 포함한 결과</returns>
    public async Task<(bool Success, string Message)> LaunchAsync()
    {
        try
        {
            var config = _configManager.LoadConfiguration<ApplicationConfiguration>();
            var programName = "General Camera";
            var programPath = config?.ExternalProgramSettings?.GeneralCameraProgramPath ?? string.Empty;
            
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

            // 이미 실행 중인 프로세스 검색
            var existingProcess = WindowActivationHelper.FindExistingProcess(programPath);
            if (existingProcess != null && !existingProcess.HasExited)
            {
                _process = existingProcess;
                _logger.LogInformation("Existing {ProgramName} process found (PID: {ProcessId}), activating...", programName, existingProcess.Id);
                WindowActivationHelper.ActivateProcessWindow(_process);
                StatusChanged?.Invoke(this, true);
                StartMonitoring();
                return (true, $"{programName} activated");
            }

            // 실행 중이 아니면 새로 실행
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
                // Wait for window to appear (hybrid: detection + min delay)
                await _process.WaitForWindowAsync(minDurationMs: 1000, timeoutMs: 10000);
                
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
