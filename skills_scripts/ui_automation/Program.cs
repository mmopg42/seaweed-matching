using System.CommandLine;
using System.Text.Json;
using System.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;
using Workflow = SkillsScripts.UiAutomation.ChronoWorkflowController;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;
using Settings = SkillsScripts.UiAutomation.ChronoSettingsController;
using FileOps = SkillsScripts.UiAutomation.ChronoFileOperationsController;
using UiAutomation.Commands;

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

        // CommandRegistry for modular command registration (Phase 11-01+)
        var registry = new CommandRegistry();

        // JSON output option (local to Program.cs for remaining inline commands)
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // Options shared by scenario and batch commands
        var rowsOption = new Option<int[]>(
            ["--rows", "-r"],
            "행 인덱스 목록 (쉼표로 구분)"
        );
        var groupIdsOption = new Option<string[]>(
            ["--group-ids", "-g"],
            "GroupId 목록 (쉼표로 구분)"
        );

        // Phase 14-01: DataPanelCommands (stats, datagrid) registered via registry

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

        // test 명령: 연결성 및 기능 테스트
        var testCommand = new Command("test", "ChronoView 연결성 및 기능 테스트");

        // test connectivity: ChronoView 실행 중인지 확인
        var testConnectivityCommand = new Command("connectivity", "ChronoView 연결 상태 확인");
        testConnectivityCommand.AddOption(jsonOption);
        testConnectivityCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var window = finder.FindMainWindow();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        connected = window != null,
                        windowFound = window != null,
                        appName = window != null ? window.Name : null,
                        timestamp = DateTime.Now.ToString("o")
                    }
                });
            }
            else
            {
                if (window != null)
                {
                    PrintOutput($"[test-connectivity] Connected: '{window.Name}'");
                }
                else
                {
                    PrintOutput("[test-connectivity] Not connected: ChronoView MainWindow not found");
                }
            }
            Environment.Exit(window != null ? EXIT_SUCCESS : EXIT_NOT_FOUND);
        }, jsonOption);
        testCommand.AddCommand(testConnectivityCommand);

        // test capabilities: 사용 가능한 자동화 기능 목록
        var testCapabilitiesCommand = new Command("capabilities", "사용 가능한 자동화 기능 목록");
        testCapabilitiesCommand.AddOption(jsonOption);
        testCapabilitiesCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());

            // Check which windows are available
            var mainWindow = finder.FindMainWindow();
            var setupWindow = finder.FindSetupWindow();
            var settingsDialog = finder.FindSettingsDialog();

            // Get available controllers/capabilities
            var windows = new List<object>();
            var controllers = new List<string>();
            var commands = new List<string>();

            if (mainWindow != null)
            {
                windows.Add(new { type = "MainWindow", title = mainWindow.Name, accessible = true });
                controllers.Add("ChronoToolbarController");
                controllers.Add("ChronoDataPanelReader");
                controllers.Add("ChronoWorkflowController");
                controllers.Add("ChronoFileOperationsController");
                commands.Add("toolbar");
                commands.Add("datagrid");
                commands.Add("workflow");
                commands.Add("logs");
                commands.Add("file-ops");
            }
            else
            {
                windows.Add(new { type = "MainWindow", accessible = false });
            }

            if (setupWindow != null)
            {
                windows.Add(new { type = "SetupWindow", title = setupWindow.Name, accessible = true });
            }
            else
            {
                windows.Add(new { type = "SetupWindow", accessible = false });
            }

            if (settingsDialog != null)
            {
                windows.Add(new { type = "SettingsDialog", title = settingsDialog.Name, accessible = true });
                controllers.Add("ChronoSettingsController");
                commands.Add("settings-dialog");
            }
            else
            {
                windows.Add(new { type = "SettingsDialog", accessible = false });
            }

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        windows,
                        controllers = controllers.Distinct().ToList(),
                        commands = commands.Distinct().ToList()
                    }
                });
            }
            else
            {
                PrintOutput("[test-capabilities] Available automation capabilities:");
                PrintOutput("  Windows:");
                foreach (var w in windows)
                {
                    PrintOutput($"    - {w}");
                }
                PrintOutput($"  Controllers: {string.Join(", ", controllers.Distinct())}");
                PrintOutput($"  Commands: {string.Join(", ", commands.Distinct())}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        testCommand.AddCommand(testCapabilitiesCommand);

        // test datagrid: DataGrid 접근 가능 여부 확인
        var testDatagridCommand = new Command("datagrid", "DataGrid 접근 가능 여부 및 행 개수 확인");
        testDatagridCommand.AddOption(jsonOption);
        testDatagridCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var dataGrid = reader.FindDataGrid();

            if (dataGrid != null)
            {
                var rowCount = reader.GetDataRowCount();
                var headers = reader.GetDataGridHeaders();

                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = true,
                        data = new
                        {
                            accessible = true,
                            rowCount = rowCount,
                            headers = headers
                        }
                    });
                }
                else
                {
                    PrintOutput($"[test-datagrid] Accessible: {rowCount} row(s), {headers.Count} column(s)");
                    PrintOutput($"  Columns: {string.Join(", ", headers)}");
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
                        error = "DataGrid not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    PrintOutput("[test-datagrid] Not accessible: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
            }
        }, jsonOption);
        testCommand.AddCommand(testDatagridCommand);

        rootCommand.AddCommand(testCommand);

        // scenario 명령: 종단간 워크플로우 자동화
        var scenarioCommand = new Command("scenario", "종단간 워크플로우 자동화");

        // scenario start-monitoring: 모니터링 시작 완전 워크플로우
        var scenarioStartMonitoringCommand = new Command("start-monitoring", "모니터링 시작 완전 워크플로우");
        scenarioStartMonitoringCommand.AddOption(jsonOption);
        scenarioStartMonitoringCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());
            var window = finder.FindMainWindow();

            if (window == null)
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
                    PrintOutput("[scenario-start-monitoring] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
            }

            using var toolbar = new Toolbar(automation.GetAutomation());

            // Click Start button
            var clicked = toolbar.ClickStartButton();
            if (!clicked)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "Could not click Start button",
                        errorCode = EXIT_ERROR
                    });
                }
                else
                {
                    PrintOutput("[scenario-start-monitoring] Failed: Could not click Start button");
                }
                Environment.Exit(EXIT_ERROR);
            }

            // Wait for button state change (Start becomes disabled)
            var stateChanged = toolbar.WaitForButtonDisabled("시작", 5000);

            // Check statistics for monitoring indicators
            using var reader = new DataReader(automation.GetAutomation());
            var stats = reader.GetAllStatistics();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        started = true,
                        buttonState = stateChanged ? "disabled" : "unknown",
                        stats = stats
                    }
                });
            }
            else
            {
                PrintOutput($"[scenario-start-monitoring] Success: Monitoring started");
                PrintOutput($"  Button state changed: {stateChanged}");
                if (stats != null)
                {
                    PrintOutput($"  Statistics: {string.Join(", ", stats.Keys)}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        scenarioCommand.AddCommand(scenarioStartMonitoringCommand);

        // scenario configure-paths: 모니터링 경로 설정 완전 워크플로우
        var scenarioConfigurePathsCommand = new Command("configure-paths", "모니터링 경로 설정 완전 워크플로우");
        var line1NirOption = new Option<string>(
            ["--line1-nir"],
            "Line 1 NIR 경로"
        );
        var line1NormalOption = new Option<string>(
            ["--line1-normal"],
            "Line 1 Normal 경로"
        );
        var line2NirOption = new Option<string>(
            ["--line2-nir"],
            "Line 2 NIR 경로"
        );
        var line2NormalOption = new Option<string>(
            ["--line2-normal"],
            "Line 2 Normal 경로"
        );
        var outputPathOption = new Option<string>(
            ["--output"],
            "출력 경로"
        );
        scenarioConfigurePathsCommand.AddOption(line1NirOption);
        scenarioConfigurePathsCommand.AddOption(line1NormalOption);
        scenarioConfigurePathsCommand.AddOption(line2NirOption);
        scenarioConfigurePathsCommand.AddOption(line2NormalOption);
        scenarioConfigurePathsCommand.AddOption(outputPathOption);
        scenarioConfigurePathsCommand.AddOption(jsonOption);
        scenarioConfigurePathsCommand.SetHandler((line1Nir, line1Normal, line2Nir, line2Normal, output, json) =>
        {
            using var automation = new UiAuto();
            var finder = new Finder(automation.GetAutomation());

            // Open SettingsDialog
            using var toolbar = new Toolbar(automation.GetAutomation());
            var opened = toolbar.ClickSettingsButton();
            if (!opened)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "Could not open SettingsDialog",
                        errorCode = EXIT_ERROR
                    });
                }
                else
                {
                    PrintOutput("[scenario-configure-paths] Failed: Could not open SettingsDialog");
                }
                Environment.Exit(EXIT_ERROR);
            }

            // Wait for dialog to appear
            var dialog = finder.WaitForWindow("Settings", 5000);
            if (dialog == null)
            {
                // Try Korean title
                dialog = finder.WaitForWindow("설정", 2000);
            }
            if (dialog == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "SettingsDialog did not appear",
                        errorCode = EXIT_TIMEOUT
                    });
                }
                else
                {
                    PrintOutput("[scenario-configure-paths] Failed: SettingsDialog did not appear");
                }
                Environment.Exit(EXIT_TIMEOUT);
            }

            using var settings = new Settings(automation.GetAutomation());

            // Set paths if provided using the dedicated path setters
            var configured = new List<string>();
            if (!string.IsNullOrEmpty(line1Nir))
            {
                if (settings.SetLine1Path("nir1", line1Nir))
                {
                    configured.Add("Line1NIR");
                }
            }
            if (!string.IsNullOrEmpty(line1Normal))
            {
                if (settings.SetLine1Path("normal1", line1Normal))
                {
                    configured.Add("Line1Normal");
                }
            }
            if (!string.IsNullOrEmpty(line2Nir))
            {
                if (settings.SetLine2Path("nir2", line2Nir))
                {
                    configured.Add("Line2NIR");
                }
            }
            if (!string.IsNullOrEmpty(line2Normal))
            {
                if (settings.SetLine2Path("normal2", line2Normal))
                {
                    configured.Add("Line2Normal");
                }
            }
            if (!string.IsNullOrEmpty(output))
            {
                // Output path needs to use SetPathTextBoxValue
                var outputSet = settings.SetPathTextBoxValue(dialog, "Output", output);
                if (outputSet)
                {
                    configured.Add("Output");
                }
            }

            // Click Save button
            var saved = settings.ClickSaveButton();

            // Wait for dialog to close
            var dialogClosed = finder.WaitForWindowToClose("Settings", 3000);
            if (!dialogClosed)
            {
                // Try Korean title
                dialogClosed = finder.WaitForWindowToClose("설정", 1000);
            }

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = saved && dialogClosed,
                    data = new
                    {
                        configured = configured,
                        verified = dialogClosed
                    }
                });
            }
            else
            {
                PrintOutput($"[scenario-configure-paths] {(saved && dialogClosed ? "Success" : "Partial")}: Configured {configured.Count} path(s)");
                foreach (var path in configured)
                {
                    PrintOutput($"  - {path}");
                }
                PrintOutput($"  Verified: {dialogClosed}");
            }
            Environment.Exit(saved && dialogClosed ? EXIT_SUCCESS : EXIT_ERROR);
        }, line1NirOption, line1NormalOption, line2NirOption, line2NormalOption, outputPathOption, jsonOption);
        scenarioCommand.AddCommand(scenarioConfigurePathsCommand);

        // scenario move-groups: 파일 그룹 이동 완전 워크플로우
        var scenarioMoveGroupsCommand = new Command("move-groups", "파일 그룹 이동 완전 워크플로우");
        scenarioMoveGroupsCommand.AddOption(rowsOption);
        scenarioMoveGroupsCommand.AddOption(groupIdsOption);
        scenarioMoveGroupsCommand.AddOption(jsonOption);
        scenarioMoveGroupsCommand.SetHandler((rows, groupIds, json) =>
        {
            using var reader = new DataReader();
            using var controller = new FileOps();

            // Get original row count
            var originalCount = reader.GetDataRowCount();

            // Select and move rows
            var moved = false;
            if (rows != null && rows.Length > 0)
            {
                moved = controller.SelectAndMoveRows(rows);
            }
            else if (groupIds != null && groupIds.Length > 0)
            {
                moved = controller.SelectAndMoveByGroupIds(groupIds);
            }
            else
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "Must specify --rows or --group-ids",
                        errorCode = EXIT_INVALID_ARGUMENT
                    });
                }
                else
                {
                    PrintOutput("[scenario-move-groups] Failed: Must specify --rows or --group-ids");
                }
                Environment.Exit(EXIT_INVALID_ARGUMENT);
            }

            // Wait for operation completion
            if (moved)
            {
                controller.WaitForMoveComplete(30000);
            }

            // Get new row count
            var newCount = reader.GetDataRowCount();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = moved,
                    data = new
                    {
                        moved = moved ? (rows?.Length ?? groupIds?.Length ?? 0) : 0,
                        originalCount = originalCount,
                        newCount = newCount
                    }
                });
            }
            else
            {
                PrintOutput($"[scenario-move-groups] {(moved ? "Success" : "Failed")}: Moved {rows?.Length ?? groupIds?.Length ?? 0} group(s)");
                PrintOutput($"  Row count: {originalCount} -> {newCount}");
            }
            Environment.Exit(moved ? EXIT_SUCCESS : EXIT_ERROR);
        }, rowsOption, groupIdsOption, jsonOption);
        scenarioCommand.AddCommand(scenarioMoveGroupsCommand);

        rootCommand.AddCommand(scenarioCommand);

        // batch 명령: 대량 작업
        var batchCommand = new Command("batch", "대량 작업 (multi-row operations)");

        // batch select-and-move: Select multiple rows and move them
        var batchSelectAndMoveCommand = new Command("select-and-move", "Select multiple rows and move them");
        var startIndexOption = new Option<int>(
            ["--start-index", "-s"],
            "Start index (0-based)"
        );
        var countOption = new Option<int>(
            ["--count", "-c"],
            "Number of rows to select"
        );
        batchSelectAndMoveCommand.AddOption(startIndexOption);
        batchSelectAndMoveCommand.AddOption(countOption);
        batchSelectAndMoveCommand.AddOption(rowsOption);
        batchSelectAndMoveCommand.AddOption(groupIdsOption);
        batchSelectAndMoveCommand.AddOption(jsonOption);
        batchSelectAndMoveCommand.SetHandler((startIndex, count, rows, groupIds, json) =>
        {
            using var controller = new FileOps();

            // Clear existing selection first
            controller.ClearSelection();

            var selectedCount = 0;

            // Determine selection method
            if (rows != null && rows.Length > 0)
            {
                // Select by specific row indices
                selectedCount = rows.Length;
                foreach (var rowIndex in rows)
                {
                    controller.SelectRowByIndex(rowIndex);
                }
            }
            else if (groupIds != null && groupIds.Length > 0)
            {
                // Select by GroupIds
                selectedCount = groupIds.Length;
                foreach (var groupId in groupIds)
                {
                    controller.SelectRowByGroupId(groupId);
                }
            }
            else if (count > 0)
            {
                // Select by range
                selectedCount = count;
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    controller.SelectRowByIndex(i);
                }
            }
            else
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "Must specify --rows, --group-ids, or --count with --start-index",
                        errorCode = EXIT_INVALID_ARGUMENT
                    });
                }
                else
                {
                    PrintOutput("[batch-select-and-move] Failed: Must specify --rows, --group-ids, or --count with --start-index");
                }
                Environment.Exit(EXIT_INVALID_ARGUMENT);
            }

            // Click Move button
            var moved = controller.ClickMoveButton();

            // Wait for completion
            if (moved)
            {
                controller.WaitForMoveComplete(30000);
            }

            var duration = moved ? "completed" : "failed";

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = moved,
                    data = new
                    {
                        selected = selectedCount,
                        moved = moved ? selectedCount : 0,
                        duration = duration
                    }
                });
            }
            else
            {
                PrintOutput($"[batch-select-and-move] {(moved ? "Success" : "Failed")}: {selectedCount} row(s) selected, {(moved ? "moved" : "move failed")}");
            }
            Environment.Exit(moved ? EXIT_SUCCESS : EXIT_ERROR);
        }, startIndexOption, countOption, rowsOption, groupIdsOption, jsonOption);
        batchCommand.AddCommand(batchSelectAndMoveCommand);

        // batch select-and-delete: Select multiple rows and delete them
        var batchSelectAndDeleteCommand = new Command("select-and-delete", "Select multiple rows and delete them");
        batchSelectAndDeleteCommand.AddOption(startIndexOption);
        batchSelectAndDeleteCommand.AddOption(countOption);
        batchSelectAndDeleteCommand.AddOption(rowsOption);
        batchSelectAndDeleteCommand.AddOption(groupIdsOption);
        batchSelectAndDeleteCommand.AddOption(jsonOption);
        batchSelectAndDeleteCommand.SetHandler((startIndex, count, rows, groupIds, json) =>
        {
            using var controller = new FileOps();

            // Clear existing selection first
            controller.ClearSelection();

            var selectedCount = 0;

            // Determine selection method
            if (rows != null && rows.Length > 0)
            {
                // Select by specific row indices
                selectedCount = rows.Length;
                foreach (var rowIndex in rows)
                {
                    controller.SelectRowByIndex(rowIndex);
                }
            }
            else if (groupIds != null && groupIds.Length > 0)
            {
                // Select by GroupIds
                selectedCount = groupIds.Length;
                foreach (var groupId in groupIds)
                {
                    controller.SelectRowByGroupId(groupId);
                }
            }
            else if (count > 0)
            {
                // Select by range
                selectedCount = count;
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    controller.SelectRowByIndex(i);
                }
            }
            else
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "Must specify --rows, --group-ids, or --count with --start-index",
                        errorCode = EXIT_INVALID_ARGUMENT
                    });
                }
                else
                {
                    PrintOutput("[batch-select-and-delete] Failed: Must specify --rows, --group-ids, or --count with --start-index");
                }
                Environment.Exit(EXIT_INVALID_ARGUMENT);
            }

            // Click Delete button
            var deleted = controller.ClickDeleteButton();

            // Handle confirmation dialog if present
            var confirmed = false;
            if (deleted)
            {
                confirmed = controller.HandleDeleteConfirmationDialog();
                if (confirmed)
                {
                    controller.WaitForDeleteComplete(30000);
                }
            }

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = confirmed,
                    data = new
                    {
                        selected = selectedCount,
                        deleted = confirmed ? selectedCount : 0,
                        confirmed = confirmed
                    }
                });
            }
            else
            {
                PrintOutput($"[batch-select-and-delete] {(confirmed ? "Success" : "Failed")}: {selectedCount} row(s) selected, {(confirmed ? "deleted" : "delete failed")}");
                PrintOutput($"  Confirmed: {confirmed}");
            }
            Environment.Exit(confirmed ? EXIT_SUCCESS : EXIT_ERROR);
        }, startIndexOption, countOption, rowsOption, groupIdsOption, jsonOption);
        batchCommand.AddCommand(batchSelectAndDeleteCommand);

        // batch export-all: Export all available data from ChronoView
        var batchExportAllCommand = new Command("export-all", "Export all available data from ChronoView");
        batchExportAllCommand.AddOption(jsonOption);
        batchExportAllCommand.SetHandler((json) =>
        {
            using var automation = new UiAuto();

            // Get StatisticsPanel statistics
            using var reader = new DataReader(automation.GetAutomation());
            var stats = reader.GetAllStatistics();

            // Get all DataGrid rows
            var allData = reader.GetAllData();

            // Get camera states from WorkflowPanel
            using var workflow = new Workflow(automation.GetAutomation());
            var cameraStates = workflow.GetCameraStates();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        timestamp = DateTime.Now.ToString("o"),
                        statistics = stats,
                        dataGrid = new
                        {
                            rowCount = allData.Count,
                            rows = allData
                        },
                        cameraStates = cameraStates
                    }
                });
            }
            else
            {
                PrintOutput("[batch-export-all] Exported all available data:");
                PrintOutput($"  Timestamp: {DateTime.Now:O}");
                PrintOutput($"  Statistics: {stats?.Count ?? 0} entries");
                if (stats != null)
                {
                    foreach (var stat in stats)
                    {
                        PrintOutput($"    - {stat.Key}: {stat.Value}");
                    }
                }
                PrintOutput($"  DataGrid: {allData.Count} row(s)");
                PrintOutput($"  Camera States: {cameraStates.Count} camera(s)");
                foreach (var camState in cameraStates)
                {
                    PrintOutput($"    - {camState.Key}: {camState.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        batchCommand.AddCommand(batchExportAllCommand);

        rootCommand.AddCommand(batchCommand);

        // config: Config 파일 직접 읽기 (UI Automation 없이 파일 시스템에서 직접 확인)
        var configCommand = new Command("config", "Config 파일 직접 읽기");

        // config path: Config 파일 위치 확인
        var configPathCommand = new Command("path", "Config 파일 위치 확인");
        configPathCommand.SetHandler(() =>
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");

            Console.WriteLine($"[Config Path] {configPath}");
            Console.WriteLine($"[Exists] {File.Exists(configPath)}");

            if (File.Exists(configPath))
            {
                var fileInfo = new FileInfo(configPath);
                Console.WriteLine($"[Size] {fileInfo.Length} bytes");
                Console.WriteLine($"[Modified] {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }

            Environment.Exit(EXIT_SUCCESS);
        });
        configCommand.AddCommand(configPathCommand);

        // config read: Config 파일 내용 읽기
        var configReadCommand = new Command("read", "Config 파일 내용 읽기");
        var jsonConfigOption = new Option<bool>(["--json", "-j"], "JSON 형식으로 출력");
        configReadCommand.AddOption(jsonConfigOption);
        configReadCommand.SetHandler((json) =>
        {
            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[Config] File not found: {configPath}");
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);

                if (json)
                {
                    // Pretty print JSON
                    using var jsonDoc = JsonDocument.Parse(jsonContent);
                    var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    Console.WriteLine(prettyJson);
                }
                else
                {
                    Console.WriteLine($"[Config] Reading from: {configPath}");
                    Console.WriteLine();
                    Console.WriteLine("=== Raw JSON ===");
                    Console.WriteLine(jsonContent);
                }

                Environment.Exit(EXIT_SUCCESS);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Error reading file: {ex.Message}");
                Environment.Exit(EXIT_ERROR);
                return;
            }
        }, jsonConfigOption);
        configCommand.AddCommand(configReadCommand);

        // config get: 특정 설정 값 읽기 (경로 등)
        var configGetCommand = new Command("get", "특정 설정 값 읽기");
        var keyOption = new Option<string>(["--key", "-k"], "설정 키 (예: folderPaths.line1SampleName)");
        configGetCommand.AddOption(keyOption);
        configGetCommand.SetHandler((key) =>
        {
            if (string.IsNullOrEmpty(key))
            {
                Console.WriteLine("[Config] --key parameter is required");
                Environment.Exit(EXIT_INVALID_ARGUMENT);
                return;
            }

            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");

            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[Config] File not found: {configPath}");
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            try
            {
                var jsonContent = File.ReadAllText(configPath);
                using var jsonDoc = JsonDocument.Parse(jsonContent);
                var root = jsonDoc.RootElement;

                // Navigate using JSON path (dot notation)
                var parts = key.Split('.');
                var current = root;

                foreach (var part in parts)
                {
                    if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var property))
                    {
                        current = property;
                    }
                    else
                    {
                        Console.WriteLine($"[Config] Key not found: {key}");
                        Environment.Exit(EXIT_NOT_FOUND);
                        return;
                    }
                }

                Console.WriteLine($"[{key}] {current}");
                Environment.Exit(EXIT_SUCCESS);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Config] Error reading key: {ex.Message}");
                Environment.Exit(EXIT_ERROR);
                return;
            }
        }, keyOption);
        configCommand.AddCommand(configGetCommand);

        rootCommand.AddCommand(configCommand);

        // Register modular command handlers via registry (Phase 11-02+)
        registry.RegisterHandler(new LegacyCommands());
        registry.RegisterHandler(new WindowsCommands());
        // Phase 13-01: Register toolbar commands
        registry.RegisterHandler(new ToolbarCommands());
        // Phase 14-01: Register data panel commands
        registry.RegisterHandler(new DataPanelCommands());
        // Phase 15-01: Register workflow and log commands
        registry.RegisterHandler(new WorkflowCommands());
        // Phase 15-02: Register settings dialog and console log commands
        registry.RegisterHandler(new SettingsCommands());
        // Phase 16-01: Register file operations commands
        registry.RegisterHandler(new FileOpsCommands());
        registry.RegisterAllCommands(rootCommand);

        // Parse args to capture global options before command execution
        var parseResult = rootCommand.Parse(args);
        s_isQuiet = parseResult.GetValueForOption(quietOption) == true;
        s_isVerbose = parseResult.GetValueForOption(verboseOption) == true;

        return await rootCommand.InvokeAsync(args);
    }
}
