using System.CommandLine;
using System.Text.Json;
using SetupVerifier = SkillsScripts.UiAutomation.SetupConfigVerifier;
using SetupController = SkillsScripts.UiAutomation.ChronoSetupWindowController;
using SkillsScripts.UiAutomation;

namespace UiAutomation.Commands;

/// <summary>
/// Setup window commands for ChronoView automation.
/// Provides commands to control SetupWindow (camera buttons, start button, etc.)
/// and verify configuration consistency.
/// </summary>
public class SetupCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_TIMEOUT = 3;
    private const int EXIT_INVALID_ARGUMENT = 4;

    // Default simulator config path
    private const string DefaultSimulatorConfigPath = "task_helper/data_test/dist/simulator_config.json";

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

        // ============================================================
        // setup 명령: SetupWindow 제어
        // ============================================================

        var setupCommand = new Command("setup", "SetupWindow 제어");

        // setup verify-config: 데이터 시뮬레이터와 ChronoView 설정 비교
        var configPathOption = new Option<string?>(
            ["--config-path", "-c"],
            () => DefaultSimulatorConfigPath,
            "시뮬레이터 설정 파일 경로"
        );
        var openSettingsOption = new Option<bool>(
            ["--open-settings", "-o"],
            "SettingsDialog가 열려있지 않으면 자동으로 열기"
        );
        var strictOption = new Option<bool>(
            ["--strict", "-s"],
            "설정이 일치하지 않으면 오류로 처리"
        );

        var verifyConfigCommand = new Command("verify-config", "시뮬레이터와 ChronoView 설정 비교");
        verifyConfigCommand.AddOption(configPathOption);
        verifyConfigCommand.AddOption(openSettingsOption);
        verifyConfigCommand.AddOption(strictOption);
        verifyConfigCommand.AddOption(jsonOption);
        verifyConfigCommand.SetHandler((configPath, openSettings, strict, json) =>
        {
            var actualConfigPath = configPath ?? DefaultSimulatorConfigPath;
            using var verifier = new SetupVerifier(actualConfigPath);

            var result = verifier.Verify(openSettingsIfNeeded: openSettings);

            if (json)
            {
                Console.WriteLine(result.ToJson());
            }
            else
            {
                PrintVerificationResult(result);
            }

            // Determine exit code
            int exitCode = EXIT_SUCCESS;
            if (!result.Success)
            {
                exitCode = strict ? EXIT_ERROR : EXIT_SUCCESS;
            }

            // Handle missing config file
            if (result.Error == "Simulator config file not found")
            {
                exitCode = EXIT_NOT_FOUND;
            }

            Environment.Exit(exitCode);
        }, configPathOption, openSettingsOption, strictOption, jsonOption);
        setupCommand.AddCommand(verifyConfigCommand);

        // setup complete-full: Complete setup workflow with optional config verification
        var verifyConfigIntegrationOption = new Option<bool>(
            ["--verify-config", "-v"],
            "설정 검증을 먼저 수행"
        );
        var configPathForIntegrationOption = new Option<string?>(
            ["--config-path", "-c"],
            () => DefaultSimulatorConfigPath,
            "검증에 사용할 시뮬레이터 설정 파일 경로"
        );
        var strictIntegrationOption = new Option<bool>(
            ["--strict"],
            "설정 검증 실패 시 중단"
        );

        var completeFullCommand = new Command("complete-full", "완전한 설정 워크플로우 실행 (카메라 실행 + 시작)");
        completeFullCommand.AddOption(verifyConfigIntegrationOption);
        completeFullCommand.AddOption(configPathForIntegrationOption);
        completeFullCommand.AddOption(strictIntegrationOption);
        completeFullCommand.AddOption(jsonOption);
        completeFullCommand.SetHandler((verifyConfig, configPath, strict, json) =>
        {
            // Step 1: Config verification (if requested)
            if (verifyConfig)
            {
                Console.WriteLine("[setup complete-full] Running configuration verification...");

                var actualConfigPath = configPath ?? DefaultSimulatorConfigPath;
                using var verifier = new SetupVerifier(actualConfigPath);
                var verifyResult = verifier.Verify(openSettingsIfNeeded: true);

                if (json)
                {
                    Console.WriteLine(verifyResult.ToJson());
                }
                else
                {
                    PrintVerificationResult(verifyResult);
                }

                // Check if verification failed
                if (!verifyResult.Success)
                {
                    Console.WriteLine("[setup complete-full] Configuration verification failed");

                    if (strict)
                    {
                        Console.WriteLine("[setup complete-full] Aborting due to strict mode");
                        Environment.Exit(EXIT_ERROR);
                        return;
                    }
                    else
                    {
                        Console.WriteLine("[setup complete-full] Continuing despite mismatches (use --strict to abort)");
                    }
                }
                else
                {
                    Console.WriteLine("[setup complete-full] Configuration verification passed");
                }
            }

            // Step 2: Execute complete-full workflow
            using var controller = new SetupController();

            // Find SetupWindow
            var setupWindow = controller.FindSetupWindow();
            if (setupWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "SetupWindow not found"
                    });
                }
                else
                {
                    Console.WriteLine("[setup complete-full] SetupWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            // Launch cameras
            Console.WriteLine("[setup complete-full] Launching cameras...");
            controller.ClickGeneralCamera();
            controller.ClickNir1();
            controller.ClickNir2();

            // Toggle NIR filtering if needed
            var states = controller.GetCameraStates();
            string nirFilteringState = states.TryGetValue("NirFiltering", out var nirVal) ? nirVal.ToString() : "unknown";
            Console.WriteLine($"[setup complete-full] Camera states: NirFiltering={nirFilteringState}");

            // Click Start button
            Console.WriteLine("[setup complete-full] Clicking Start button...");
            bool startSuccess = controller.ClickStartButton();

            if (!startSuccess)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "Failed to click Start button"
                    });
                }
                else
                {
                    Console.WriteLine("[setup complete-full] Failed to click Start button");
                }
                Environment.Exit(EXIT_ERROR);
                return;
            }

            // Wait for MainWindow
            Console.WriteLine("[setup complete-full] Waiting for MainWindow...");
            var mainWindow = controller.WaitForMainWindow();
            bool mainWindowAppeared = mainWindow != null;

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = mainWindowAppeared,
                    data = new
                    {
                        completed = true,
                        configVerified = verifyConfig,
                        mainWindowAppeared = mainWindowAppeared
                    }
                });
            }
            else
            {
                if (mainWindowAppeared)
                {
                    Console.WriteLine("[setup complete-full] Setup completed successfully, MainWindow is now active");
                }
                else
                {
                    Console.WriteLine("[setup complete-full] Start clicked but MainWindow did not appear");
                }
            }

            Environment.Exit(mainWindowAppeared ? EXIT_SUCCESS : EXIT_TIMEOUT);
        }, verifyConfigIntegrationOption, configPathForIntegrationOption, strictIntegrationOption, jsonOption);
        setupCommand.AddCommand(completeFullCommand);

        // setup open-settings: Open SettingsDialog from SetupWindow
        var openSettingsCommand = new Command("open-settings", "SetupWindow에서 설정 다이얼로그 열기");
        openSettingsCommand.AddOption(jsonOption);
        openSettingsCommand.SetHandler((json) =>
        {
            using var controller = new SetupController();

            // Find SetupWindow first
            var setupWindow = controller.FindSetupWindow();
            if (setupWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "SetupWindow not found"
                    });
                }
                else
                {
                    Console.WriteLine("[setup open-settings] SetupWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            // Click the Settings button
            bool success = controller.ClickSettingsButton();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = success,
                    data = new
                    {
                        settingsOpened = success
                    }
                });
            }
            else
            {
                if (success)
                {
                    Console.WriteLine("[setup open-settings] SettingsDialog opened successfully");
                }
                else
                {
                    Console.WriteLine("[setup open-settings] Failed to open SettingsDialog");
                }
            }

            Environment.Exit(success ? EXIT_SUCCESS : EXIT_ERROR);
        }, jsonOption);
        setupCommand.AddCommand(openSettingsCommand);

        // setup camera-states: Get camera button states from SetupWindow
        var cameraStatesCommand = new Command("camera-states", "카메라 버튼 상태 확인 (general, nir1, nir2)");
        cameraStatesCommand.AddOption(jsonOption);
        cameraStatesCommand.SetHandler((json) =>
        {
            using var controller = new SetupController();

            // Find SetupWindow first
            var setupWindow = controller.FindSetupWindow();
            if (setupWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "SetupWindow not found"
                    });
                }
                else
                {
                    Console.WriteLine("[setup camera-states] SetupWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            // Get camera states
            var states = controller.GetCameraStates();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        general = states.TryGetValue("general", out var general) ? general : false,
                        nir1 = states.TryGetValue("nir1", out var nir1) ? nir1 : false,
                        nir2 = states.TryGetValue("nir2", out var nir2) ? nir2 : false
                    }
                });
            }
            else
            {
                Console.WriteLine("[setup camera-states] Camera button states:");
                Console.WriteLine($"  General Camera: {(states.TryGetValue("general", out var g) && g ? "Enabled" : "Disabled")}");
                Console.WriteLine($"  NIR1 Camera: {(states.TryGetValue("nir1", out var n1) && n1 ? "Enabled" : "Disabled")}");
                Console.WriteLine($"  NIR2 Camera: {(states.TryGetValue("nir2", out var n2) && n2 ? "Enabled" : "Disabled")}");
            }

            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        setupCommand.AddCommand(cameraStatesCommand);

        rootCommand.AddCommand(setupCommand);
    }

    /// <summary>
    /// Prints verification result in human-readable format.
    /// </summary>
    private static void PrintVerificationResult(VerificationResult result)
    {
        if (result.Error != null)
        {
            Console.WriteLine($"[setup verify-config] Error: {result.Error}");
            return;
        }

        Console.WriteLine($"[setup verify-config] Result: {(result.Success ? "PASS" : "FAIL")}");

        if (result.Matched.Count > 0)
        {
            Console.WriteLine($"  Matched ({result.Matched.Count}):");
            foreach (var match in result.Matched)
            {
                Console.WriteLine($"    - {match}");
            }
        }

        if (result.Mismatches.Count > 0)
        {
            Console.WriteLine($"  Mismatches ({result.Mismatches.Count}):");
            foreach (var mismatch in result.Mismatches)
            {
                Console.WriteLine($"    - {mismatch.SimulatorKey} vs {mismatch.ChronoViewKey}");
                Console.WriteLine($"      Simulator: {mismatch.SimulatorPath}");
                Console.WriteLine($"      ChronoView: {mismatch.ChronoViewPath}");
                Console.WriteLine($"      Reason: {mismatch.Reason}");
            }
        }

        if (result.Missing.Count > 0)
        {
            Console.WriteLine($"  Missing ({result.Missing.Count}):");
            foreach (var missing in result.Missing)
            {
                Console.WriteLine($"    - {missing}");
            }
        }
    }

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
}
