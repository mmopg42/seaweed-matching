using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ChronoView.Helpers;

/// <summary>
/// Extension methods for System.Diagnostics.Process
/// </summary>
public static class ProcessExtensions
{
    /// <summary>
    /// Waits for the process to open a window and be ready for input,
    /// with a guaranteed minimum duration for UI stability.
    /// </summary>
    /// <param name="process">The process to wait for.</param>
    /// <param name="minDurationMs">Minimum duration to wait (for UI visibility). Default: 1000ms.</param>
    /// <param name="timeoutMs">Maximum time to wait for window detection. Default: 10000ms.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task WaitForWindowAsync(
        this Process process,
        int minDurationMs = 1000,
        int timeoutMs = 10000,
        CancellationToken cancellationToken = default)
    {
        if (process == null || process.HasExited)
            return;

        // Run detection and minimum delay in parallel, wait for BOTH to complete
        var minDelayTask = Task.Delay(minDurationMs, cancellationToken);
        var detectionTask = WaitForWindowDetectionAsync(process, timeoutMs, cancellationToken);

        await Task.WhenAll(minDelayTask, detectionTask);
    }

    private static async Task WaitForWindowDetectionAsync(
        Process process,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 1: Wait for input idle (message loop started)
            await Task.Run(() =>
            {
                try
                {
                    process.WaitForInputIdle(timeoutMs);
                }
                catch (InvalidOperationException)
                {
                    // Process has no graphical interface or has exited
                }
            }, cancellationToken);

            // Step 2: Poll for MainWindowHandle (window created)
            while (stopwatch.ElapsedMilliseconds < timeoutMs && !cancellationToken.IsCancellationRequested)
            {
                if (process.HasExited)
                    return;

                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                    return; // Window detected!

                await Task.Delay(100, cancellationToken); // Poll every 100ms
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested, exit gracefully
        }
        catch (InvalidOperationException)
        {
            // Process exited during wait
        }
    }
}
