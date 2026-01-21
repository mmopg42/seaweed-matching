using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using static UiAutomation.Commands.ExitCodes;
using static UiAutomation.Commands.JsonResponseHelper;
using static UiAutomation.Commands.DryRunHandler;

namespace UiAutomation.Commands;

/// <summary>
/// App lifecycle commands for ChronoView process management.
/// Provides commands to launch, stop, restart, and check status of ChronoView application.
/// This enables test-executor agents to manage ChronoView autonomously.
/// </summary>
public class AppLifecycleCommands : ICommandHandler
{
    // ChronoView process name (without .exe extension for GetProcessesByName)
    private const string CHRONOVIEW_PROCESS_NAME = "ChronoView";

    // Path to ChronoView project relative to workspace root
    private const string CHRONOVIEW_PROJECT_PATH = "ChronoView/ChronoView.csproj";

    /// <summary>
    /// Registers all app lifecycle commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // JSON output option
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // app 명령: ChronoView 앱 라이프사이클 관리
        var appCommand = new Command("app", "ChronoView 앱 라이프사이클 관리");

        // app launch: ChronoView 실행
        var launchCommand = new Command("launch", "ChronoView 앱 실행");
        launchCommand.AddOption(jsonOption);
        launchCommand.SetHandler(async (InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "APP_LAUNCH", "app launch"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                int processId = await LaunchChronoViewAsync();

                if (processId > 0)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            launched = true,
                            processId
                        });
                    }
                    else
                    {
                        PrintOutput($"[app launch] ChronoView launched with PID: {processId}");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("Failed to launch ChronoView", ERROR,
                            "Check if ChronoView is already running. Try 'app status --json'.");
                    }
                    else
                    {
                        PrintOutput("[app launch] Failed to launch ChronoView");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[app launch] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        appCommand.AddCommand(launchCommand);

        // app stop: ChronoView 중지
        var stopCommand = new Command("stop", "모든 ChronoView 프로세스 중지");
        stopCommand.AddOption(jsonOption);
        stopCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "APP_STOP", "app stop"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                int stoppedCount = StopChronoViewProcesses();

                if (stoppedCount > 0)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            stopped = true,
                            processesStopped = stoppedCount
                        });
                    }
                    else
                    {
                        PrintOutput($"[app stop] Stopped {stoppedCount} ChronoView process(es)");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("No ChronoView processes found running", NOT_FOUND,
                            "Ensure ChronoView was launched before stopping.");
                    }
                    else
                    {
                        PrintOutput("[app stop] No ChronoView processes found running");
                    }
                    context.ExitCode = NOT_FOUND;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[app stop] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        appCommand.AddCommand(stopCommand);

        // app restart: ChronoView 재시작
        var restartCommand = new Command("restart", "ChronoView 재시작 (중지 후 실행)");
        restartCommand.AddOption(jsonOption);
        restartCommand.SetHandler(async (InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "APP_RESTART", "app restart"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                var result = await RestartChronoViewAsync();

                if (result.Success)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            restarted = true,
                            processesStopped = result.StoppedCount,
                            newProcessId = result.ProcessId
                        });
                    }
                    else
                    {
                        PrintOutput($"[app restart] Restarted ChronoView: stopped {result.StoppedCount} process(es), new PID: {result.ProcessId}");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("Failed to restart ChronoView", ERROR,
                            "Stopped existing processes but launch failed. Try 'app launch'.");
                    }
                    else
                    {
                        PrintOutput($"[app restart] Failed to restart (stopped {result.StoppedCount} processes, launch failed)");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[app restart] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        appCommand.AddCommand(restartCommand);

        // app status: ChronoView 상태 확인
        var statusCommand = new Command("status", "ChronoView 실행 상태 확인");
        statusCommand.AddOption(jsonOption);
        statusCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "APP_STATUS", "app status"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                var status = GetChronoViewStatus();

                if (status.IsRunning)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            isRunning = true,
                            processCount = status.ProcessCount,
                            processIds = status.ProcessIds,
                            mainWindowTitles = status.MainWindowTitles
                        });
                    }
                    else
                    {
                        PrintOutput($"[app status] ChronoView is running: {status.ProcessCount} process(es)");
                        foreach (var pid in status.ProcessIds)
                        {
                            PrintOutput($"  - PID: {pid}");
                        }
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            isRunning = false,
                            processCount = 0,
                            processIds = Array.Empty<int>(),
                            mainWindowTitles = Array.Empty<string>()
                        });
                    }
                    else
                    {
                        PrintOutput("[app status] ChronoView is not running");
                    }
                    context.ExitCode = NOT_FOUND;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[app status] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        appCommand.AddCommand(statusCommand);

        rootCommand.AddCommand(appCommand);
    }

    /// <summary>
    /// Launch ChronoView using dotnet run.
    /// Returns immediately after Process.Start() without waiting for exit.
    /// </summary>
    /// <returns>Process ID if launched, 0 if failed</returns>
    private static async Task<int> LaunchChronoViewAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{CHRONOVIEW_PROJECT_PATH}\"",
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                var process = Process.Start(startInfo);
                return process?.Id ?? 0;
            }
            catch (Exception ex)
            {
                PrintVerbose($"[app launch] Exception: {ex.Message}");
                return 0;
            }
        });
    }

    /// <summary>
    /// Stop all ChronoView processes.
    /// </summary>
    /// <returns>Count of processes killed</returns>
    private static int StopChronoViewProcesses()
    {
        int stoppedCount = 0;

        try
        {
            var processes = Process.GetProcessesByName(CHRONOVIEW_PROCESS_NAME);

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                    stoppedCount++;
                    PrintVerbose($"[app stop] Killed PID: {process.Id}");
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                    PrintVerbose($"[app stop] Process {process.Id} already exited");
                }
                catch (Exception ex)
                {
                    PrintVerbose($"[app stop] Failed to kill PID {process.Id}: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            PrintVerbose($"[app stop] Exception enumerating processes: {ex.Message}");
        }

        return stoppedCount;
    }

    /// <summary>
    /// Result of restart operation.
    /// </summary>
    private readonly struct RestartResult
    {
        public bool Success { get; init; }
        public int StoppedCount { get; init; }
        public int ProcessId { get; init; }
    }

    /// <summary>
    /// Restart ChronoView (stop then launch).
    /// </summary>
    /// <returns>RestartResult with status details</returns>
    private static async Task<RestartResult> RestartChronoViewAsync()
    {
        // Stop existing processes
        int stoppedCount = StopChronoViewProcesses();

        // Wait for cleanup
        await Task.Delay(500);

        // Launch new instance
        int processId = await LaunchChronoViewAsync();

        return new RestartResult
        {
            Success = processId > 0,
            StoppedCount = stoppedCount,
            ProcessId = processId
        };
    }

    /// <summary>
    /// Status information for ChronoView processes.
    /// </summary>
    private readonly struct ChronoViewStatus
    {
        public bool IsRunning { get; init; }
        public int ProcessCount { get; init; }
        public int[] ProcessIds { get; init; }
        public string[] MainWindowTitles { get; init; }
    }

    /// <summary>
    /// Check if ChronoView is running and get status details.
    /// </summary>
    /// <returns>ChronoViewStatus with process information</returns>
    private static ChronoViewStatus GetChronoViewStatus()
    {
        try
        {
            var processes = Process.GetProcessesByName(CHRONOVIEW_PROCESS_NAME);
            var processIds = new int[processes.Length];
            var titles = new string[processes.Length];

            for (int i = 0; i < processes.Length; i++)
            {
                processIds[i] = processes[i].Id;
                try
                {
                    titles[i] = processes[i].MainWindowTitle ?? "(no title)";
                }
                catch
                {
                    titles[i] = "(access denied)";
                }
                processes[i].Dispose();
            }

            return new ChronoViewStatus
            {
                IsRunning = processIds.Length > 0,
                ProcessCount = processIds.Length,
                ProcessIds = processIds,
                MainWindowTitles = titles
            };
        }
        catch (Exception ex)
        {
            PrintVerbose($"[app status] Exception: {ex.Message}");
            return new ChronoViewStatus
            {
                IsRunning = false,
                ProcessCount = 0,
                ProcessIds = Array.Empty<int>(),
                MainWindowTitles = Array.Empty<string>()
            };
        }
    }

    /// <summary>
    /// Print output only if not in quiet mode
    /// </summary>
    private static void PrintOutput(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Print verbose output only if verbose mode is enabled
    /// </summary>
    private static void PrintVerbose(string message)
    {
        Console.WriteLine($"[VERBOSE] {message}");
    }
}
