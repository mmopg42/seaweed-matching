using System.CommandLine;
using System.Linq;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;

namespace UiAutomation.Commands;

/// <summary>
/// Windows commands for ChronoView window detection (ChronoWindowFinder).
/// Provides commands to find and interact with ChronoView windows.
/// </summary>
public class WindowsCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_TIMEOUT = 3;

    /// <summary>
    /// Registers all windows commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // JSON output option
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // windows 명령: ChronoView 윈도우 찾기 (ChronoWindowFinder 사용)
        var windowsCommand = new Command("windows", "ChronoView 윈도우 찾기");

        // windows main: MainWindow 찾기
        var mainCommand = new Command("main", "ChronoView MainWindow 찾기");
        mainCommand.AddOption(jsonOption);
        mainCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var window = finder.FindMainWindow();

            if (window != null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = true,
                        data = new
                        {
                            found = true,
                            windowType = "MainWindow",
                            title = window.Name,
                            className = window.ClassName,
                            automationId = window.AutomationId
                        }
                    });
                }
                else
                {
                    PrintOutput($"[MainWindow] Found: '{window.Name}'");
                    PrintOutput($"  - ClassName: {window.ClassName ?? "(null)"}");
                    PrintOutput($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "MainWindow not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    PrintOutput("[MainWindow] Not found - make sure ChronoView is running");
                }
                Environment.Exit(EXIT_NOT_FOUND);
            }
        }, jsonOption);
        windowsCommand.AddCommand(mainCommand);

        // windows setup: SetupWindow 찾기
        var setupCommand = new Command("setup", "SetupWindow 찾기");
        setupCommand.AddOption(jsonOption);
        setupCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var window = finder.FindSetupWindow();

            if (window != null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = true,
                        data = new
                        {
                            found = true,
                            windowType = "SetupWindow",
                            title = window.Name,
                            className = window.ClassName,
                            automationId = window.AutomationId
                        }
                    });
                }
                else
                {
                    PrintOutput($"[SetupWindow] Found: '{window.Name}'");
                    PrintOutput($"  - ClassName: {window.ClassName ?? "(null)"}");
                    PrintOutput($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "SetupWindow not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    PrintOutput("[SetupWindow] Not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
            }
        }, jsonOption);
        windowsCommand.AddCommand(setupCommand);

        // windows setup-complete: SetupWindow 완료 (시작 버튼 클릭)
        var setupCompleteCommand = new Command("setup-complete", "SetupWindow의 시작 버튼 클릭하여 완료");
        setupCompleteCommand.AddOption(jsonOption);
        setupCompleteCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var setupWindow = finder.FindSetupWindow();

            if (setupWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "SetupWindow not found",
                        hint = "ChronoView may not be at setup screen"
                    });
                }
                else
                {
                    PrintOutput("[setup-complete] SetupWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            // "모니터링 프로그램 시작" 버튼 찾기
            var startButton = setupWindow.FindFirstDescendant(cf => cf.ByText("모니터링 프로그램 시작")
                .Or(cf.ByName("모니터링 프로그램 시작")))?.AsButton();

            if (startButton == null)
            {
                // Fallback: ControlType.Button으로 모든 버튼 검색
                var allButtons = setupWindow.FindAllChildren(cf => cf.ByControlType(ControlType.Button));
                foreach (var btn in allButtons)
                {
                    var name = btn.Name;
                    if (!string.IsNullOrEmpty(name) && name.Contains("모니터링"))
                    {
                        startButton = btn.AsButton();
                        break;
                    }
                }
            }

            if (startButton == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "Start button not found in SetupWindow",
                        hint = "Button text may have changed"
                    });
                }
                else
                {
                    PrintOutput("[setup-complete] Start button not found");
                }
                Environment.Exit(EXIT_ERROR);
                return;
            }

            // 버튼 클릭
            startButton.Click();
            PrintVerbose("[setup-complete] Start button clicked");

            // MainWindow가 나타날 때까지 대기 (최대 10초)
            var mainWindow = finder.WaitForWindow("ChronoView Pro", 10000);
            bool success = mainWindow != null;

            if (json)
            {
                PrintJsonOutput(new
                {
                    success,
                    data = success ? new
                    {
                        completed = true,
                        mainWindowFound = true,
                        mainWindowTitle = mainWindow?.Name
                    } : null,
                    error = success ? null : "MainWindow did not appear after clicking start"
                });
            }
            else
            {
                if (success)
                {
                    PrintOutput($"[setup-complete] Setup completed, MainWindow found: '{mainWindow?.Name}'");
                }
                else
                {
                    PrintOutput("[setup-complete] Start button clicked but MainWindow did not appear");
                }
            }

            Environment.Exit(success ? EXIT_SUCCESS : EXIT_TIMEOUT);
        }, jsonOption);
        windowsCommand.AddCommand(setupCompleteCommand);

        // windows settings: SettingsDialog 찾기
        var settingsCommand = new Command("settings", "SettingsDialog 찾기");
        settingsCommand.AddOption(jsonOption);
        settingsCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var window = finder.FindSettingsDialog();

            if (window != null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = true,
                        data = new
                        {
                            found = true,
                            windowType = "SettingsDialog",
                            title = window.Name,
                            className = window.ClassName,
                            automationId = window.AutomationId
                        }
                    });
                }
                else
                {
                    PrintOutput($"[SettingsDialog] Found: '{window.Name}'");
                    PrintOutput($"  - ClassName: {window.ClassName ?? "(null)"}");
                    PrintOutput($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "SettingsDialog not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    PrintOutput("[SettingsDialog] Not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
            }
        }, jsonOption);
        windowsCommand.AddCommand(settingsCommand);

        // windows preview: ImagePreviewWindow 찾기
        var previewCommand = new Command("preview", "ImagePreviewWindow 찾기");
        previewCommand.AddOption(jsonOption);
        previewCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var window = finder.FindImagePreviewWindow();

            if (window != null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = true,
                        data = new
                        {
                            found = true,
                            windowType = "ImagePreviewWindow",
                            title = window.Name,
                            className = window.ClassName,
                            automationId = window.AutomationId
                        }
                    });
                }
                else
                {
                    PrintOutput($"[ImagePreviewWindow] Found: '{window.Name}'");
                    PrintOutput($"  - ClassName: {window.ClassName ?? "(null)"}");
                    PrintOutput($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "ImagePreviewWindow not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    PrintOutput("[ImagePreviewWindow] Not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
            }
        }, jsonOption);
        windowsCommand.AddCommand(previewCommand);

        // windows all: 모든 ChronoView 윈도우 나열
        var allCommand = new Command("all", "모든 ChronoView 윈도우 나열");
        allCommand.AddOption(jsonOption);
        allCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var windows = finder.FindAllChronoViewWindows();

            if (json)
            {
                var windowList = windows.Select(w => new
                {
                    title = w.Name,
                    className = w.ClassName,
                    automationId = TryGetAutomationId(w)
                });
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        count = windows.Count,
                        windows = windowList
                    }
                });
            }
            else
            {
                PrintOutput($"[All ChronoView Windows] Found: {windows.Count}");
                foreach (var window in windows)
                {
                    PrintOutput($"  - '{window.Name}'");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        windowsCommand.AddCommand(allCommand);

        rootCommand.AddCommand(windowsCommand);
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

    /// <summary>
    /// Print output only if not in quiet mode
    /// </summary>
    private static void PrintOutput(string message)
    {
        // Access global quiet state through reflection or pass as parameter
        // For now, always print (quiet mode handled at Program.cs level)
        Console.WriteLine(message);
    }

    /// <summary>
    /// Print verbose output only if verbose mode is enabled
    /// </summary>
    private static void PrintVerbose(string message)
    {
        // Access global verbose state through reflection or pass as parameter
        // For now, always print (quiet mode handled at Program.cs level)
        Console.WriteLine($"[VERBOSE] {message}");
    }

    /// <summary>
    /// Get AutomationId safely, catching PropertyNotSupportedException
    /// </summary>
    private static string? TryGetAutomationId(AutomationElement element)
    {
        try
        {
            return element.AutomationId;
        }
        catch (FlaUI.Core.Exceptions.PropertyNotSupportedException)
        {
            return null;
        }
    }
}
