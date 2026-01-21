using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using FileOps = SkillsScripts.UiAutomation.ChronoFileOperationsController;
using static UiAutomation.Commands.ExitCodes;

namespace UiAutomation.Commands;

/// <summary>
/// File operations commands for ChronoView automation.
/// Provides commands to control file operations (select, move, delete, wait, confirm, verify)
/// using ChronoFileOperationsController.
/// </summary>
public class FileOpsCommands : ICommandHandler
{
    /// <summary>
    /// Registers all file operations commands with the root command.
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
        // file-ops 명령: ChronoFileOperationsController 기반 파일 작업 제어
        // ============================================================

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
        selectRowIndexCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var rowIndex = context.ParseResult.GetValueForOption(rowIndexOption);
                using var controller = new FileOps();
                var result = controller.SelectRowByIndex(rowIndex);
                Console.WriteLine(result ? $"[file-ops-select row-index] Success: Row {rowIndex} selected" : $"[file-ops-select row-index] Failed: Could not select row {rowIndex}");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-select row-index] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsSelectCommand.AddCommand(selectRowIndexCommand);

        // file-ops select --group-id: GroupId로 행 선택
        var groupIdOption = new Option<string>(
            ["--group-id", "-g"],
            "선택할 GroupId"
        );
        var selectGroupIdCommand = new Command("group-id", "GroupId로 행 선택");
        selectGroupIdCommand.AddOption(groupIdOption);
        selectGroupIdCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var groupId = context.ParseResult.GetValueForOption(groupIdOption);
                using var controller = new FileOps();
                var result = controller.SelectRowByGroupId(groupId);
                Console.WriteLine(result ? $"[file-ops-select group-id] Success: Row with GroupId '{groupId}' selected" : $"[file-ops-select group-id] Failed: Could not select row with GroupId '{groupId}'");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-select group-id] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsSelectCommand.AddCommand(selectGroupIdCommand);

        // file-ops select prefix: 프리픽스로 행 선택
        var prefixOption = new Option<string>(
            ["--prefix", "-p"],
            "GroupId prefix to filter (e.g., 'line2_')"
        );
        var selectPrefixCommand = new Command("prefix", "Select rows by GroupId prefix");
        selectPrefixCommand.AddOption(prefixOption);
        selectPrefixCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var prefix = context.ParseResult.GetValueForOption(prefixOption);
                using var controller = new FileOps();
                var result = controller.SelectRowsByPrefix(prefix);
                Console.WriteLine(result ?
                    $"[file-ops-select prefix] Success: Selected rows with prefix '{prefix}'" :
                    $"[file-ops-select prefix] Failed: No rows found with prefix '{prefix}'");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-select prefix] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsSelectCommand.AddCommand(selectPrefixCommand);

        fileOpsCommand.AddCommand(fileOpsSelectCommand);

        // file-ops select-all: 모든 행 선택
        var fileOpsSelectAllCommand = new Command("select-all", "모든 행 선택 (SelectAll 체크박스 클릭)");
        fileOpsSelectAllCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new FileOps();
                var result = controller.SelectAllRows();
                Console.WriteLine(result ? "[file-ops-select-all] Success: All rows selected" : "[file-ops-select-all] Failed: Could not select all rows");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-select-all] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsCommand.AddCommand(fileOpsSelectAllCommand);

        // file-ops clear-selection: 선택 해제
        var fileOpsClearSelectionCommand = new Command("clear-selection", "모든 행 선택 해제");
        fileOpsClearSelectionCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new FileOps();
                var result = controller.ClearSelection();
                Console.WriteLine(result ? "[file-ops-clear-selection] Success: Selection cleared" : "[file-ops-clear-selection] Failed: Could not clear selection");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-clear-selection] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsCommand.AddCommand(fileOpsClearSelectionCommand);

        // file-ops selected: 선택된 행 목록 조회
        var fileOpsSelectedCommand = new Command("selected", "선택된 행 인덱스 목록 조회");
        fileOpsSelectedCommand.AddOption(jsonOption);
        fileOpsSelectedCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-selected] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
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
        moveRowsCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var rows = context.ParseResult.GetValueForOption(rowsOption);
                using var controller = new FileOps();
                var result = controller.SelectAndMoveRows(rows);
                if (result)
                {
                    Console.WriteLine($"[file-ops-move rows] Success: Moved {rows.Length} row(s)");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    Console.WriteLine($"[file-ops-move rows] Failed: Could not move rows");
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-move rows] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsMoveCommand.AddCommand(moveRowsCommand);

        // file-ops move --group-ids: GroupId로 이동
        var groupIdsOption = new Option<string[]>(
            ["--group-ids", "-g"],
            "이동할 GroupId 목록 (쉼표로 구분)"
        );
        var moveGroupIdsCommand = new Command("group-ids", "GroupId로 선택 후 이동");
        moveGroupIdsCommand.AddOption(groupIdsOption);
        moveGroupIdsCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var groupIds = context.ParseResult.GetValueForOption(groupIdsOption);
                using var controller = new FileOps();
                var result = controller.SelectAndMoveByGroupIds(groupIds);
                Console.WriteLine(result ? $"[file-ops-move group-ids] Success: Moved {groupIds.Length} row(s)" : $"[file-ops-move group-ids] Failed: Could not move rows by GroupId");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-move group-ids] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsMoveCommand.AddCommand(moveGroupIdsCommand);

        // file-ops move prefix: 프리픽스로 선택 후 이동
        var movePrefixOption = new Option<string>(
            ["--prefix", "-p"],
            "GroupId prefix to filter (e.g., 'line2_')"
        );
        var movePrefixCommand = new Command("prefix", "Select and move rows by GroupId prefix");
        movePrefixCommand.AddOption(movePrefixOption);
        movePrefixCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var prefix = context.ParseResult.GetValueForOption(movePrefixOption);
                using var controller = new FileOps();

                // 1. Select by prefix
                if (!controller.SelectRowsByPrefix(prefix))
                {
                    Console.WriteLine($"[file-ops-move prefix] Failed: No rows found with prefix '{prefix}'");
                    context.ExitCode = ERROR;
                    return;
                }

                // 2. Small delay for UI update
                System.Threading.Thread.Sleep(100);

                // 3. Click move button
                var result = controller.ClickMoveButton();
                Console.WriteLine(result ?
                    $"[file-ops-move prefix] Success: Move initiated for '{prefix}'" :
                    $"[file-ops-move prefix] Failed: Could not click Move button");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-move prefix] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsMoveCommand.AddCommand(movePrefixCommand);

        fileOpsCommand.AddCommand(fileOpsMoveCommand);

        // file-ops delete: 삭제 작업
        var fileOpsDeleteCommand = new Command("delete", "행 선택 후 삭제 버튼 클릭");

        // file-ops delete --rows: 행 인덱스로 삭제
        var deleteRowsCommand = new Command("rows", "행 인덱스로 선택 후 삭제");
        deleteRowsCommand.AddOption(rowsOption);
        deleteRowsCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var rows = context.ParseResult.GetValueForOption(rowsOption);
                using var controller = new FileOps();
                var result = controller.SelectAndDeleteRows(rows);
                Console.WriteLine(result ? $"[file-ops-delete rows] Success: Deleted {rows.Length} row(s)" : $"[file-ops-delete rows] Failed: Could not delete rows");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-delete rows] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsDeleteCommand.AddCommand(deleteRowsCommand);

        // file-ops delete --group-ids: GroupId로 삭제
        var deleteGroupIdsCommand = new Command("group-ids", "GroupId로 선택 후 삭제");
        deleteGroupIdsCommand.AddOption(groupIdsOption);
        deleteGroupIdsCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var groupIds = context.ParseResult.GetValueForOption(groupIdsOption);
                using var controller = new FileOps();
                var result = controller.SelectAndDeleteByGroupIds(groupIds);
                Console.WriteLine(result ? $"[file-ops-delete group-ids] Success: Deleted {groupIds.Length} row(s)" : $"[file-ops-delete group-ids] Failed: Could not delete rows by GroupId");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-delete group-ids] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
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
        waitMoveCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var timeout = context.ParseResult.GetValueForOption(timeoutOption);
                using var controller = new FileOps();
                var result = controller.WaitForMoveComplete(timeout);
                Console.WriteLine(result ? $"[file-ops-wait move] Success: Move operation completed" : $"[file-ops-wait move] Failed: Timeout waiting for move operation");
                context.ExitCode = result ? SUCCESS : TIMEOUT;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-wait move] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsWaitCommand.AddCommand(waitMoveCommand);

        // file-ops wait delete: 삭제 작업 완료 대기
        var waitDeleteCommand = new Command("delete", "삭제 작업 완료 대기");
        waitDeleteCommand.AddOption(timeoutOption);
        waitDeleteCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var timeout = context.ParseResult.GetValueForOption(timeoutOption);
                using var controller = new FileOps();
                var result = controller.WaitForDeleteComplete(timeout);
                Console.WriteLine(result ? $"[file-ops-wait delete] Success: Delete operation completed" : $"[file-ops-wait delete] Failed: Timeout waiting for delete operation");
                context.ExitCode = result ? SUCCESS : TIMEOUT;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-wait delete] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsWaitCommand.AddCommand(waitDeleteCommand);

        fileOpsCommand.AddCommand(fileOpsWaitCommand);

        // file-ops confirm: 확인 대화상자 처리
        var fileOpsConfirmCommand = new Command("confirm", "확인 대화상자 찾기 및 클릭");
        fileOpsConfirmCommand.AddOption(jsonOption);
        fileOpsConfirmCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
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
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-confirm] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsCommand.AddCommand(fileOpsConfirmCommand);

        // file-ops verify: 삭제 검증
        var fileOpsVerifyCommand = new Command("verify", "삭제 작업 결과 검증");

        // file-ops verify deleted: GroupId로 삭제 검증
        var groupIdArgument = new Argument<string>("groupId", "검증할 GroupId");
        var verifyDeletedCommand = new Command("deleted", "GroupId로 그룹 삭제 검증");
        verifyDeletedCommand.AddArgument(groupIdArgument);
        verifyDeletedCommand.AddOption(jsonOption);
        verifyDeletedCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var groupId = context.ParseResult.GetValueForArgument(groupIdArgument);
                var json = context.ParseResult.GetValueForOption(jsonOption);

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
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-verify deleted] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsVerifyCommand.AddCommand(verifyDeletedCommand);

        // file-ops verify row-count: 행 개수 변화 검증
        var rowCountWaitCommand = new Command("row-count", "행 개수 변화 대기 및 검증");
        var originalCountArgument = new Argument<int>("originalCount", "원래 행 개수");
        rowCountWaitCommand.AddArgument(originalCountArgument);
        rowCountWaitCommand.AddOption(timeoutOption);
        rowCountWaitCommand.AddOption(jsonOption);
        rowCountWaitCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var originalCount = context.ParseResult.GetValueForArgument(originalCountArgument);
                var timeout = context.ParseResult.GetValueForOption(timeoutOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

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
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[file-ops-verify row-count] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        fileOpsVerifyCommand.AddCommand(rowCountWaitCommand);

        fileOpsCommand.AddCommand(fileOpsVerifyCommand);

        rootCommand.AddCommand(fileOpsCommand);
    }

    /// <summary>
    /// Prints output in JSON format for programmatic access.
    /// </summary>
    private static void PrintJsonOutput(object data)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(data, options));
    }
}
