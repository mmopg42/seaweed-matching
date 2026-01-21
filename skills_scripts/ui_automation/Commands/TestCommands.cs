using System.CommandLine;
using System.CommandLine.Invocation;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;
using Workflow = SkillsScripts.UiAutomation.ChronoWorkflowController;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;
using Settings = SkillsScripts.UiAutomation.ChronoSettingsController;
using FileOps = SkillsScripts.UiAutomation.ChronoFileOperationsController;
using static UiAutomation.Commands.ExitCodes;
using static UiAutomation.Commands.JsonResponseHelper;
using static UiAutomation.Commands.DryRunHandler;

namespace UiAutomation.Commands;

/// <summary>
/// Test, scenario, and batch commands for ChronoView automation.
/// Provides high-level orchestration commands that coordinate multiple controllers
/// for end-to-end workflows including:
/// - test: Connectivity, capabilities, and DataGrid accessibility checks
/// - scenario: Complete workflows (start-monitoring, configure-paths, move-groups)
/// - batch: Bulk operations (select-and-move, select-and-delete, export-all)
/// </summary>
public class TestCommands : ICommandHandler
{
    /// <summary>
    /// Registers all test, scenario, and batch commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // JSON output option
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

        // ============================================================
        // test 명령: 연결성 및 기능 테스트
        // ============================================================

        var testCommand = new Command("test", "ChronoView 연결성 및 기능 테스트");

        // test connectivity: ChronoView 실행 중인지 확인
        var testConnectivityCommand = new Command("connectivity", "ChronoView 연결 상태 확인");
        testConnectivityCommand.AddOption(jsonOption);
        testConnectivityCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TEST_CONNECTIVITY", "test connectivity"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var automation = new UiAuto();
                var finder = new Finder(automation.GetAutomation());
                var window = finder.FindMainWindow();

