using System.CommandLine;
using System.Text.Json;
using System.Linq;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;

namespace UiAutomation;

/// <summary>
/// Windows UI Automation CLI - FlaUI 5.x 기반
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Windows UI Automation - FlaUI 5.x 기반 CLI 도구");

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
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = true,
                        windowType = "MainWindow",
                        title = window.Name,
                        className = window.ClassName,
                        automationId = window.AutomationId
                    }));
                }
                else
                {
                    Console.WriteLine($"[MainWindow] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
            }
            else
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = false,
                        windowType = "MainWindow"
                    }));
                }
                else
                {
                    Console.WriteLine("[MainWindow] Not found - make sure ChronoView is running");
                }
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
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = true,
                        windowType = "SetupWindow",
                        title = window.Name,
                        className = window.ClassName,
                        automationId = window.AutomationId
                    }));
                }
                else
                {
                    Console.WriteLine($"[SetupWindow] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
            }
            else
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = false,
                        windowType = "SetupWindow"
                    }));
                }
                else
                {
                    Console.WriteLine("[SetupWindow] Not found");
                }
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
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = true,
                        windowType = "SettingsDialog",
                        title = window.Name,
                        className = window.ClassName,
                        automationId = window.AutomationId
                    }));
                }
                else
                {
                    Console.WriteLine($"[SettingsDialog] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
            }
            else
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = false,
                        windowType = "SettingsDialog"
                    }));
                }
                else
                {
                    Console.WriteLine("[SettingsDialog] Not found");
                }
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
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = true,
                        windowType = "ImagePreviewWindow",
                        title = window.Name,
                        className = window.ClassName,
                        automationId = window.AutomationId
                    }));
                }
                else
                {
                    Console.WriteLine($"[ImagePreviewWindow] Found: '{window.Name}'");
                    Console.WriteLine($"  - ClassName: {window.ClassName ?? "(null)"}");
                    Console.WriteLine($"  - AutomationId: {window.AutomationId ?? "(null)"}");
                }
            }
            else
            {
                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        found = false,
                        windowType = "ImagePreviewWindow"
                    }));
                }
                else
                {
                    Console.WriteLine("[ImagePreviewWindow] Not found");
                }
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    count = windows.Count,
                    windows = windowList
                }));
            }
            else
            {
                Console.WriteLine($"[All ChronoView Windows] Found: {windows.Count}");
                foreach (var window in windows)
                {
                    Console.WriteLine($"  - '{window.Name}'");
                }
            }
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
            Console.WriteLine(result ? "[toolbar-start] Success: Start button clicked" : "[toolbar-start] Failed: Could not click Start button");
        });
        toolbarCommand.AddCommand(toolbarStartCommand);

        // toolbar stop: Stop 버튼 클릭
        var toolbarStopCommand = new Command("stop", "Stop 버튼 클릭");
        toolbarStopCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickStopButton();
            Console.WriteLine(result ? "[toolbar-stop] Success: Stop button clicked" : "[toolbar-stop] Failed: Could not click Stop button");
        });
        toolbarCommand.AddCommand(toolbarStopCommand);

        // toolbar settings: Settings (Setup) 버튼 클릭
        var toolbarSettingsCommand = new Command("settings", "Settings (Setup) 버튼 클릭");
        toolbarSettingsCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickSettingsButton();
            Console.WriteLine(result ? "[toolbar-settings] Success: Settings button clicked" : "[toolbar-settings] Failed: Could not click Settings button");
        });
        toolbarCommand.AddCommand(toolbarSettingsCommand);

        // toolbar refresh: Refresh 버튼 클릭
        var toolbarRefreshCommand = new Command("refresh", "Refresh 버튼 클릭");
        toolbarRefreshCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickRefreshButton();
            Console.WriteLine(result ? "[toolbar-refresh] Success: Refresh button clicked" : "[toolbar-refresh] Failed: Could not click Refresh button");
        });
        toolbarCommand.AddCommand(toolbarRefreshCommand);

        // toolbar move: Move 버튼 클릭
        var toolbarMoveCommand = new Command("move", "Move 버튼 클릭");
        toolbarMoveCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickMoveButton();
            Console.WriteLine(result ? "[toolbar-move] Success: Move button clicked" : "[toolbar-move] Failed: Could not click Move button");
        });
        toolbarCommand.AddCommand(toolbarMoveCommand);

        // toolbar delete: Delete 버튼 클릭
        var toolbarDeleteCommand = new Command("delete", "Delete 버튼 클릭");
        toolbarDeleteCommand.SetHandler(() =>
        {
            using var controller = new Toolbar();
            var result = controller.ClickDeleteButton();
            Console.WriteLine(result ? "[toolbar-delete] Success: Delete button clicked" : "[toolbar-delete] Failed: Could not click Delete button");
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    count = buttons.Length,
                    buttons = buttons
                }));
            }
            else
            {
                Console.WriteLine($"[toolbar-list] Found {buttons.Length} toolbar button(s):");
                foreach (var button in buttons)
                {
                    Console.WriteLine($"  - '{button}'");
                }
            }
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
            Console.WriteLine(result ? $"[toolbar-click] Success: Button '{text}' clicked" : $"[toolbar-click] Failed: Could not click button '{text}'");
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
                Console.WriteLine("[stats] Failed: MainWindow not found");
                return;
            }

            var statistics = automation.GetAllStatistics(mainWindow);
            if (statistics == null)
            {
                Console.WriteLine("[stats] Failed: Could not extract statistics (StatisticsPanel not found)");
                return;
            }

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    source = "StatisticsPanel",
                    statistics = statistics
                }));
            }
            else
            {
                Console.WriteLine("[stats] Statistics from StatisticsPanel:");
                Console.WriteLine("\n📊 File Counts:");
                foreach (var kvp in statistics.Where(k => k.Key.StartsWith("NIR") || k.Key.StartsWith("Normal") || k.Key.StartsWith("Cam")))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine("\n🔗 Matching Status:");
                foreach (var kvp in statistics.Where(k => !k.Key.StartsWith("NIR") && !k.Key.StartsWith("Normal") && !k.Key.StartsWith("Cam") && k.Key != "일반2"))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
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
                Console.WriteLine("[datagrid-headers] Failed: MainWindow not found");
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                Console.WriteLine("[datagrid-headers] Failed: DataGrid not found");
                return;
            }

            var headers = automation.GetDataGridHeaders(dataGrid);
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    columnCount = headers.Count,
                    columns = headers
                }));
            }
            else
            {
                Console.WriteLine($"[datagrid-headers] Found {headers.Count} columns:");
                foreach (var header in headers)
                {
                    Console.WriteLine($"  - {header}");
                }
            }
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
                Console.WriteLine("[datagrid-rows] Failed: MainWindow not found");
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                Console.WriteLine("[datagrid-rows] Failed: DataGrid not found");
                return;
            }

            var rowCount = automation.GetDataRowCount(dataGrid);
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    rowCount = rowCount
                }));
            }
            else
            {
                Console.WriteLine($"[datagrid-rows] DataGrid has {rowCount} data rows");
            }
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
                Console.WriteLine("[datagrid-data] Failed: MainWindow not found");
                return;
            }

            var allData = automation.GetAllDataGridData(mainWindow);
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    rowCount = allData.Count,
                    data = allData
                }));
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
        }, jsonOption);
        datagridCommand.AddCommand(dgDataCommand);

        rootCommand.AddCommand(datagridCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
