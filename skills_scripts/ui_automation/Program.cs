using System.CommandLine;
using System.Text.Json;
using System.Linq;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;
using Workflow = SkillsScripts.UiAutomation.ChronoWorkflowController;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;
using Settings = SkillsScripts.UiAutomation.ChronoSettingsController;
using FileOps = SkillsScripts.UiAutomation.ChronoFileOperationsController;

namespace UiAutomation;

/// <summary>
/// Windows UI Automation CLI - FlaUI 5.x 기반
/// </summary>
class Program
{
    // Exit code constants for agent consumption
    const int EXIT_SUCCESS = 0;
    const int EXIT_ERROR = 1;
    const int EXIT_NOT_FOUND = 2;
    const int EXIT_TIMEOUT = 3;
    const int EXIT_INVALID_ARGUMENT = 4;

    // Global options state
    static bool s_isQuiet = false;
    static bool s_isVerbose = false;

    /// <summary>
    /// Print JSON output with consistent formatting for programmatic consumption
    /// </summary>
    static void PrintJsonOutput(object data)
    {
        Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        }));
    }

    /// <summary>
    /// Print error message and return exit code
    /// </summary>
    static int PrintError(string message, int exitCode = EXIT_ERROR)
    {
        Console.Error.WriteLine(message);
        return exitCode;
    }

    /// <summary>
    /// Print output only if not in quiet mode
    /// </summary>
    static void PrintOutput(string message)
    {
        if (!s_isQuiet)
        {
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Print verbose output only if verbose mode is enabled
    /// </summary>
    static void PrintVerbose(string message)
    {
        if (s_isVerbose && !s_isQuiet)
        {
            Console.WriteLine($"[VERBOSE] {message}");
        }
    }
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Windows UI Automation - FlaUI 5.x 기반 CLI 도구");

        // Global options for agent control
        var quietOption = new Option<bool>(
            ["--quiet", "-q"],
            "Suppress all non-error output (for agent consumption)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            "Enable verbose output for debugging")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        // Add global options to root
        rootCommand.AddGlobalOption(quietOption);
        rootCommand.AddGlobalOption(verboseOption);

        // JSON output option
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // detect 명령: ChronoView MainWindow 감지 및 정보 출력
        var detectCommand = new Command("detect", "ChronoView MainWindow 감지 및 속성 출력");
        detectCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            automation.PrintMainWindowInfo();
        });
        rootCommand.AddCommand(detectCommand);

        // list 명령: 모든 윈도우 나열
        var listCommand = new Command("list", "모든 윈도우 나열");
        listCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var windows = automation.FindAllChronoViewWindows();
            Console.WriteLine($"Found {windows.Count} ChronoView window(s)");
        });
        rootCommand.AddCommand(listCommand);

        // find 명령: 윈도우 찾기
        var titleOption = new Option<string>(
            ["--title", "-t"],
            "윈도우 제목으로 검색"
        );
        var processOption = new Option<string>(
            ["--process", "-p"],
            "프로세스 이름으로 검색"
        );

        var findCommand = new Command("find", "윈도우 찾기");
        findCommand.AddOption(titleOption);
        findCommand.AddOption(processOption);
        findCommand.SetHandler((title, process) =>
        {
            using var automation = new UiAuto();

            if (!string.IsNullOrEmpty(title))
            {
                Console.WriteLine($"Finding window by title: {title}");
                var window = automation.FindWindowByTitle(title, substring: true);
                if (window != null)
                {
                    Console.WriteLine($"Found: '{window.Name}'");
                    automation.GetWindowProperties(window);
                }
                else
                {
                    Console.WriteLine("Window not found");
                }
            }
            else if (!string.IsNullOrEmpty(process))
            {
                Console.WriteLine($"Finding window by process: {process}");
                var window = automation.FindWindowByProcess(process);
                if (window != null)
                {
                    Console.WriteLine($"Found: '{window.Name}'");
                    automation.GetWindowProperties(window);
                }
                else
                {
                    Console.WriteLine("Window not found");
                }
            }
            else
            {
                Console.WriteLine("Please specify --title or --process option");
            }
        }, titleOption, processOption);
        rootCommand.AddCommand(findCommand);

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
                    Console.WriteLine($"[MainWindow] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
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
                    Console.WriteLine("[MainWindow] Not found - make sure ChronoView is running");
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
                    Console.WriteLine($"[SetupWindow] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
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
                    Console.WriteLine("[SetupWindow] Not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
            }
        }, jsonOption);
        windowsCommand.AddCommand(setupCommand);

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
                    Console.WriteLine($"[SettingsDialog] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
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
                    Console.WriteLine("[SettingsDialog] Not found");
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
                    Console.WriteLine($"[ImagePreviewWindow] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
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
                    Console.WriteLine("[ImagePreviewWindow] Not found");
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
                    automationId = w.AutomationId
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
                Console.WriteLine($"[All ChronoView Windows] Found: {windows.Count}");
                foreach (var window in windows)
                {
                    Console.WriteLine($"  - '{window.Name}'");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        windowsCommand.AddCommand(allCommand);

        rootCommand.AddCommand(windowsCommand);

        // click 명령: Toolbar 버튼 클릭
        var clickCommand = new Command("click", "Toolbar 버튼 클릭");

        // click start: Start 버튼 클릭
        var clickStartCommand = new Command("start", "Start 버튼 클릭");
        clickStartCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-start] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickStartButton(mainWindow);
            Console.WriteLine(result ? "[click-start] Success: Start button clicked" : "[click-start] Failed: Could not click Start button");
        });
        clickCommand.AddCommand(clickStartCommand);

        // click stop: Stop 버튼 클릭
        var clickStopCommand = new Command("stop", "Stop 버튼 클릭");
        clickStopCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-stop] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickStopButton(mainWindow);
            Console.WriteLine(result ? "[click-stop] Success: Stop button clicked" : "[click-stop] Failed: Could not click Stop button");
        });
        clickCommand.AddCommand(clickStopCommand);

        // click settings: Settings (Setup) 버튼 클릭
        var clickSettingsCommand = new Command("settings", "Settings (Setup) 버튼 클릭");
        clickSettingsCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-settings] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickSettingsButton(mainWindow);
            if (result)
            {
                Console.WriteLine("[click-settings] Success: Settings button clicked, SetupWindow should open");
            }
            else
            {
                Console.WriteLine("[click-settings] Failed: Could not click Settings button");
            }
        });
        clickCommand.AddCommand(clickSettingsCommand);

        // click refresh: Refresh 버튼 클릭
        var clickRefreshCommand = new Command("refresh", "Refresh 버튼 클릭");
        clickRefreshCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-refresh] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickRefreshButton(mainWindow);
            Console.WriteLine(result ? "[click-refresh] Success: Refresh button clicked" : "[click-refresh] Failed: Could not click Refresh button");
        });
        clickCommand.AddCommand(clickRefreshCommand);

        // click move: Move 버튼 클릭
        var clickMoveCommand = new Command("move", "Move 버튼 클릭");
        clickMoveCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-move] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickMoveButton(mainWindow);
            Console.WriteLine(result ? "[click-move] Success: Move button clicked" : "[click-move] Failed: Could not click Move button");
        });
        clickCommand.AddCommand(clickMoveCommand);

        // click delete: Delete 버튼 클릭
        var clickDeleteCommand = new Command("delete", "Delete 버튼 클릭");
        clickDeleteCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-delete] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickDeleteButton(mainWindow);
            Console.WriteLine(result ? "[click-delete] Success: Delete button clicked" : "[click-delete] Failed: Could not click Delete button");
        });
        clickCommand.AddCommand(clickDeleteCommand);

        rootCommand.AddCommand(clickCommand);

        // toolbar 명령: ChronoToolbarController 기반 통합 툴바 컨트롤
        var toolbarCommand = new Command("toolbar", "툴바 버튼 제어 (ChronoToolbarController)");

        // toolbar start: Start 버튼 클릭
        var toolbarStartCommand = new Command("start", "Start 버튼 클릭");
        toolbarStartCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickStartButton();
            if (result)
            {
                Console.WriteLine("[toolbar-start] Success: Start button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[toolbar-start] Failed: Could not click Start button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        toolbarCommand.AddCommand(toolbarStartCommand);

        // toolbar stop: Stop 버튼 클릭
        var toolbarStopCommand = new Command("stop", "Stop 버튼 클릭");
        toolbarStopCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickStopButton();
            if (result)
            {
                Console.WriteLine("[toolbar-stop] Success: Stop button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[toolbar-stop] Failed: Could not click Stop button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        toolbarCommand.AddCommand(toolbarStopCommand);

        // toolbar settings: Settings (Setup) 버튼 클릭
        var toolbarSettingsCommand = new Command("settings", "Settings (Setup) 버튼 클릭");
        toolbarSettingsCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickSettingsButton();
            if (result)
            {
                Console.WriteLine("[toolbar-settings] Success: Settings button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[toolbar-settings] Failed: Could not click Settings button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        toolbarCommand.AddCommand(toolbarSettingsCommand);

        // toolbar refresh: Refresh 버튼 클릭
        var toolbarRefreshCommand = new Command("refresh", "Refresh 버튼 클릭");
        toolbarRefreshCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickRefreshButton();
            if (result)
            {
                Console.WriteLine("[toolbar-refresh] Success: Refresh button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[toolbar-refresh] Failed: Could not click Refresh button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        toolbarCommand.AddCommand(toolbarRefreshCommand);

        // toolbar move: Move 버튼 클릭
        var toolbarMoveCommand = new Command("move", "Move 버튼 클릭");
        toolbarMoveCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickMoveButton();
            if (result)
            {
                Console.WriteLine("[toolbar-move] Success: Move button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[toolbar-move] Failed: Could not click Move button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        toolbarCommand.AddCommand(toolbarMoveCommand);

        // toolbar delete: Delete 버튼 클릭
        var toolbarDeleteCommand = new Command("delete", "Delete 버튼 클릭");
        toolbarDeleteCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickDeleteButton();
            if (result)
            {
                Console.WriteLine("[toolbar-delete] Success: Delete button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[toolbar-delete] Failed: Could not click Delete button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        toolbarCommand.AddCommand(toolbarDeleteCommand);

        // toolbar list: 모든 툴바 버튼 나열
        var toolbarListCommand = new Command("list", "모든 툴바 버튼 나열");
        toolbarListCommand.AddOption(jsonOption);
        toolbarListCommand.SetHandler((json) =>
        {
            using var controller = new Toolbar();
            var buttons = controller.GetAvailableButtons();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        count = buttons.Length,
                        buttons = buttons
                    }
                });
            }
            else
            {
                Console.WriteLine($"[toolbar-list] Found {buttons.Length} toolbar button(s):");
                foreach (var button in buttons)
                {
                    Console.WriteLine($"  - '{button}'");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        toolbarCommand.AddCommand(toolbarListCommand);

        // toolbar click: 지정한 텍스트의 버튼 클릭
        var buttonTextArgument = new Argument<string>("text", "버튼 텍스트 (예: '시작', '중지', '설정')");
        var toolbarClickCommand = new Command("click", "지정한 텍스트의 버튼 클릭");
        toolbarClickCommand.AddArgument(buttonTextArgument);
        toolbarClickCommand.SetHandler((text) =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickToolbarButton(text);
            if (result)
            {
                Console.WriteLine($"[toolbar-click] Success: Button '{text}' clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine($"[toolbar-click] Failed: Could not click button '{text}'");
                Environment.Exit(EXIT_ERROR);
            }
        }, buttonTextArgument);
        toolbarCommand.AddCommand(toolbarClickCommand);

        // toolbar enabled: 버튼 활성화 상태 확인
        var toolbarEnabledCommand = new Command("enabled", "버튼 활성화 상태 확인");
        toolbarEnabledCommand.AddArgument(buttonTextArgument);
        toolbarEnabledCommand.SetHandler((text) =>
        {
            using var controller = new Toolbar();
            var isEnabled = controller.IsButtonEnabled(text);
            Console.WriteLine(isEnabled ? $"[toolbar-enabled] Button '{text}' is enabled" : $"[toolbar-enabled] Button '{text}' is disabled");
            Environment.Exit(EXIT_SUCCESS);
        }, buttonTextArgument);
        toolbarCommand.AddCommand(toolbarEnabledCommand);

        rootCommand.AddCommand(toolbarCommand);

        // stats 명령: StatisticsPanel 데이터 읽기
        var statsCommand = new Command("stats", "StatisticsPanel 데이터 읽기");
        statsCommand.AddOption(jsonOption);
        statsCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
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
                    Console.WriteLine("[stats] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var statistics = automation.GetAllStatistics(mainWindow);
            if (statistics == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "StatisticsPanel not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    Console.WriteLine("[stats] Failed: Could not extract statistics (StatisticsPanel not found)");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "StatisticsPanel",
                        statistics = statistics
                    }
                });
            }
            else
            {
                Console.WriteLine("[stats] Statistics from StatisticsPanel:");
                Console.WriteLine("\nFile Counts:");
                foreach (var kvp in statistics.Where(k => k.Key.StartsWith("NIR") || k.Key.StartsWith("Normal") || k.Key.StartsWith("Cam")))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine("\nMatching Status:");
                foreach (var kvp in statistics.Where(k => !k.Key.StartsWith("NIR") && !k.Key.StartsWith("Normal") && !k.Key.StartsWith("Cam") && k.Key != "일반2"))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        rootCommand.AddCommand(statsCommand);

        // datagrid 명령: DataGrid 데이터 읽기
        var datagridCommand = new Command("datagrid", "DataGrid 데이터 읽기");

        // datagrid headers: DataGrid 헤더 읽기
        var dgHeadersCommand = new Command("headers", "DataGrid 헤더 읽기");
        dgHeadersCommand.AddOption(jsonOption);
        dgHeadersCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-headers] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-headers] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var headers = automation.GetDataGridHeaders(dataGrid);
            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { columnCount = headers.Count, columns = headers }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-headers] Found {headers.Count} columns:");
                foreach (var header in headers)
                {
                    Console.WriteLine($"  - {header}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgHeadersCommand);

        // datagrid rows: 데이터 행 개수 확인
        var dgRowsCommand = new Command("rows", "데이터 행 개수 확인");
        dgRowsCommand.AddOption(jsonOption);
        dgRowsCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-rows] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-rows] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var rowCount = automation.GetDataRowCount(dataGrid);
            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { rowCount = rowCount }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-rows] DataGrid has {rowCount} data rows");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgRowsCommand);

        // datagrid data: 모든 데이터 추출
        var dgDataCommand = new Command("data", "모든 DataGrid 데이터 추출");
        dgDataCommand.AddOption(jsonOption);
        dgDataCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-data] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var allData = automation.GetAllDataGridData(mainWindow);
            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { rowCount = allData.Count, data = allData }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-data] Extracted {allData.Count} rows");
                if (allData.Count > 0)
                {
                    var headers = allData[0].Keys.ToList();
                    Console.WriteLine("  Headers: " + string.Join(", ", headers));
                    foreach (var row in allData)
                    {
                        Console.WriteLine("  Row: " + string.Join(" | ", row.Values));
                    }
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgDataCommand);

        // datagrid info: Show headers and row count
        var dgInfoCommand = new Command("info", "DataGrid 헤더 및 행 개수 요약");
        dgInfoCommand.AddOption(jsonOption);
        dgInfoCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-info] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-info] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var headers = automation.GetDataGridHeaders(dataGrid);
            var rowCount = automation.GetDataRowCount(dataGrid);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { columnCount = headers.Count, rowCount = rowCount, columns = headers }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-info] DataGrid: {headers.Count} columns, {rowCount} rows");
                Console.WriteLine("  Columns: " + string.Join(", ", headers));
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgInfoCommand);

        // datagrid cell: Get specific cell value
        var rowArgument = new Argument<int>("row", "행 인덱스 (0-based)");
        var colArgument = new Argument<int>("col", "열 인덱스 (0-based)");
        var dgCellCommand = new Command("cell", "특정 셀 값 가져오기");
        dgCellCommand.AddArgument(rowArgument);
        dgCellCommand.AddArgument(colArgument);
        dgCellCommand.AddOption(jsonOption);
        dgCellCommand.SetHandler((row, col, json) =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-cell] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-cell] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var cf = automation.GetAutomation().ConditionFactory;
            var rows = dataGrid.FindAllChildren(cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem));

            if (row < 0 || row >= rows.Length)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = $"Row index {row} out of range", errorCode = EXIT_INVALID_ARGUMENT });
                }
                else
                {
                    Console.WriteLine($"[datagrid-cell] Failed: Row index {row} out of range (0-{rows.Length - 1})");
                }
                Environment.Exit(EXIT_INVALID_ARGUMENT);
                return;
            }

            var rowElement = rows[row];
            var cellText = automation.GetCellText(rowElement, col);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = cellText != null,
                    data = new { row = row, column = col, value = cellText }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-cell] Row {row}, Column {col}: '{cellText ?? "(null)"}'");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, rowArgument, colArgument, jsonOption);
        datagridCommand.AddCommand(dgCellCommand);

        // datagrid export: Export all data as JSON
        var dgExportCommand = new Command("export", "모든 DataGrid 데이터를 JSON으로 내보내기");
        dgExportCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[datagrid-export] Failed: MainWindow not found");
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var allData = automation.GetAllDataGridData(mainWindow);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                success = true,
                data = new
                {
                    rowCount = allData.Count,
                    exportedAt = DateTime.UtcNow.ToString("o"),
                    data = allData
                }
            }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.Exit(EXIT_SUCCESS);
        });
        datagridCommand.AddCommand(dgExportCommand);

        rootCommand.AddCommand(datagridCommand);

        // inspect 명령: UI 요소 구조 검사
        var inspectCommand = new Command("inspect", "UI 요소 구조 검사");

        // inspect workflow: WorkflowPanel 구조 검사
        var inspectWorkflowCommand = new Command("workflow", "WorkflowPanel 구조 검사");
        inspectWorkflowCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[inspect-workflow] Failed: MainWindow not found");
                return;
            }

            var workflowPanel = automation.FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[inspect-workflow] Failed: WorkflowPanel not found");
                return;
            }

            Console.WriteLine("[inspect-workflow] WorkflowPanel found - listing element tree (depth=3):");
            Console.WriteLine();
            Console.WriteLine("=== Key Elements to Identify ===");
            Console.WriteLine("  - Camera status buttons (General, NIR, NIR2, NIR Filtering)");
            Console.WriteLine("  - Path TextBox controls (Line1SampleName, Line1MoveNir, Line1MoveAllData, etc.)");
            Console.WriteLine("  - Expander headers (Camera Status, Sample Move Settings, Data Status)");
            Console.WriteLine();
            Console.WriteLine("=== Element Tree ===");
            automation.ListElements(workflowPanel, maxDepth: 3);
        });
        inspectCommand.AddCommand(inspectWorkflowCommand);

        // inspect log: LogPanel 구조 검사
        var inspectLogCommand = new Command("log", "LogPanel 구조 검사");
        inspectLogCommand.SetHandler(() =>
        {
            using var reader = new SkillsScripts.UiAutomation.ChronoDataPanelReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[inspect-log] Failed: MainWindow not found");
                return;
            }

            var logPanel = reader.FindLogPanel(mainWindow);
            if (logPanel == null)
            {
                Console.WriteLine("[inspect-log] Failed: LogPanel not found");
                return;
            }

            Console.WriteLine("[inspect-log] LogPanel found - listing element tree (depth=2):");
            Console.WriteLine();
            Console.WriteLine("=== Key Elements to Identify ===");
            Console.WriteLine("  - LogDataGrid (DataGrid with Severity, Time, Source, Message columns)");
            Console.WriteLine("  - SearchBox (TextBox for search filtering)");
            Console.WriteLine("  - LevelFilter (ComboBox with: All, Debug, Info, Warning, Error)");
            Console.WriteLine("  - AutoScrollCheckBox (CheckBox for auto-scroll toggle)");
            Console.WriteLine("  - Action buttons (Clear, QuickSave, OpenLogFolder, Close)");
            Console.WriteLine();

            // Get log summary
            var logDataGrid = reader.FindLogDataGrid(logPanel);
            if (logDataGrid != null)
            {
                Console.WriteLine("=== LogDataGrid Summary ===");
                Console.WriteLine($"  - ControlType: {logDataGrid.ControlType}");
                Console.WriteLine($"  - Name: '{logDataGrid.Name ?? "(unnamed)"}'");
                Console.WriteLine($"  - ClassName: '{logDataGrid.ClassName ?? "(null)"}'");
                Console.WriteLine($"  - AutomationId: '{logDataGrid.AutomationId ?? "(null)"}'");

                var headers = reader.GetLogHeaders(logPanel);
                if (headers.Count > 0)
                {
                    Console.WriteLine($"  - Columns: {string.Join(", ", headers)}");
                }

                var rowCount = reader.GetLogRowCount(logPanel);
                Console.WriteLine($"  - Row count: {rowCount}");
                Console.WriteLine();
            }

            Console.WriteLine("=== Element Tree (depth=2) ===");
            using var automation = new UiAuto();
            automation.ListElements(logPanel, maxDepth: 2);
        });
        inspectCommand.AddCommand(inspectLogCommand);

        rootCommand.AddCommand(inspectCommand);

        // workflow 명령: ChronoWorkflowController 기반 워크플로우 패널 제어
        var workflowCommand = new Command("workflow", "WorkflowPanel 제어 (ChronoWorkflowController)");

        // workflow launch-general: General Camera 버튼 클릭
        var wfLaunchGeneralCommand = new Command("launch-general", "General Camera 버튼 클릭");
        wfLaunchGeneralCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickGeneralCameraButton();
            if (result)
            {
                Console.WriteLine("[workflow-launch-general] Success: General Camera button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-launch-general] Failed: Could not click General Camera button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfLaunchGeneralCommand);

        // workflow launch-nir: NIR 1 Camera 버튼 클릭
        var wfLaunchNirCommand = new Command("launch-nir", "NIR 1 Camera 버튼 클릭");
        wfLaunchNirCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickNirCameraButton();
            if (result)
            {
                Console.WriteLine("[workflow-launch-nir] Success: NIR 1 Camera button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-launch-nir] Failed: Could not click NIR 1 Camera button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfLaunchNirCommand);

        // workflow launch-nir2: NIR 2 Camera 버튼 클릭
        var wfLaunchNir2Command = new Command("launch-nir2", "NIR 2 Camera 버튼 클릭");
        wfLaunchNir2Command.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickNir2CameraButton();
            if (result)
            {
                Console.WriteLine("[workflow-launch-nir2] Success: NIR 2 Camera button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-launch-nir2] Failed: Could not click NIR 2 Camera button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfLaunchNir2Command);

        // workflow toggle-filtering: NIR Filtering 토글
        var wfToggleFilteringCommand = new Command("toggle-filtering", "NIR Filtering 토글");
        wfToggleFilteringCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ToggleNir2Filtering();
            if (result)
            {
                Console.WriteLine("[workflow-toggle-filtering] Success: NIR Filtering toggled");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-toggle-filtering] Failed: Could not toggle NIR Filtering");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfToggleFilteringCommand);

        // workflow camera-states: 모든 카메라 상태 읽기
        var wfCameraStatesCommand = new Command("camera-states", "모든 카메라 상태 읽기");
        wfCameraStatesCommand.AddOption(jsonOption);
        wfCameraStatesCommand.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var states = controller.GetCameraStates();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "WorkflowPanel",
                        count = states.Count,
                        states = states
                    }
                });
            }
            else
            {
                Console.WriteLine($"[workflow-camera-states] Found {states.Count} camera state(s):");
                foreach (var kvp in states)
                {
                    Console.WriteLine($"  - {kvp.Key}: {kvp.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowCommand.AddCommand(wfCameraStatesCommand);

        // workflow path: Path 제어 (읽기/쓰기)
        var workflowPathCommand = new Command("path", "WorkflowPanel Path 설정 제어");

        // workflow path get-line1: Line 1 경로 읽기
        var pathGetLine1Command = new Command("get-line1", "Line 1 경로 모두 읽기");
        pathGetLine1Command.AddOption(jsonOption);
        pathGetLine1Command.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var paths = controller.GetLine1Paths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        line = "Line1",
                        count = paths.Count,
                        paths = paths
                    }
                });
            }
            else
            {
                Console.WriteLine("[workflow-path get-line1] Line 1 Paths:");
                Console.WriteLine($"  SampleName: {paths.GetValueOrDefault("SampleName", "(not found)")}");
                Console.WriteLine($"  MoveNIR: {paths.GetValueOrDefault("MoveNir", "(not found)")}");
                Console.WriteLine($"  MoveAllData: {paths.GetValueOrDefault("MoveAllData", "(not found)")}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowPathCommand.AddCommand(pathGetLine1Command);

        // workflow path get-line2: Line 2 경로 읽기
        var pathGetLine2Command = new Command("get-line2", "Line 2 경로 모두 읽기");
        pathGetLine2Command.AddOption(jsonOption);
        pathGetLine2Command.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var paths = controller.GetLine2Paths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        line = "Line2",
                        count = paths.Count,
                        paths = paths
                    }
                });
            }
            else
            {
                Console.WriteLine("[workflow-path get-line2] Line 2 Paths:");
                Console.WriteLine($"  SampleName: {paths.GetValueOrDefault("SampleName", "(not found)")}");
                Console.WriteLine($"  MoveNIR: {paths.GetValueOrDefault("MoveNir", "(not found)")}");
                Console.WriteLine($"  MoveAllData: {paths.GetValueOrDefault("MoveAllData", "(not found)")}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowPathCommand.AddCommand(pathGetLine2Command);

        // workflow path get-all: 모든 라인 경로 읽기
        var pathGetAllCommand = new Command("get-all", "모든 Line 1/Line 2 경로 읽기");
        pathGetAllCommand.AddOption(jsonOption);
        pathGetAllCommand.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var allPaths = controller.GetAllPaths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        count = allPaths.Count,
                        paths = allPaths
                    }
                });
            }
            else
            {
                Console.WriteLine("[workflow-path get-all] All Paths:");
                foreach (var linePaths in allPaths)
                {
                    Console.WriteLine($"\n{linePaths.Key}:");
                    foreach (var path in linePaths.Value)
                    {
                        Console.WriteLine($"  {path.Key}: {path.Value}");
                    }
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowPathCommand.AddCommand(pathGetAllCommand);

        // workflow path set-line1: Line 1 특정 경로 설정
        var pathTypeArgument = new Argument<string>("type", "Path type (samplename|movenir|movealldata)");
        var pathValueArgument = new Argument<string>("value", "Path value to set");
        var pathSetLine1Command = new Command("set-line1", "Line 1 특정 경로 설정");
        pathSetLine1Command.AddArgument(pathTypeArgument);
        pathSetLine1Command.AddArgument(pathValueArgument);
        pathSetLine1Command.SetHandler((type, value) =>
        {
            using var controller = new Workflow();
            var result = controller.SetLine1Path(type, value);
            if (result)
            {
                Console.WriteLine($"[workflow-path set-line1] Success: {type} set to '{value}'");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine($"[workflow-path set-line1] Failed: Could not set {type}");
                Environment.Exit(EXIT_ERROR);
            }
        }, pathTypeArgument, pathValueArgument);
        workflowPathCommand.AddCommand(pathSetLine1Command);

        // workflow path set-line2: Line 2 특정 경로 설정
        var pathSetLine2Command = new Command("set-line2", "Line 2 특정 경로 설정");
        pathSetLine2Command.AddArgument(pathTypeArgument);
        pathSetLine2Command.AddArgument(pathValueArgument);
        pathSetLine2Command.SetHandler((type, value) =>
        {
            using var controller = new Workflow();
            var result = controller.SetLine2Path(type, value);
            if (result)
            {
                Console.WriteLine($"[workflow-path set-line2] Success: {type} set to '{value}'");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine($"[workflow-path set-line2] Failed: Could not set {type}");
                Environment.Exit(EXIT_ERROR);
            }
        }, pathTypeArgument, pathValueArgument);
        workflowPathCommand.AddCommand(pathSetLine2Command);

        workflowCommand.AddCommand(workflowPathCommand);

        rootCommand.AddCommand(workflowCommand);

        // logs 명령: LogPanel 데이터 읽기 및 필터링
        var logsCommand = new Command("logs", "LogPanel 데이터 읽기 및 필터링");

        // logs get: 모든 로그 메시지 가져오기
        var logsGetCommand = new Command("get", "모든 로그 메시지 가져오기");
        logsGetCommand.AddOption(jsonOption);
        logsGetCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var logs = reader.GetAllLogMessages();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        count = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                Console.WriteLine($"[logs-get] Found {logs.Count} log message(s):");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        logsCommand.AddCommand(logsGetCommand);

        // logs tail: 최근 N개 로그 메시지 가져오기
        var countArgument = new Argument<int>("count", "가져올 로그 개수 (기본값: 10)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var logsTailCommand = new Command("tail", "최근 N개 로그 메시지 가져오기");
        logsTailCommand.AddArgument(countArgument);
        logsTailCommand.AddOption(jsonOption);
        logsTailCommand.SetHandler((count, json) =>
        {
            var actualCount = count > 0 ? count : 10;
            using var reader = new DataReader();
            var logs = reader.GetLatestLogs(actualCount);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        requested = actualCount,
                        returned = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                Console.WriteLine($"[logs-tail] Latest {logs.Count} log message(s):");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, countArgument, jsonOption);
        logsCommand.AddCommand(logsTailCommand);

        // logs filter: 로그 레벨로 필터링
        var levelOption = new Option<string?>(
            ["--level", "-l"],
            () => null,
            "필터링할 로그 레벨 (Debug, Info, Warning, Error)"
        );
        var logsFilterCommand = new Command("filter", "로그 레벨로 필터링");
        logsFilterCommand.AddOption(levelOption);
        logsFilterCommand.AddOption(jsonOption);
        logsFilterCommand.SetHandler((level, json) =>
        {
            using var reader = new DataReader();
            var logs = reader.GetLogsByLevel(level);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        filter = new { level = level },
                        count = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                var levelText = string.IsNullOrWhiteSpace(level) ? "All" : level;
                Console.WriteLine($"[logs-filter] Filtered by level '{levelText}': {logs.Count} message(s)");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, levelOption, jsonOption);
        logsCommand.AddCommand(logsFilterCommand);

        // logs search: 로그 메시지 검색
        var searchTextArgument = new Argument<string>("text", "검색할 텍스트");
        var logsSearchCommand = new Command("search", "로그 메시지 검색");
        logsSearchCommand.AddArgument(searchTextArgument);
        logsSearchCommand.AddOption(jsonOption);
        logsSearchCommand.SetHandler((text, json) =>
        {
            using var reader = new DataReader();
            var logs = reader.SearchLogs(text);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        search = text,
                        count = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                Console.WriteLine($"[logs-search] Searched for '{text}': {logs.Count} message(s) found");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, searchTextArgument, jsonOption);
        logsCommand.AddCommand(logsSearchCommand);

        rootCommand.AddCommand(logsCommand);

        // settings-dialog 명령: ChronoSettingsController 기반 SettingsDialog 제어
        var settingsDialogCommand = new Command("settings-dialog", "SettingsDialog 제어 (ChronoSettingsController)");

        // settings-dialog open: SettingsDialog 열기
        var settingsOpenCommand = new Command("open", "SettingsDialog 열기 (Settings 버튼 클릭)");
        settingsOpenCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.OpenSettingsDialog();
            if (result)
            {
                Console.WriteLine("[settings-dialog-open] Success: SettingsDialog opened");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[settings-dialog-open] Failed: Could not open SettingsDialog");
                Environment.Exit(EXIT_ERROR);
            }
        });
        settingsDialogCommand.AddCommand(settingsOpenCommand);

        // settings-dialog close: SettingsDialog 닫기
        var settingsCloseCommand = new Command("close", "SettingsDialog 닫기 (Cancel 버튼 클릭)");
        settingsCloseCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.CloseSettingsDialog();
            if (result)
            {
                Console.WriteLine("[settings-dialog-close] Success: SettingsDialog closed");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[settings-dialog-close] Failed: Could not close SettingsDialog");
                Environment.Exit(EXIT_ERROR);
            }
        });
        settingsDialogCommand.AddCommand(settingsCloseCommand);

        // settings-dialog inspect: SettingsDialog 구조 검사
        var settingsInspectCommand = new Command("inspect", "SettingsDialog 구조 검사");
        settingsInspectCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.InspectSettingsDialog();
            if (result)
            {
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[settings-dialog-inspect] Failed: Could not inspect SettingsDialog (dialog not open?)");
                Environment.Exit(EXIT_ERROR);
            }
        });
        settingsDialogCommand.AddCommand(settingsInspectCommand);

        // settings-dialog status: SettingsDialog 열림 상태 확인
        var settingsStatusCommand = new Command("status", "SettingsDialog 열림 상태 확인");
        settingsStatusCommand.AddOption(jsonOption);
        settingsStatusCommand.SetHandler((json) =>
        {
            using var controller = new Settings();
            var isOpen = controller.IsSettingsDialogOpen();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        dialogType = "SettingsDialog",
                        isOpen = isOpen
                    }
                });
            }
            else
            {
                Console.WriteLine(isOpen ? "[settings-dialog-status] SettingsDialog is open" : "[settings-dialog-status] SettingsDialog is not open");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        settingsDialogCommand.AddCommand(settingsStatusCommand);

        // settings-dialog path: Path 제어 (읽기/쓰기)
        var settingsPathCommand = new Command("path", "SettingsDialog Path 설정 제어");

        // settings-dialog path get-all: 모든 경로 읽기
        var settingsPathGetAllCommand = new Command("get-all", "모든 경로 읽기 (Line 1, Line 2, Output, Quarantine)");
        settingsPathGetAllCommand.AddOption(jsonOption);
        settingsPathGetAllCommand.SetHandler((json) =>
        {
            using var controller = new Settings();
            var line1Paths = controller.GetLine1Paths();
            var line2Paths = controller.GetLine2Paths();
            var outputPath = controller.GetOutputPath();
            var quarantinePath = controller.GetQuarantinePath();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        line1 = line1Paths,
                        line2 = line2Paths,
                        output = outputPath,
                        quarantine = quarantinePath
                    }
                });
            }
            else
            {
                Console.WriteLine("[settings-dialog-path get-all] All Paths:");
                Console.WriteLine("\nLine 1:");
                foreach (var kvp in line1Paths)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine("\nLine 2:");
                foreach (var kvp in line2Paths)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine($"\nOutput: {outputPath}");
                Console.WriteLine($"Quarantine: {quarantinePath}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        settingsPathCommand.AddCommand(settingsPathGetAllCommand);

        // settings-dialog path get-line1: Line 1 경로 읽기
        var settingsPathGetLine1Command = new Command("get-line1", "Line 1 경로 읽기");
        settingsPathGetLine1Command.AddOption(jsonOption);
        settingsPathGetLine1Command.SetHandler((json) =>
        {
            using var controller = new Settings();
            var paths = controller.GetLine1Paths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        line = "Line1",
                        count = paths.Count,
                        paths = paths
                    }
                });
            }
            else
            {
                Console.WriteLine("[settings-dialog-path get-line1] Line 1 Paths:");
                foreach (var kvp in paths)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        settingsPathCommand.AddCommand(settingsPathGetLine1Command);

        // settings-dialog path get-line2: Line 2 경로 읽기
        var settingsPathGetLine2Command = new Command("get-line2", "Line 2 경로 읽기");
        settingsPathGetLine2Command.AddOption(jsonOption);
        settingsPathGetLine2Command.SetHandler((json) =>
        {
            using var controller = new Settings();
            var paths = controller.GetLine2Paths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        line = "Line2",
                        count = paths.Count,
                        paths = paths
                    }
                });
            }
            else
            {
                Console.WriteLine("[settings-dialog-path get-line2] Line 2 Paths:");
                foreach (var kvp in paths)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        settingsPathCommand.AddCommand(settingsPathGetLine2Command);

        // settings-dialog path get-output: 출력 경로 읽기
        var settingsPathGetOutputCommand = new Command("get-output", "출력 경로 읽기");
        settingsPathGetOutputCommand.AddOption(jsonOption);
        settingsPathGetOutputCommand.SetHandler((json) =>
        {
            using var controller = new Settings();
            var path = controller.GetOutputPath();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        pathType = "output",
                        path = path
                    }
                });
            }
            else
            {
                Console.WriteLine($"[settings-dialog-path get-output] Output Path: {path}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        settingsPathCommand.AddCommand(settingsPathGetOutputCommand);

        // settings-dialog path get-quarantine: 격리 경로 읽기
        var settingsPathGetQuarantineCommand = new Command("get-quarantine", "격리 경로 읽기");
        settingsPathGetQuarantineCommand.AddOption(jsonOption);
        settingsPathGetQuarantineCommand.SetHandler((json) =>
        {
            using var controller = new Settings();
            var path = controller.GetQuarantinePath();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        pathType = "quarantine",
                        path = path
                    }
                });
            }
            else
            {
                Console.WriteLine($"[settings-dialog-path get-quarantine] Quarantine Path: {path}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        settingsPathCommand.AddCommand(settingsPathGetQuarantineCommand);

        // settings-dialog path set: 경로 설정
        var settingsPathKeyArgument = new Argument<string>("key", "Path key (e.g., nir1, normal1, cam1-6)");
        var settingsPathValueArgument = new Argument<string>("value", "Path value to set");
        var settingsPathSetCommand = new Command("set", "경로 설정");
        settingsPathSetCommand.AddArgument(settingsPathKeyArgument);
        settingsPathSetCommand.AddArgument(settingsPathValueArgument);
        settingsPathSetCommand.SetHandler((key, value) =>
        {
            using var controller = new Settings();
            bool result = false;

            var keyLower = key.ToLowerInvariant();
            if (keyLower.StartsWith("cam") && keyLower.Length == 4)
            {
                // cam1-6
                var camNum = keyLower[3];
                if (camNum >= '1' && camNum <= '3')
                {
                    result = controller.SetLine1Path(key, value);
                }
                else if (camNum >= '4' && camNum <= '6')
                {
                    result = controller.SetLine2Path(key, value);
                }
                else
                {
                    Console.WriteLine($"[settings-dialog-path set] Unknown camera key: {key}");
                    return;
                }
            }
            else if (keyLower == "nir1" || keyLower == "normal1")
            {
                result = controller.SetLine1Path(key, value);
            }
            else if (keyLower == "nir2" || keyLower == "normal2")
            {
                result = controller.SetLine2Path(key, value);
            }
            else
            {
                Console.WriteLine($"[settings-dialog-path set] Unknown path key: {key}");
                return;
            }

            if (result)
            {
                Console.WriteLine($"[settings-dialog-path set] Success: {key} set to '{value}'");
            }
            else
            {
                Console.WriteLine($"[settings-dialog-path set] Failed: Could not set {key}");
            }
        }, settingsPathKeyArgument, settingsPathValueArgument);
        settingsPathCommand.AddCommand(settingsPathSetCommand);

        settingsDialogCommand.AddCommand(settingsPathCommand);

        // settings-dialog checkbox: CheckBox 제어
        var settingsCheckboxCommand = new Command("checkbox", "SettingsDialog CheckBox 제어");

        // settings-dialog checkbox get: CheckBox 상태 읽기
        var checkboxNameArgument = new Argument<string>("name", "CheckBox 이름 (use_folder_suffix, use_disk_cache, etc.)");
        var checkboxGetCommand = new Command("get", "CheckBox 상태 읽기");
        checkboxGetCommand.AddArgument(checkboxNameArgument);
        checkboxGetCommand.AddOption(jsonOption);
        checkboxGetCommand.SetHandler((name, json) =>
        {
            using var controller = new Settings();
            var state = controller.GetCheckBoxState(null, name);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        checkbox = name,
                        isChecked = state
                    }
                });
            }
            else
            {
                Console.WriteLine($"[settings-dialog-checkbox get] '{name}': {(state ? "Checked" : "Unchecked")}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, checkboxNameArgument, jsonOption);
        settingsCheckboxCommand.AddCommand(checkboxGetCommand);

        // settings-dialog checkbox set: CheckBox 상태 설정
        var checkboxValueArgument = new Argument<bool>("value", "CheckBox 값 (true/false)");
        var checkboxSetCommand = new Command("set", "CheckBox 상태 설정");
        checkboxSetCommand.AddArgument(checkboxNameArgument);
        checkboxSetCommand.AddArgument(checkboxValueArgument);
        checkboxSetCommand.SetHandler((name, value) =>
        {
            using var controller = new Settings();
            var result = controller.SetCheckBoxState(null, name, value);
            if (result)
            {
                Console.WriteLine($"[settings-dialog-checkbox set] Success: '{name}' set to {value}");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine($"[settings-dialog-checkbox set] Failed: Could not set '{name}'");
                Environment.Exit(EXIT_ERROR);
            }
        }, checkboxNameArgument, checkboxValueArgument);
        settingsCheckboxCommand.AddCommand(checkboxSetCommand);

        // settings-dialog checkbox list: 모든 CheckBox 상태 목록
        var checkboxListCommand = new Command("list", "모든 CheckBox 상태 목록 (Advanced tab)");
        checkboxListCommand.AddOption(jsonOption);
        checkboxListCommand.SetHandler((json) =>
        {
            using var controller = new Settings();
            var settings = controller.GetAdvancedSettings();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "AdvancedTab",
                        count = settings.Count,
                        settings = settings
                    }
                });
            }
            else
            {
                Console.WriteLine($"[settings-dialog-checkbox list] Found {settings.Count} setting(s):");
                foreach (var kvp in settings)
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        settingsCheckboxCommand.AddCommand(checkboxListCommand);

        settingsDialogCommand.AddCommand(settingsCheckboxCommand);

        // settings-dialog action: Dialog 동작 버튼 제어
        var settingsActionCommand = new Command("action", "SettingsDialog 동작 버튼 제어");

        // settings-dialog action save: Save/OK 버튼 클릭
        var actionSaveCommand = new Command("save", "Save/OK 버튼 클릭 (dialog closes)");
        actionSaveCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.ClickSaveButton();
            Console.WriteLine(result ? "[settings-dialog-action save] Success: Dialog saved and closed" : "[settings-dialog-action save] Failed: Could not click Save button or dialog did not close");
        });
        settingsActionCommand.AddCommand(actionSaveCommand);

        // settings-dialog action apply: Apply 버튼 클릭
        var actionApplyCommand = new Command("apply", "Apply 버튼 클릭 (dialog stays open)");
        actionApplyCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.ClickApplyButton();
            Console.WriteLine(result ? "[settings-dialog-action apply] Success: Apply button clicked" : "[settings-dialog-action apply] Failed: Could not click Apply button");
        });
        settingsActionCommand.AddCommand(actionApplyCommand);

        // settings-dialog action cancel: Cancel 버튼 클릭
        var actionCancelCommand = new Command("cancel", "Cancel 버튼 클릭 (dialog closes)");
        actionCancelCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.ClickCancelButton();
            Console.WriteLine(result ? "[settings-dialog-action cancel] Success: Dialog cancelled and closed" : "[settings-dialog-action cancel] Failed: Could not click Cancel button or dialog did not close");
        });
        settingsActionCommand.AddCommand(actionCancelCommand);

        // settings-dialog action reset: Reset/Defaults 버튼 클릭
        var actionResetCommand = new Command("reset", "Reset/Defaults 버튼 클릭 (dialog stays open)");
        actionResetCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.ClickResetButton();
            Console.WriteLine(result ? "[settings-dialog-action reset] Success: Reset button clicked" : "[settings-dialog-action reset] Failed: Could not click Reset button");
        });
        settingsActionCommand.AddCommand(actionResetCommand);

        settingsDialogCommand.AddCommand(settingsActionCommand);

        rootCommand.AddCommand(settingsDialogCommand);

        // file-ops 명령: ChronoFileOperationsController 기반 파일 작업 제어
        var fileOpsCommand = new Command("file-ops", "파일 작업 제어 (ChronoFileOperationsController)");

        // file-ops select: 행 선택
        var fileOpsSelectCommand = new Command("select", "DataGrid 행 선택");

        // file-ops select --row-index: 특정 인덱스의 행 선택
        var rowIndexOption = new Option<int>(
            ["--row-index", "-r"],
            "선택할 행 인덱스 (0-based)"
        );
        var selectRowIndexCommand = new Command("row-index", "특정 인덱스의 행 선택");
        selectRowIndexCommand.AddOption(rowIndexOption);
        selectRowIndexCommand.SetHandler((rowIndex) =>
        {
            using var controller = new FileOps();
            var result = controller.SelectRowByIndex(rowIndex);
            Console.WriteLine(result ? $"[file-ops-select row-index] Success: Row {rowIndex} selected" : $"[file-ops-select row-index] Failed: Could not select row {rowIndex}");
        }, rowIndexOption);
        fileOpsSelectCommand.AddCommand(selectRowIndexCommand);

        // file-ops select --group-id: GroupId로 행 선택
        var groupIdOption = new Option<string>(
            ["--group-id", "-g"],
            "선택할 GroupId"
        );
        var selectGroupIdCommand = new Command("group-id", "GroupId로 행 선택");
        selectGroupIdCommand.AddOption(groupIdOption);
        selectGroupIdCommand.SetHandler((groupId) =>
        {
            using var controller = new FileOps();
            var result = controller.SelectRowByGroupId(groupId);
            Console.WriteLine(result ? $"[file-ops-select group-id] Success: Row with GroupId '{groupId}' selected" : $"[file-ops-select group-id] Failed: Could not select row with GroupId '{groupId}'");
        }, groupIdOption);
        fileOpsSelectCommand.AddCommand(selectGroupIdCommand);

        fileOpsCommand.AddCommand(fileOpsSelectCommand);

        // file-ops select-all: 모든 행 선택
        var fileOpsSelectAllCommand = new Command("select-all", "모든 행 선택 (SelectAll 체크박스 클릭)");
        fileOpsSelectAllCommand.SetHandler(() =>
        {
            using var controller = new FileOps();
            var result = controller.SelectAllRows();
            Console.WriteLine(result ? "[file-ops-select-all] Success: All rows selected" : "[file-ops-select-all] Failed: Could not select all rows");
        });
        fileOpsCommand.AddCommand(fileOpsSelectAllCommand);

        // file-ops clear-selection: 선택 해제
        var fileOpsClearSelectionCommand = new Command("clear-selection", "모든 행 선택 해제");
        fileOpsClearSelectionCommand.SetHandler(() =>
        {
            using var controller = new FileOps();
            var result = controller.ClearSelection();
            Console.WriteLine(result ? "[file-ops-clear-selection] Success: Selection cleared" : "[file-ops-clear-selection] Failed: Could not clear selection");
        });
        fileOpsCommand.AddCommand(fileOpsClearSelectionCommand);

        // file-ops selected: 선택된 행 목록 조회
        var fileOpsSelectedCommand = new Command("selected", "선택된 행 인덱스 목록 조회");
        fileOpsSelectedCommand.AddOption(jsonOption);
        fileOpsSelectedCommand.SetHandler((json) =>
        {
            using var controller = new FileOps();
            var selectedRows = controller.GetSelectedRows();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        count = selectedRows.Count,
                        selectedRows = selectedRows
                    }
                });
            }
            else
            {
                Console.WriteLine($"[file-ops-selected] Found {selectedRows.Count} selected row(s):");
                foreach (var index in selectedRows)
                {
                    Console.WriteLine($"  - Row {index}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        fileOpsCommand.AddCommand(fileOpsSelectedCommand);

        // file-ops move: 이동 작업
        var fileOpsMoveCommand = new Command("move", "행 선택 후 이동 버튼 클릭");

        // file-ops move --rows: 행 인덱스로 이동
        var rowsOption = new Option<int[]>(
            ["--rows", "-r"],
            "이동할 행 인덱스 목록 (쉼표로 구분)"
        );
        var moveRowsCommand = new Command("rows", "행 인덱스로 선택 후 이동");
        moveRowsCommand.AddOption(rowsOption);
        moveRowsCommand.SetHandler((rows) =>
        {
            using var controller = new FileOps();
            var result = controller.SelectAndMoveRows(rows);
            if (result)
            {
                Console.WriteLine($"[file-ops-move rows] Success: Moved {rows.Length} row(s)");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine($"[file-ops-move rows] Failed: Could not move rows");
                Environment.Exit(EXIT_ERROR);
            }
        }, rowsOption);
        fileOpsMoveCommand.AddCommand(moveRowsCommand);

        // file-ops move --group-ids: GroupId로 이동
        var groupIdsOption = new Option<string[]>(
            ["--group-ids", "-g"],
            "이동할 GroupId 목록 (쉼표로 구분)"
        );
        var moveGroupIdsCommand = new Command("group-ids", "GroupId로 선택 후 이동");
        moveGroupIdsCommand.AddOption(groupIdsOption);
        moveGroupIdsCommand.SetHandler((groupIds) =>
        {
            using var controller = new FileOps();
            var result = controller.SelectAndMoveByGroupIds(groupIds);
            Console.WriteLine(result ? $"[file-ops-move group-ids] Success: Moved {groupIds.Length} row(s)" : $"[file-ops-move group-ids] Failed: Could not move rows by GroupId");
        }, groupIdsOption);
        fileOpsMoveCommand.AddCommand(moveGroupIdsCommand);

        fileOpsCommand.AddCommand(fileOpsMoveCommand);

        // file-ops delete: 삭제 작업
        var fileOpsDeleteCommand = new Command("delete", "행 선택 후 삭제 버튼 클릭");

        // file-ops delete --rows: 행 인덱스로 삭제
        var deleteRowsCommand = new Command("rows", "행 인덱스로 선택 후 삭제");
        deleteRowsCommand.AddOption(rowsOption);
        deleteRowsCommand.SetHandler((rows) =>
        {
            using var controller = new FileOps();
            var result = controller.SelectAndDeleteRows(rows);
            Console.WriteLine(result ? $"[file-ops-delete rows] Success: Deleted {rows.Length} row(s)" : $"[file-ops-delete rows] Failed: Could not delete rows");
        }, rowsOption);
        fileOpsDeleteCommand.AddCommand(deleteRowsCommand);

        // file-ops delete --group-ids: GroupId로 삭제
        var deleteGroupIdsCommand = new Command("group-ids", "GroupId로 선택 후 삭제");
        deleteGroupIdsCommand.AddOption(groupIdsOption);
        deleteGroupIdsCommand.SetHandler((groupIds) =>
        {
            using var controller = new FileOps();
            var result = controller.SelectAndDeleteByGroupIds(groupIds);
            Console.WriteLine(result ? $"[file-ops-delete group-ids] Success: Deleted {groupIds.Length} row(s)" : $"[file-ops-delete group-ids] Failed: Could not delete rows by GroupId");
        }, groupIdsOption);
        fileOpsDeleteCommand.AddCommand(deleteGroupIdsCommand);

        fileOpsCommand.AddCommand(fileOpsDeleteCommand);

        // file-ops wait: 작업 완료 대기
        var fileOpsWaitCommand = new Command("wait", "파일 작업 완료 대기");

        // file-ops wait move: 이동 작업 완료 대기
        var timeoutOption = new Option<int>(
            ["--timeout", "-t"],
            () => 30000,
            "대기 시간 (밀리초, 기본값: 30000)"
        );
        var waitMoveCommand = new Command("move", "이동 작업 완료 대기");
        waitMoveCommand.AddOption(timeoutOption);
        waitMoveCommand.SetHandler((timeout) =>
        {
            using var controller = new FileOps();
            var result = controller.WaitForMoveComplete(timeout);
            Console.WriteLine(result ? $"[file-ops-wait move] Success: Move operation completed" : $"[file-ops-wait move] Failed: Timeout waiting for move operation");
        }, timeoutOption);
        fileOpsWaitCommand.AddCommand(waitMoveCommand);

        // file-ops wait delete: 삭제 작업 완료 대기
        var waitDeleteCommand = new Command("delete", "삭제 작업 완료 대기");
        waitDeleteCommand.AddOption(timeoutOption);
        waitDeleteCommand.SetHandler((timeout) =>
        {
            using var controller = new FileOps();
            var result = controller.WaitForDeleteComplete(timeout);
            Console.WriteLine(result ? $"[file-ops-wait delete] Success: Delete operation completed" : $"[file-ops-wait delete] Failed: Timeout waiting for delete operation");
        }, timeoutOption);
        fileOpsWaitCommand.AddCommand(waitDeleteCommand);

        fileOpsCommand.AddCommand(fileOpsWaitCommand);

        // file-ops confirm: 확인 대화상자 처리
        var fileOpsConfirmCommand = new Command("confirm", "확인 대화상자 찾기 및 클릭");
        fileOpsConfirmCommand.AddOption(jsonOption);
        fileOpsConfirmCommand.SetHandler((json) =>
        {
            using var controller = new FileOps();
            var result = controller.HandleDeleteConfirmationDialog();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = result,
                    data = new
                    {
                        action = "confirm-dialog",
                        confirmed = result
                    }
                });
            }
            else
            {
                Console.WriteLine(result ? "[file-ops-confirm] Success: Confirmation dialog handled" : "[file-ops-confirm] Failed: Could not handle confirmation dialog");
            }
            Environment.Exit(result ? EXIT_SUCCESS : EXIT_ERROR);
        }, jsonOption);
        fileOpsCommand.AddCommand(fileOpsConfirmCommand);

        // file-ops verify: 삭제 검증
        var fileOpsVerifyCommand = new Command("verify", "삭제 작업 결과 검증");

        // file-ops verify deleted: GroupId로 삭제 검증
        var groupIdArgument = new Argument<string>("groupId", "검증할 GroupId");
        var verifyDeletedCommand = new Command("deleted", "GroupId로 그룹 삭제 검증");
        verifyDeletedCommand.AddArgument(groupIdArgument);
        verifyDeletedCommand.AddOption(jsonOption);
        verifyDeletedCommand.SetHandler((groupId, json) =>
        {
            using var controller = new FileOps();
            var result = controller.VerifyGroupDeleted(groupId);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = result,
                    data = new
                    {
                        groupId = groupId,
                        verified = result
                    }
                });
            }
            else
            {
                Console.WriteLine(result ? $"[file-ops-verify deleted] Success: GroupId '{groupId}' has been deleted" : $"[file-ops-verify deleted] Failed: GroupId '{groupId}' still exists");
            }
            Environment.Exit(result ? EXIT_SUCCESS : EXIT_ERROR);
        }, groupIdArgument, jsonOption);
        fileOpsVerifyCommand.AddCommand(verifyDeletedCommand);

        // file-ops verify row-count: 행 개수 변화 검증
        var rowCountWaitCommand = new Command("row-count", "행 개수 변화 대기 및 검증");
        var originalCountArgument = new Argument<int>("originalCount", "원래 행 개수");
        rowCountWaitCommand.AddArgument(originalCountArgument);
        rowCountWaitCommand.AddOption(timeoutOption);
        rowCountWaitCommand.AddOption(jsonOption);
        rowCountWaitCommand.SetHandler((originalCount, timeout, json) =>
        {
            using var controller = new FileOps();
            var result = controller.WaitForRowCountChange(originalCount, timeout);

            if (json)
            {
                var currentCount = controller.GetDataRowCountAfterOperation();
                PrintJsonOutput(new
                {
                    success = result,
                    data = new
                    {
                        originalCount = originalCount,
                        currentCount = currentCount,
                        changed = result
                    }
                });
            }
            else
            {
                var currentCount = controller.GetDataRowCountAfterOperation();
                Console.WriteLine(result ? $"[file-ops-verify row-count] Success: Row count changed from {originalCount} to {currentCount}" : $"[file-ops-verify row-count] Failed: Row count did not change (still {currentCount})");
            }
            Environment.Exit(result ? EXIT_SUCCESS : EXIT_ERROR);
        }, originalCountArgument, timeoutOption, jsonOption);
        fileOpsVerifyCommand.AddCommand(rowCountWaitCommand);

        fileOpsCommand.AddCommand(fileOpsVerifyCommand);

        rootCommand.AddCommand(fileOpsCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