                if (json)
                {
                    if (window != null)
                    {
                        PrintSuccess(new
                        {
                            connected = true,
                            windowFound = true,
                            appName = window.Name,
                            timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }
                    else
                    {
                        PrintSuccess(new
                        {
                            connected = false,
                            windowFound = false,
                            appName = (string?)null,
                            timestamp = DateTime.UtcNow.ToString("o")
                        });
                    }
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
                context.ExitCode = window != null ? SUCCESS : NOT_FOUND;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[test-connectivity] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        testCommand.AddCommand(testConnectivityCommand);

        // test capabilities: 사용 가능한 자동화 기능 목록
        var testCapabilitiesCommand = new Command("capabilities", "사용 가능한 자동화 기능 목록");
        testCapabilitiesCommand.AddOption(jsonOption);
        testCapabilitiesCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TEST_CAPABILITIES", "test capabilities"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
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
                    PrintSuccess(new
                    {
                        windows,
                        controllers = controllers.Distinct().ToList(),
                        commands = commands.Distinct().ToList()
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[test-capabilities] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        testCommand.AddCommand(testCapabilitiesCommand);

        // test datagrid: DataGrid 접근 가능 여부 확인
        var testDatagridCommand = new Command("datagrid", "DataGrid 접근 가능 여부 및 행 개수 확인");
        testDatagridCommand.AddOption(jsonOption);
        testDatagridCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TEST_DATAGRID", "test datagrid"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var reader = new DataReader();
                var dataGrid = reader.FindDataGrid();

                if (dataGrid != null)
                {
                    var rowCount = reader.GetDataRowCount();
                    var headers = reader.GetDataGridHeaders();

                    if (json)
                    {
                        PrintSuccess(new
                        {
                            accessible = true,
                            rowCount = rowCount,
                            headers = headers
                        });
                    }
                    else
                    {
                        PrintOutput($"[test-datagrid] Accessible: {rowCount} row(s), {headers.Count} column(s)");
                        PrintOutput($"  Columns: {string.Join(", ", headers)}");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("DataGrid not accessible", NOT_FOUND,
                            "Ensure ChronoView is running and MainWindow is active. Try 'windows main --json'.");
                    }
                    else
                    {
                        PrintOutput("[test-datagrid] Not accessible: DataGrid not found");
                    }
                    context.ExitCode = NOT_FOUND;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[test-datagrid] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        testCommand.AddCommand(testDatagridCommand);

        rootCommand.AddCommand(testCommand);

        // ============================================================
        // scenario 명령: 종단간 워크플로우 자동화
        // ============================================================

        var scenarioCommand = new Command("scenario", "종단간 워크플로우 자동화");

        // scenario start-monitoring: 모니터링 시작 완전 워크플로우
        var scenarioStartMonitoringCommand = new Command("start-monitoring", "모니터링 시작 완전 워크플로우");
        scenarioStartMonitoringCommand.AddOption(jsonOption);
        scenarioStartMonitoringCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            // Note: This is a scenario command (orchestration), not directly mapped to a skill
            // Use empty skill name to skip validation in dry-run mode
            if (CheckDryRun(context, "", "scenario start-monitoring"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var automation = new UiAuto();
                var finder = new Finder(automation.GetAutomation());
                var window = finder.FindMainWindow();

                if (window == null)
                {
                    if (json)
                    {
                        PrintError("MainWindow not found", NOT_FOUND,
                            "Ensure ChronoView is running. Try 'app status --json' to check.");
                    }
                    else
                    {
                        PrintOutput("[scenario-start-monitoring] Failed: MainWindow not found");
                    }
                    context.ExitCode = NOT_FOUND;
                    return;
                }

                using var toolbar = new Toolbar(automation.GetAutomation());

                // Click Start button
                var clicked = toolbar.ClickStartButton();
                if (!clicked)
                {
                    if (json)
                    {
                        PrintError("Could not click Start button", ERROR,
                            "The toolbar may be busy. Wait a moment and retry.");
                    }
                    else
                    {
                        PrintOutput("[scenario-start-monitoring] Failed: Could not click Start button");
                    }
                    context.ExitCode = ERROR;
                    return;
                }

                // Wait for button state change (Start becomes disabled)
                var stateChanged = toolbar.WaitForButtonDisabled("시작", 5000);

                // Check statistics for monitoring indicators
                using var reader = new DataReader(automation.GetAutomation());
                var stats = reader.GetAllStatistics();

                if (json)
                {
                    PrintSuccess(new
                    {
                        started = true,
                        buttonState = stateChanged ? "disabled" : "unknown",
                        stats = stats
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[scenario-start-monitoring] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
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
        scenarioConfigurePathsCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            // Note: This is a scenario command (orchestration), not directly mapped to a skill
            if (CheckDryRun(context, "", "scenario configure-paths"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var line1Nir = context.ParseResult.GetValueForOption(line1NirOption);
                var line1Normal = context.ParseResult.GetValueForOption(line1NormalOption);
                var line2Nir = context.ParseResult.GetValueForOption(line2NirOption);
                var line2Normal = context.ParseResult.GetValueForOption(line2NormalOption);
                var output = context.ParseResult.GetValueForOption(outputPathOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                using var automation = new UiAuto();
                var finder = new Finder(automation.GetAutomation());

                // Open SettingsDialog
                using var toolbar = new Toolbar(automation.GetAutomation());
                var opened = toolbar.ClickSettingsButton();
                if (!opened)
                {
                    if (json)
                    {
                        PrintError("Could not open SettingsDialog", ERROR,
                            "MainWindow may not be active. Try 'windows main --json' first.");
                    }
                    else
                    {
                        PrintOutput("[scenario-configure-paths] Failed: Could not open SettingsDialog");
                    }
                    context.ExitCode = ERROR;
                    return;
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
                        PrintError("SettingsDialog did not appear", TIMEOUT,
                            "The dialog may have opened and closed quickly. Try 'settings-dialog open --json'.");
                    }
                    else
                    {
                        PrintOutput("[scenario-configure-paths] Failed: SettingsDialog did not appear");
                    }
                    context.ExitCode = TIMEOUT;
                    return;
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
                    PrintSuccess(new
                    {
                        configured = configured,
                        verified = dialogClosed
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
                context.ExitCode = saved && dialogClosed ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[scenario-configure-paths] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        scenarioCommand.AddCommand(scenarioConfigurePathsCommand);

        // scenario move-groups: 파일 그룹 이동 완전 워크플로우
        var scenarioMoveGroupsCommand = new Command("move-groups", "파일 그룹 이동 완전 워크플로우");
        scenarioMoveGroupsCommand.AddOption(rowsOption);
        scenarioMoveGroupsCommand.AddOption(groupIdsOption);
        scenarioMoveGroupsCommand.AddOption(jsonOption);
        scenarioMoveGroupsCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            // Note: This is a scenario command (orchestration), not directly mapped to a skill
            if (CheckDryRun(context, "", "scenario move-groups"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var rows = context.ParseResult.GetValueForOption(rowsOption);
                var groupIds = context.ParseResult.GetValueForOption(groupIdsOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

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
                        PrintError("Must specify --rows or --group-ids", INVALID_ARGUMENT,
                            "Provide row indices with --rows or GroupIds with --group-ids.");
                    }
                    else
                    {
                        PrintOutput("[scenario-move-groups] Failed: Must specify --rows or --group-ids");
                    }
                    context.ExitCode = INVALID_ARGUMENT;
                    return;
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
                    PrintSuccess(new
                    {
                        moved = moved ? (rows?.Length ?? groupIds?.Length ?? 0) : 0,
                        originalCount = originalCount,
                        newCount = newCount
                    });
                }
                else
                {
                    PrintOutput($"[scenario-move-groups] {(moved ? "Success" : "Failed")}: Moved {rows?.Length ?? groupIds?.Length ?? 0} group(s)");
                    PrintOutput($"  Row count: {originalCount} -> {newCount}");
                }
                context.ExitCode = moved ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[scenario-move-groups] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        scenarioCommand.AddCommand(scenarioMoveGroupsCommand);

        rootCommand.AddCommand(scenarioCommand);

        // ============================================================
        // batch 명령: 대량 작업
        // ============================================================

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
        batchSelectAndMoveCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "BATCH_SELECT_AND_MOVE", "batch select-and-move"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var startIndex = context.ParseResult.GetValueForOption(startIndexOption);
                var count = context.ParseResult.GetValueForOption(countOption);
                var rows = context.ParseResult.GetValueForOption(rowsOption);
                var groupIds = context.ParseResult.GetValueForOption(groupIdsOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

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
                        PrintError("Must specify --rows, --group-ids, or --count with --start-index", INVALID_ARGUMENT,
                            "Provide selection criteria: --rows, --group-ids, or --count with --start-index.");
                    }
                    else
                    {
                        PrintOutput("[batch-select-and-move] Failed: Must specify --rows, --group-ids, or --count with --start-index");
                    }
                    context.ExitCode = INVALID_ARGUMENT;
                    return;
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
                    PrintSuccess(new
                    {
                        selected = selectedCount,
                        moved = moved ? selectedCount : 0,
                        duration = duration
                    });
                }
                else
                {
                    PrintOutput($"[batch-select-and-move] {(moved ? "Success" : "Failed")}: {selectedCount} row(s) selected, {(moved ? "moved" : "move failed")}");
                }
                context.ExitCode = moved ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[batch-select-and-move] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        batchCommand.AddCommand(batchSelectAndMoveCommand);

        // batch select-and-delete: Select multiple rows and delete them
        var batchSelectAndDeleteCommand = new Command("select-and-delete", "Select multiple rows and delete them");
        batchSelectAndDeleteCommand.AddOption(startIndexOption);
        batchSelectAndDeleteCommand.AddOption(countOption);
        batchSelectAndDeleteCommand.AddOption(rowsOption);
        batchSelectAndDeleteCommand.AddOption(groupIdsOption);
        batchSelectAndDeleteCommand.AddOption(jsonOption);
        batchSelectAndDeleteCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "BATCH_SELECT_AND_DELETE", "batch select-and-delete"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var startIndex = context.ParseResult.GetValueForOption(startIndexOption);
                var count = context.ParseResult.GetValueForOption(countOption);
                var rows = context.ParseResult.GetValueForOption(rowsOption);
                var groupIds = context.ParseResult.GetValueForOption(groupIdsOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

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
                        PrintError("Must specify --rows, --group-ids, or --count with --start-index", INVALID_ARGUMENT,
                            "Provide selection criteria: --rows, --group-ids, or --count with --start-index.");
                    }
                    else
                    {
                        PrintOutput("[batch-select-and-delete] Failed: Must specify --rows, --group-ids, or --count with --start-index");
                    }
                    context.ExitCode = INVALID_ARGUMENT;
                    return;
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
                    PrintSuccess(new
                    {
                        selected = selectedCount,
                        deleted = confirmed ? selectedCount : 0,
                        confirmed = confirmed
                    });
                }
                else
                {
                    PrintOutput($"[batch-select-and-delete] {(confirmed ? "Success" : "Failed")}: {selectedCount} row(s) selected, {(confirmed ? "deleted" : "delete failed")}");
                    PrintOutput($"  Confirmed: {confirmed}");
                }
                context.ExitCode = confirmed ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[batch-select-and-delete] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        batchCommand.AddCommand(batchSelectAndDeleteCommand);

        // batch export-all: Export all available data from ChronoView
        var batchExportAllCommand = new Command("export-all", "Export all available data from ChronoView");
        batchExportAllCommand.AddOption(jsonOption);
        batchExportAllCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "BATCH_EXPORT_ALL", "batch export-all"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
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
                    PrintSuccess(new
                    {
                        timestamp = DateTime.UtcNow.ToString("o"),
                        statistics = stats,
                        dataGrid = new
                        {
                            rowCount = allData.Count,
                            rows = allData
                        },
                        cameraStates = cameraStates
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[batch-export-all] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        batchCommand.AddCommand(batchExportAllCommand);

        rootCommand.AddCommand(batchCommand);
    }

    // ============================================================
    // Helper methods
    // ============================================================

    /// <summary>
    /// Print output message (always enabled for test/scenario/batch commands)
    /// </summary>
    private static void PrintOutput(string message)
    {
        Console.WriteLine(message);
    }
}
