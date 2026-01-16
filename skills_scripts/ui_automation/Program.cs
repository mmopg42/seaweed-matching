using System.CommandLine;
using System.Text.Json;
using System.Linq;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;
using Workflow = SkillsScripts.UiAutomation.ChronoWorkflowController;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;
using Settings = SkillsScripts.UiAutomation.ChronoSettingsController;

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

        // datagrid info: Show headers and row count
        var dgInfoCommand = new Command("info", "DataGrid 헤더 및 행 개수 요약");
        dgInfoCommand.AddOption(jsonOption);
        dgInfoCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[datagrid-info] Failed: MainWindow not found");
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                Console.WriteLine("[datagrid-info] Failed: DataGrid not found");
                return;
            }

            var headers = automation.GetDataGridHeaders(dataGrid);
            var rowCount = automation.GetDataRowCount(dataGrid);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    columnCount = headers.Count,
                    rowCount = rowCount,
                    columns = headers
                }));
            }
            else
            {
                Console.WriteLine($"[datagrid-info] DataGrid: {headers.Count} columns, {rowCount} rows");
                Console.WriteLine("  Columns: " + string.Join(", ", headers));
            }
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
                Console.WriteLine("[datagrid-cell] Failed: MainWindow not found");
                return;
            }

            var dataGrid = automation.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                Console.WriteLine("[datagrid-cell] Failed: DataGrid not found");
                return;
            }

            var cf = automation.GetAutomation().ConditionFactory;
            var rows = dataGrid.FindAllChildren(cf.ByControlType(FlaUI.Core.Definitions.ControlType.DataItem));

            if (row < 0 || row >= rows.Length)
            {
                Console.WriteLine($"[datagrid-cell] Failed: Row index {row} out of range (0-{rows.Length - 1})");
                return;
            }

            var rowElement = rows[row];
            var cellText = automation.GetCellText(rowElement, col);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = cellText != null,
                    row = row,
                    column = col,
                    value = cellText
                }));
            }
            else
            {
                Console.WriteLine($"[datagrid-cell] Row {row}, Column {col}: '{cellText ?? "(null)"}'");
            }
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
                return;
            }

            var allData = automation.GetAllDataGridData(mainWindow);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                success = true,
                rowCount = allData.Count,
                exportedAt = DateTime.UtcNow.ToString("o"),
                data = allData
            }, new JsonSerializerOptions { WriteIndented = true }));
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
            Console.WriteLine(result ? "[workflow-launch-general] Success: General Camera button clicked" : "[workflow-launch-general] Failed: Could not click General Camera button");
        });
        workflowCommand.AddCommand(wfLaunchGeneralCommand);

        // workflow launch-nir: NIR 1 Camera 버튼 클릭
        var wfLaunchNirCommand = new Command("launch-nir", "NIR 1 Camera 버튼 클릭");
        wfLaunchNirCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickNirCameraButton();
            Console.WriteLine(result ? "[workflow-launch-nir] Success: NIR 1 Camera button clicked" : "[workflow-launch-nir] Failed: Could not click NIR 1 Camera button");
        });
        workflowCommand.AddCommand(wfLaunchNirCommand);

        // workflow launch-nir2: NIR 2 Camera 버튼 클릭
        var wfLaunchNir2Command = new Command("launch-nir2", "NIR 2 Camera 버튼 클릭");
        wfLaunchNir2Command.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickNir2CameraButton();
            Console.WriteLine(result ? "[workflow-launch-nir2] Success: NIR 2 Camera button clicked" : "[workflow-launch-nir2] Failed: Could not click NIR 2 Camera button");
        });
        workflowCommand.AddCommand(wfLaunchNir2Command);

        // workflow toggle-filtering: NIR Filtering 토글
        var wfToggleFilteringCommand = new Command("toggle-filtering", "NIR Filtering 토글");
        wfToggleFilteringCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ToggleNir2Filtering();
            Console.WriteLine(result ? "[workflow-toggle-filtering] Success: NIR Filtering toggled" : "[workflow-toggle-filtering] Failed: Could not toggle NIR Filtering");
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    source = "WorkflowPanel",
                    count = states.Count,
                    states = states
                }));
            }
            else
            {
                Console.WriteLine($"[workflow-camera-states] Found {states.Count} camera state(s):");
                foreach (var kvp in states)
                {
                    Console.WriteLine($"  - {kvp.Key}: {kvp.Value}");
                }
            }
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    line = "Line1",
                    count = paths.Count,
                    paths = paths
                }));
            }
            else
            {
                Console.WriteLine("[workflow-path get-line1] Line 1 Paths:");
                Console.WriteLine($"  SampleName: {paths.GetValueOrDefault("SampleName", "(not found)")}");
                Console.WriteLine($"  MoveNIR: {paths.GetValueOrDefault("MoveNir", "(not found)")}");
                Console.WriteLine($"  MoveAllData: {paths.GetValueOrDefault("MoveAllData", "(not found)")}");
            }
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    line = "Line2",
                    count = paths.Count,
                    paths = paths
                }));
            }
            else
            {
                Console.WriteLine("[workflow-path get-line2] Line 2 Paths:");
                Console.WriteLine($"  SampleName: {paths.GetValueOrDefault("SampleName", "(not found)")}");
                Console.WriteLine($"  MoveNIR: {paths.GetValueOrDefault("MoveNir", "(not found)")}");
                Console.WriteLine($"  MoveAllData: {paths.GetValueOrDefault("MoveAllData", "(not found)")}");
            }
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    count = allPaths.Count,
                    paths = allPaths
                }));
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
            Console.WriteLine(result ? $"[workflow-path set-line1] Success: {type} set to '{value}'" : $"[workflow-path set-line1] Failed: Could not set {type}");
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
            Console.WriteLine(result ? $"[workflow-path set-line2] Success: {type} set to '{value}'" : $"[workflow-path set-line2] Failed: Could not set {type}");
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    source = "LogPanel",
                    count = logs.Count,
                    logs = logs
                }));
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    source = "LogPanel",
                    requested = actualCount,
                    returned = logs.Count,
                    logs = logs
                }));
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    source = "LogPanel",
                    filter = new { level = level },
                    count = logs.Count,
                    logs = logs
                }));
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    source = "LogPanel",
                    search = text,
                    count = logs.Count,
                    logs = logs
                }));
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
            Console.WriteLine(result ? "[settings-dialog-open] Success: SettingsDialog opened" : "[settings-dialog-open] Failed: Could not open SettingsDialog");
        });
        settingsDialogCommand.AddCommand(settingsOpenCommand);

        // settings-dialog close: SettingsDialog 닫기
        var settingsCloseCommand = new Command("close", "SettingsDialog 닫기 (Cancel 버튼 클릭)");
        settingsCloseCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.CloseSettingsDialog();
            Console.WriteLine(result ? "[settings-dialog-close] Success: SettingsDialog closed" : "[settings-dialog-close] Failed: Could not close SettingsDialog");
        });
        settingsDialogCommand.AddCommand(settingsCloseCommand);

        // settings-dialog inspect: SettingsDialog 구조 검사
        var settingsInspectCommand = new Command("inspect", "SettingsDialog 구조 검사");
        settingsInspectCommand.SetHandler(() =>
        {
            using var controller = new Settings();
            var result = controller.InspectSettingsDialog();
            if (!result)
            {
                Console.WriteLine("[settings-dialog-inspect] Failed: Could not inspect SettingsDialog (dialog not open?)");
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
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    success = true,
                    dialogType = "SettingsDialog",
                    isOpen = isOpen
                }));
            }
            else
            {
                Console.WriteLine(isOpen ? "[settings-dialog-status] SettingsDialog is open" : "[settings-dialog-status] SettingsDialog is not open");
            }
        }, jsonOption);
        settingsDialogCommand.AddCommand(settingsStatusCommand);

        rootCommand.AddCommand(settingsDialogCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
