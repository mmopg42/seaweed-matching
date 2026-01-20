using System.CommandLine;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using Controller = SkillsScripts.UiAutomation.ChronoSetupWindowController;

namespace UiAutomation.Commands;

/// <summary>
/// Setup commands for ChronoView SetupWindow automation.
/// Provides commands to interact with the SetupWindow including launching cameras,
/// toggling NIR filtering, and starting monitoring.
/// </summary>
public class SetupCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_TIMEOUT = 3;

    /// <summary>
    /// Registers all setup commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // JSON output option
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // Timeout option for wait operations
        var timeoutOption = new Option<int>(
            ["--timeout-ms"],
            () => 10000,
            "Timeout in milliseconds for wait operations"
        );

        // Quiet option for agent consumption
        var quietOption = new Option<bool>(
            ["--quiet", "-q"],
            "Suppress all non-error output"
        );

        // setup 명령 그룹
        var setupCommand = new Command("setup", "SetupWindow automation commands");

        // setup open-settings: SettingsDialog 열기
        var openSettingsCommand = new Command("open-settings", "Open SettingsDialog from SetupWindow");
        openSettingsCommand.AddOption(jsonOption);
        openSettingsCommand.AddOption(quietOption);
        openSettingsCommand.SetHandler((json, quiet) =>
        {
            ExecuteOpenSettings(json, quiet);
        }, jsonOption, quietOption);
        setupCommand.AddCommand(openSettingsCommand);

        // setup camera-general: General Camera 버튼 클릭
        var cameraGeneralCommand = new Command("camera-general", "Click General Camera launch button");
        cameraGeneralCommand.AddOption(jsonOption);
        cameraGeneralCommand.AddOption(quietOption);
        cameraGeneralCommand.SetHandler((json, quiet) =>
        {
            ExecuteCameraGeneral(json, quiet);
        }, jsonOption, quietOption);
        setupCommand.AddCommand(cameraGeneralCommand);

        // setup camera-nir1: NIR1 Camera 버튼 클릭
        var cameraNir1Command = new Command("camera-nir1", "Click NIR1 Camera launch button");
        cameraNir1Command.AddOption(jsonOption);
        cameraNir1Command.AddOption(quietOption);
        cameraNir1Command.SetHandler((json, quiet) =>
        {
            ExecuteCameraNir1(json, quiet);
        }, jsonOption, quietOption);
        setupCommand.AddCommand(cameraNir1Command);

        // setup camera-nir2: NIR2 Camera 버튼 클릭
        var cameraNir2Command = new Command("camera-nir2", "Click NIR2 Camera launch button");
        cameraNir2Command.AddOption(jsonOption);
        cameraNir2Command.AddOption(quietOption);
        cameraNir2Command.SetHandler((json, quiet) =>
        {
            ExecuteCameraNir2(json, quiet);
        }, jsonOption, quietOption);
        setupCommand.AddCommand(cameraNir2Command);

        // setup toggle-nir-filtering: NIR 필터링 토글
        var toggleNirFilteringCommand = new Command("toggle-nir-filtering", "Toggle NIR filtering state");
        toggleNirFilteringCommand.AddOption(jsonOption);
        toggleNirFilteringCommand.AddOption(quietOption);
        var stateOption = new Option<string?>(
            ["--state"],
            "Target state: on or off (default: toggle current state)"
        );
        toggleNirFilteringCommand.AddOption(stateOption);
        toggleNirFilteringCommand.SetHandler((json, quiet, state) =>
        {
            ExecuteToggleNirFiltering(json, quiet, state);
        }, jsonOption, quietOption, stateOption);
        setupCommand.AddCommand(toggleNirFilteringCommand);

        // setup camera-states: 카메라 버튼 상태 확인
        var cameraStatesCommand = new Command("camera-states", "Get camera button states");
        cameraStatesCommand.AddOption(jsonOption);
        cameraStatesCommand.AddOption(quietOption);
        cameraStatesCommand.SetHandler((json, quiet) =>
        {
            ExecuteCameraStates(json, quiet);
        }, jsonOption, quietOption);
        setupCommand.AddCommand(cameraStatesCommand);

        // setup start-monitoring: 모니터링 시작 버튼 클릭
        var startMonitoringCommand = new Command("start-monitoring", "Click Start button to begin monitoring");
        startMonitoringCommand.AddOption(jsonOption);
        startMonitoringCommand.AddOption(timeoutOption);
        startMonitoringCommand.AddOption(quietOption);
        startMonitoringCommand.SetHandler((json, timeout, quiet) =>
        {
            ExecuteStartMonitoring(json, timeout, quiet);
        }, jsonOption, timeoutOption, quietOption);
        setupCommand.AddCommand(startMonitoringCommand);

        // setup complete-full: 완전한 셋업 완료 워크플로우
        var completeFullCommand = new Command("complete-full", "Execute complete setup workflow");
        completeFullCommand.AddOption(jsonOption);
        completeFullCommand.AddOption(timeoutOption);
        completeFullCommand.AddOption(quietOption);
        var skipNirToggleOption = new Option<bool>(
            ["--skip-nir-toggle"],
            "Skip NIR filtering toggle step"
        );
        completeFullCommand.AddOption(skipNirToggleOption);
        completeFullCommand.SetHandler((json, timeout, quiet, skipNirToggle) =>
        {
            ExecuteCompleteFull(json, timeout, quiet, skipNirToggle);
        }, jsonOption, timeoutOption, quietOption, skipNirToggleOption);
        setupCommand.AddCommand(completeFullCommand);

        rootCommand.AddCommand(setupCommand);
    }

    #region Command Handlers

    /// <summary>
    /// Execute setup open-settings command.
    /// </summary>
    private static void ExecuteOpenSettings(bool json, bool quiet)
    {
        SetQuietMode(quiet);

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("open-settings", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        bool success = controller.ClickSettingsButton(setupWindow);
        if (!success)
        {
            PrintError("open-settings", "Failed to click Settings button");
            PrintJsonError(json, "Failed to click Settings button", EXIT_ERROR);
            Environment.Exit(EXIT_ERROR);
            return;
        }

        // Wait for SettingsDialog to appear
        Thread.Sleep(500);
        var finder = new Finder(automation);
        var settingsDialog = finder.FindSettingsDialog();
        bool settingsFound = settingsDialog != null;

        if (json)
        {
            PrintJsonOutput(new
            {
                success = settingsFound,
                data = new
                {
                    settingsOpened = settingsFound,
                    settingsTitle = settingsFound ? settingsDialog!.Name : null
                },
                error = settingsFound ? null : "SettingsDialog did not appear"
            });
        }
        else
        {
            if (settingsFound)
            {
                PrintOutput($"[open-settings] SettingsDialog opened: '{settingsDialog!.Name}'");
            }
            else
            {
                PrintOutput("[open-settings] Settings button clicked but SettingsDialog not found");
            }
        }

        Environment.Exit(settingsFound ? EXIT_SUCCESS : EXIT_TIMEOUT);
    }

    /// <summary>
    /// Execute setup camera-general command.
    /// </summary>
    private static void ExecuteCameraGeneral(bool json, bool quiet)
    {
        SetQuietMode(quiet);

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("camera-general", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        bool success = controller.ClickGeneralCamera(setupWindow);

        if (json)
        {
            PrintJsonOutput(new
            {
                success,
                data = success ? new { cameraLaunched = "general" } : null,
                error = success ? null : "Failed to click General Camera button"
            });
        }
        else
        {
            if (success)
            {
                PrintOutput("[camera-general] General Camera button clicked");
            }
            else
            {
                PrintError("camera-general", "Failed to click General Camera button");
            }
        }

        Environment.Exit(success ? EXIT_SUCCESS : EXIT_ERROR);
    }

    /// <summary>
    /// Execute setup camera-nir1 command.
    /// </summary>
    private static void ExecuteCameraNir1(bool json, bool quiet)
    {
        SetQuietMode(quiet);

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("camera-nir1", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        bool success = controller.ClickNir1(setupWindow);

        if (json)
        {
            PrintJsonOutput(new
            {
                success,
                data = success ? new { cameraLaunched = "nir1" } : null,
                error = success ? null : "Failed to click NIR1 Camera button"
            });
        }
        else
        {
            if (success)
            {
                PrintOutput("[camera-nir1] NIR1 Camera button clicked");
            }
            else
            {
                PrintError("camera-nir1", "Failed to click NIR1 Camera button");
            }
        }

        Environment.Exit(success ? EXIT_SUCCESS : EXIT_ERROR);
    }

    /// <summary>
    /// Execute setup camera-nir2 command.
    /// </summary>
    private static void ExecuteCameraNir2(bool json, bool quiet)
    {
        SetQuietMode(quiet);

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("camera-nir2", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        bool success = controller.ClickNir2(setupWindow);

        if (json)
        {
            PrintJsonOutput(new
            {
                success,
                data = success ? new { cameraLaunched = "nir2" } : null,
                error = success ? null : "Failed to click NIR2 Camera button"
            });
        }
        else
        {
            if (success)
            {
                PrintOutput("[camera-nir2] NIR2 Camera button clicked");
            }
            else
            {
                PrintError("camera-nir2", "Failed to click NIR2 Camera button");
            }
        }

        Environment.Exit(success ? EXIT_SUCCESS : EXIT_ERROR);
    }

    /// <summary>
    /// Execute setup toggle-nir-filtering command.
    /// </summary>
    private static void ExecuteToggleNirFiltering(bool json, bool quiet, string? state)
    {
        SetQuietMode(quiet);

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("toggle-nir-filtering", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        // Parse target state if provided
        bool? targetState = null;
        if (!string.IsNullOrEmpty(state))
        {
            if (state.Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                targetState = true;
            }
            else if (state.Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                targetState = false;
            }
            else
            {
                PrintError("toggle-nir-filtering", $"Invalid state value: '{state}'. Use 'on' or 'off'");
                PrintJsonError(json, $"Invalid state value: '{state}'", EXIT_ERROR);
                Environment.Exit(EXIT_ERROR);
                return;
            }
        }

        bool success = controller.ToggleNirFiltering(targetState, setupWindow);

        // Get final state
        var states = controller.GetCameraStates(setupWindow);
        // Note: NIR filtering state is not directly in camera states,
        // but we can infer from success

        if (json)
        {
            PrintJsonOutput(new
            {
                success,
                data = new
                {
                    toggled = success,
                    targetState = targetState.HasValue ? (targetState.Value ? "on" : "off") : "toggled"
                },
                error = success ? null : "Failed to toggle NIR filtering"
            });
        }
        else
        {
            if (success)
            {
                string stateText = targetState.HasValue
                    ? $"to {(targetState.Value ? "ON" : "OFF")}"
                    : "(toggled)";
                PrintOutput($"[toggle-nir-filtering] NIR filtering set {stateText}");
            }
            else
            {
                PrintError("toggle-nir-filtering", "Failed to toggle NIR filtering");
            }
        }

        Environment.Exit(success ? EXIT_SUCCESS : EXIT_ERROR);
    }

    /// <summary>
    /// Execute setup camera-states command.
    /// </summary>
    private static void ExecuteCameraStates(bool json, bool quiet)
    {
        SetQuietMode(quiet);

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("camera-states", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        var states = controller.GetCameraStates(setupWindow);

        if (json)
        {
            PrintJsonOutput(new
            {
                success = true,
                data = new
                {
                    general = states.ContainsKey("general") && states["general"],
                    nir1 = states.ContainsKey("nir1") && states["nir1"],
                    nir2 = states.ContainsKey("nir2") && states["nir2"]
                }
            });
        }
        else
        {
            PrintOutput("[camera-states] Camera button states:");
            PrintOutput($"  General: {(states.ContainsKey("general") && states["general"] ? "Enabled" : "Disabled")}");
            PrintOutput($"  NIR1: {(states.ContainsKey("nir1") && states["nir1"] ? "Enabled" : "Disabled")}");
            PrintOutput($"  NIR2: {(states.ContainsKey("nir2") && states["nir2"] ? "Enabled" : "Disabled")}");
        }

        Environment.Exit(EXIT_SUCCESS);
    }

    /// <summary>
    /// Execute setup start-monitoring command.
    /// </summary>
    private static void ExecuteStartMonitoring(bool json, int timeout, bool quiet)
    {
        SetQuietMode(quiet);

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("start-monitoring", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        bool clicked = controller.ClickStartButton(setupWindow);
        if (!clicked)
        {
            PrintError("start-monitoring", "Failed to click Start button");
            PrintJsonError(json, "Failed to click Start button", EXIT_ERROR);
            Environment.Exit(EXIT_ERROR);
            return;
        }

        // Wait for MainWindow
        var mainWindow = controller.WaitForMainWindow(timeout);

        bool success = mainWindow != null;
        if (json)
        {
            object data;
            if (success)
            {
                data = new
                {
                    startButtonClicked = true,
                    mainWindowFound = true,
                    mainWindowTitle = mainWindow!.Name
                };
            }
            else
            {
                data = new
                {
                    startButtonClicked = true,
                    mainWindowFound = false
                };
            }
            PrintJsonOutput(new
            {
                success,
                data,
                error = success ? null : $"MainWindow did not appear within {timeout}ms"
            });
        }
        else
        {
            if (success)
            {
                PrintOutput($"[start-monitoring] MainWindow found: '{mainWindow!.Name}'");
            }
            else
            {
                PrintError("start-monitoring", $"MainWindow did not appear within {timeout}ms");
            }
        }

        Environment.Exit(success ? EXIT_SUCCESS : EXIT_TIMEOUT);
    }

    /// <summary>
    /// Execute setup complete-full command.
    /// </summary>
    private static void ExecuteCompleteFull(bool json, int timeout, bool quiet, bool skipNirToggle)
    {
        SetQuietMode(quiet);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var steps = new Dictionary<string, bool>
        {
            ["general_camera"] = false,
            ["nir1_camera"] = false,
            ["nir2_camera"] = false,
            ["nir_filtering"] = true, // Optional step, default true
            ["start_button"] = false,
            ["mainwindow_ready"] = false
        };

        using var automation = new UIA3Automation();
        var controller = new Controller(automation);

        var setupWindow = controller.FindSetupWindow();
        if (setupWindow == null)
        {
            PrintError("complete-full", "SetupWindow not found");
            PrintJsonError(json, "SetupWindow not found", EXIT_NOT_FOUND);
            Environment.Exit(EXIT_NOT_FOUND);
            return;
        }

        // Step 1: Click General Camera
        PrintVerbose("complete-full", "Clicking General Camera button...");
        steps["general_camera"] = controller.ClickGeneralCamera(setupWindow);
        if (!steps["general_camera"])
        {
            PrintError("complete-full", "Failed at General Camera step");
            PrintCompleteFullResult(json, steps, stopwatch.ElapsedMilliseconds, "General Camera button click failed");
            Environment.Exit(EXIT_ERROR);
            return;
        }
        Thread.Sleep(500);

        // Step 2: Click NIR1 Camera
        PrintVerbose("complete-full", "Clicking NIR1 Camera button...");
        steps["nir1_camera"] = controller.ClickNir1(setupWindow);
        if (!steps["nir1_camera"])
        {
            PrintError("complete-full", "Failed at NIR1 Camera step");
            PrintCompleteFullResult(json, steps, stopwatch.ElapsedMilliseconds, "NIR1 Camera button click failed");
            Environment.Exit(EXIT_ERROR);
            return;
        }
        Thread.Sleep(500);

        // Step 3: Click NIR2 Camera
        PrintVerbose("complete-full", "Clicking NIR2 Camera button...");
        steps["nir2_camera"] = controller.ClickNir2(setupWindow);
        if (!steps["nir2_camera"])
        {
            PrintError("complete-full", "Failed at NIR2 Camera step");
            PrintCompleteFullResult(json, steps, stopwatch.ElapsedMilliseconds, "NIR2 Camera button click failed");
            Environment.Exit(EXIT_ERROR);
            return;
        }
        Thread.Sleep(500);

        // Step 4: Optional NIR filtering toggle
        if (!skipNirToggle)
        {
            PrintVerbose("complete-full", "Toggling NIR filtering...");
            steps["nir_filtering"] = controller.ToggleNirFiltering(null, setupWindow);
        }

        // Step 5: Click Start button
        PrintVerbose("complete-full", "Clicking Start button...");
        steps["start_button"] = controller.ClickStartButton(setupWindow);
        if (!steps["start_button"])
        {
            PrintError("complete-full", "Failed at Start button step");
            PrintCompleteFullResult(json, steps, stopwatch.ElapsedMilliseconds, "Start button click failed");
            Environment.Exit(EXIT_ERROR);
            return;
        }

        // Step 6: Wait for MainWindow
        PrintVerbose("complete-full", $"Waiting for MainWindow (timeout: {timeout}ms)...");
        var mainWindow = controller.WaitForMainWindow(timeout);
        steps["mainwindow_ready"] = mainWindow != null;

        stopwatch.Stop();

        bool allSuccess = steps.Values.All(v => v);
        if (json)
        {
            PrintCompleteFullResult(json, steps, stopwatch.ElapsedMilliseconds,
                allSuccess ? null : "Some steps failed");
        }
        else
        {
            if (allSuccess)
            {
                PrintOutput($"[complete-full] Setup workflow completed in {stopwatch.ElapsedMilliseconds}ms");
                PrintOutput($"  General Camera: OK");
                PrintOutput($"  NIR1 Camera: OK");
                PrintOutput($"  NIR2 Camera: OK");
                if (!skipNirToggle)
                {
                    PrintOutput($"  NIR Filtering: {(steps["nir_filtering"] ? "OK" : "SKIPPED")}");
                }
                PrintOutput($"  Start Button: OK");
                PrintOutput($"  MainWindow Ready: OK");
            }
            else
            {
                PrintOutput($"[complete-full] Setup workflow incomplete after {stopwatch.ElapsedMilliseconds}ms");
                foreach (var step in steps)
                {
                    PrintOutput($"  {step.Key}: {(step.Value ? "OK" : "FAILED")}");
                }
            }
        }

        Environment.Exit(allSuccess ? EXIT_SUCCESS : EXIT_ERROR);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Print JSON output with consistent formatting for programmatic consumption
    /// </summary>
    private static void PrintJsonOutput(object data)
    {
        Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        }));
    }

    /// <summary>
    /// Print JSON error response
    /// </summary>
    private static void PrintJsonError(bool json, string error, int errorCode)
    {
        if (json)
        {
            PrintJsonOutput(new
            {
                success = false,
                error,
                errorCode
            });
        }
    }

    /// <summary>
    /// Print complete-full workflow result
    /// </summary>
    private static void PrintCompleteFullResult(bool json, Dictionary<string, bool> steps, long durationMs, string? error)
    {
        if (json)
        {
            PrintJsonOutput(new
            {
                success = string.IsNullOrEmpty(error),
                data = new
                {
                    steps,
                    duration_ms = durationMs
                },
                error
            });
        }
    }

    /// <summary>
    /// Print output only if not in quiet mode
    /// </summary>
    private static void PrintOutput(string message)
    {
        if (!s_isQuiet)
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Print error message (always shown, even in quiet mode)
    /// </summary>
    private static void PrintError(string command, string message)
    {
        Console.Error.WriteLine($"[{command}] ERROR: {message}");
    }

    /// <summary>
    /// Print verbose output only if verbose mode is enabled
    /// </summary>
    private static void PrintVerbose(string command, string message)
    {
        if (s_isVerbose && !s_isQuiet)
        {
            Console.WriteLine($"[{command}] {message}");
        }
    }

    /// <summary>
    /// Set quiet mode state for the current command execution
    /// </summary>
    private static void SetQuietMode(bool quiet)
    {
        s_isQuiet = quiet;
    }

    // Quiet mode state (per command)
    private static bool s_isQuiet = false;
    private static bool s_isVerbose = false;

    #endregion
}
