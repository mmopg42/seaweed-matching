using System.CommandLine;
using System.CommandLine.Invocation;
using FlaUI.Core.AutomationElements;
using Settings = SkillsScripts.UiAutomation.ChronoSettingsController;
using ConsoleLogs = SkillsScripts.UiAutomation.ConsoleLogsReader;
using static UiAutomation.Commands.ExitCodes;
using static UiAutomation.Commands.JsonResponseHelper;
using static UiAutomation.Commands.DryRunHandler;

namespace UiAutomation.Commands;

/// <summary>
/// Settings dialog and console log commands for ChronoView automation.
/// Provides commands to control SettingsDialog (open, close, inspect, status, paths, checkboxes, actions)
/// and read console log files (list, tail, search).
/// </summary>
public class SettingsCommands : ICommandHandler
{
    /// <summary>
    /// Registers all settings dialog and console log commands with the root command.
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
        // console-logs 명령: 콘솔 로그 파일 읽기 (개발자용 디버그 로그)
        // ============================================================

        var consoleLogsCommand = new Command("console-logs", "콘솔 로그 파일 읽기 (개발자용 디버그 로그)");

        // console-logs list: 사용 가능한 로그 파일 목록
        var dateFilterOption = new Option<string?>(
            ["--date", "-d"],
            () => null,
            "날짜 필터 (YYYYMMDD 형식, 예: 20260118)"
        );
        var latestOption = new Option<bool>(
            ["--latest", "-l"],
            "가장 최신 날짜 폴더 사용 (yyyyMMdd 형식 폴더 중 최신)"
        );
        var consoleLogsListCommand = new Command("list", "사용 가능한 로그 파일 목록");
        consoleLogsListCommand.AddOption(dateFilterOption);
        consoleLogsListCommand.AddOption(latestOption);
        consoleLogsListCommand.AddOption(jsonOption);
        consoleLogsListCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "CONSOLE_LOGS_LIST", "console-logs list"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var dateFilter = context.ParseResult.GetValueForOption(dateFilterOption);
                var latest = context.ParseResult.GetValueForOption(latestOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var reader = new ConsoleLogs();
                string[] files;

                // 명시적 우선순위: --date > --latest > 기본 (모든 폴더)
                if (!string.IsNullOrEmpty(dateFilter))
                {
                    // --date 우선 (기존 동작)
                    files = reader.GetLogFiles(dateFilter);
                }
                else if (latest)
                {
                    // --latest만 사용: 자동 발견
                    var latestFolder = reader.GetLatestLogDateFolder();
                    if (latestFolder != null)
                    {
                        dateFilter = Path.GetFileName(latestFolder);
                        files = reader.GetLogFiles(dateFilter);
                    }
                    else
                    {
                        files = Array.Empty<string>();
                    }
                }
                else
                {
                    // 없음: 모든 폴더 검색 (기본 동작)
                    files = reader.GetLogFiles();
                }

                if (json)
                {
                    PrintSuccess(new
                    {
                        source = "ConsoleLogs",
                        logDirectory = reader.GetLogDirectory(),
                        dateFilter = dateFilter,
                        count = files.Length,
                        files = files
                    });
                }
                else
                {
                    Console.WriteLine($"[console-logs-list] Log directory: {reader.GetLogDirectory()}");
                    if (!string.IsNullOrEmpty(dateFilter))
                    {
                        Console.WriteLine($"  Date filter: {dateFilter}");
                    }
                    Console.WriteLine($"  Found {files.Length} file(s):");
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var lastWrite = File.GetLastWriteTime(file);
                        Console.WriteLine($"    - {fileName} (Modified: {lastWrite:yyyy-MM-dd HH:mm:ss})");
                    }
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                PrintError($"Failed to list log files: {ex.Message}", ERROR,
                    "Ensure ChronoView has been run. Check log directory path.");
                context.ExitCode = ERROR;
            }
        });
        consoleLogsCommand.AddCommand(consoleLogsListCommand);

        // console-logs tail: 최근 N줄 읽기
        var tailCountArgument = new Argument<int>("count", "읽을 줄 수 (기본값: 20)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var logPathOption = new Option<string?>(
            ["--file", "-f"],
            () => null,
            "로그 파일 경로 (지정하지 않으면 최신 파일 사용)"
        );
        var consoleLogsTailCommand = new Command("tail", "최근 N줄 읽기");
        consoleLogsTailCommand.AddArgument(tailCountArgument);
        consoleLogsTailCommand.AddOption(logPathOption);
        consoleLogsTailCommand.AddOption(latestOption);
        consoleLogsTailCommand.AddOption(jsonOption);
        consoleLogsTailCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "CONSOLE_LOGS_TAIL", "console-logs tail"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var count = context.ParseResult.GetValueForArgument(tailCountArgument);
                var logPath = context.ParseResult.GetValueForOption(logPathOption);
                var latest = context.ParseResult.GetValueForOption(latestOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var actualCount = count > 0 ? count : 20;
                var reader = new ConsoleLogs();

                // 명시적 우선순위: --file > --latest > 기본
                string? targetPath = null;
                if (!string.IsNullOrEmpty(logPath))
                {
                    targetPath = logPath;  // --file 우선
                }
                else if (latest)
                {
                    var latestFiles = reader.GetLogFilesFromLatest();
                    if (latestFiles.Length > 0)
                    {
                        targetPath = latestFiles[0];  // 최신 폴더의 첫 번째 파일
                    }
                }
                // else: 기본 동작 (모든 폴더에서 최신 파일)

                // 경로가 지정되지 않으면 모든 폴더에서 최신 파일 찾기
                if (string.IsNullOrEmpty(targetPath))
                {
                    var files = reader.GetLogFiles();
                    if (files.Length == 0)
                    {
                        PrintError("No log files found", NOT_FOUND,
                            "Ensure ChronoView has been run. Check log directory path.");
                        context.ExitCode = NOT_FOUND;
                        return;
                    }
                    targetPath = files[0]; // 최신 파일
                }

                if (!File.Exists(targetPath))
                {
                    PrintError($"File not found: {targetPath}", NOT_FOUND,
                        "Verify the file path or use --latest flag.");
                    context.ExitCode = NOT_FOUND;
                    return;
                }

                var lines = reader.ReadTail(targetPath, actualCount);

                if (json)
                {
                    PrintSuccess(new
                    {
                        source = "ConsoleLogs",
                        file = targetPath,
                        requested = actualCount,
                        returned = lines.Length,
                        logs = lines
                    });
                }
                else
                {
                    Console.WriteLine($"[console-logs-tail] Latest {lines.Length} line(s) from: {Path.GetFileName(targetPath)}");
                    foreach (var line in lines)
                    {
                        Console.WriteLine($"  {line}");
                    }
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                PrintError($"Failed to read log tail: {ex.Message}", ERROR,
                    "Ensure the log file exists and is readable.");
                context.ExitCode = ERROR;
            }
        });
        consoleLogsCommand.AddCommand(consoleLogsTailCommand);

        // console-logs search: 텍스트 검색
        var consoleSearchTextArgument = new Argument<string>("text", "검색할 텍스트");
        var consoleMaxResultsOption = new Option<int>(
            ["--max", "-m"],
            () => 50,
            "최대 결과 수"
        );
        var consoleLogsSearchCommand = new Command("search", "텍스트 검색");
        consoleLogsSearchCommand.AddArgument(consoleSearchTextArgument);
        consoleLogsSearchCommand.AddOption(logPathOption);
        consoleLogsSearchCommand.AddOption(latestOption);
        consoleLogsSearchCommand.AddOption(consoleMaxResultsOption);
        consoleLogsSearchCommand.AddOption(jsonOption);
        consoleLogsSearchCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "CONSOLE_LOGS_SEARCH", "console-logs search"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var text = context.ParseResult.GetValueForArgument(consoleSearchTextArgument);
                var path = context.ParseResult.GetValueForOption(logPathOption);
                var latest = context.ParseResult.GetValueForOption(latestOption);
                var maxResults = context.ParseResult.GetValueForOption(consoleMaxResultsOption);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                var reader = new ConsoleLogs();

                // 명시적 우선순위: --file > --latest > 기본
                string? targetPath = null;
                if (!string.IsNullOrEmpty(path))
                {
                    targetPath = path;  // --file 우선
                }
                else if (latest)
                {
                    var latestFiles = reader.GetLogFilesFromLatest();
                    if (latestFiles.Length > 0)
                    {
                        targetPath = latestFiles[0];  // 최신 폴더의 첫 번째 파일
                    }
                }
                // else: 기본 동작 (모든 폴더에서 최신 파일)

                // 경로가 지정되지 않으면 모든 폴더에서 최신 파일 찾기
                if (string.IsNullOrEmpty(targetPath))
                {
                    var files = reader.GetLogFiles();
                    if (files.Length == 0)
                    {
                        PrintError("No log files found to search", NOT_FOUND,
                            "Ensure ChronoView has been run and logs exist.");
                        context.ExitCode = NOT_FOUND;
                        return;
                    }
                    targetPath = files[0]; // 최신 파일
                }

                if (!File.Exists(targetPath))
                {
                    PrintError($"File not found: {targetPath}", NOT_FOUND,
                        "Verify the file path or use --latest flag.");
                    context.ExitCode = NOT_FOUND;
                    return;
                }

                var lines = reader.Search(targetPath, text, maxResults);

                if (json)
                {
                    PrintSuccess(new
                    {
                        source = "ConsoleLogs",
                        file = targetPath,
                        search = text,
                        maxResults = maxResults,
                        count = lines.Length,
                        logs = lines
                    });
                }
                else
                {
                    Console.WriteLine($"[console-logs-search] Searched for '{text}' in {Path.GetFileName(targetPath)}: {lines.Length} match(es)");
                    foreach (var line in lines)
                    {
                        Console.WriteLine($"  {line}");
                    }
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                PrintError($"Failed to search logs: {ex.Message}", ERROR,
                    "Verify the search text and file path are valid.");
                context.ExitCode = ERROR;
            }
        });
        consoleLogsCommand.AddCommand(consoleLogsSearchCommand);

        rootCommand.AddCommand(consoleLogsCommand);

        // ============================================================
        // settings-dialog 명령: ChronoSettingsController 기반 SettingsDialog 제어
        // ============================================================

        var settingsDialogCommand = new Command("settings-dialog", "SettingsDialog 제어 (ChronoSettingsController)");

        // settings-dialog open: SettingsDialog 열기
        var settingsOpenCommand = new Command("open", "SettingsDialog 열기 (Settings 버튼 클릭)");
        settingsOpenCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_OPEN", "settings-dialog open"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var controller = new Settings();
                var result = controller.OpenSettingsDialog();
                if (result)
                {
                    Console.WriteLine("[settings-dialog-open] Success: SettingsDialog opened");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    Console.WriteLine("[settings-dialog-open] Failed: Could not open SettingsDialog");
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-open] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsDialogCommand.AddCommand(settingsOpenCommand);

        // settings-dialog close: SettingsDialog 닫기
        var settingsCloseCommand = new Command("close", "SettingsDialog 닫기 (Cancel 버튼 클릭)");
        settingsCloseCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_CLOSE", "settings-dialog close"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var controller = new Settings();
                var result = controller.CloseSettingsDialog();
                if (result)
                {
                    Console.WriteLine("[settings-dialog-close] Success: SettingsDialog closed");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    Console.WriteLine("[settings-dialog-close] Failed: Could not close SettingsDialog");
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-close] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsDialogCommand.AddCommand(settingsCloseCommand);

        // settings-dialog inspect: SettingsDialog 구조 검사
        var settingsInspectCommand = new Command("inspect", "SettingsDialog 구조 검사");
        settingsInspectCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_INSPECT", "settings-dialog inspect"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var controller = new Settings();
                var result = controller.InspectSettingsDialog();
                if (result)
                {
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    Console.WriteLine("[settings-dialog-inspect] Failed: Could not inspect SettingsDialog (dialog not open?)");
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-inspect] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsDialogCommand.AddCommand(settingsInspectCommand);

        // settings-dialog status: SettingsDialog 열림 상태 확인
        var settingsStatusCommand = new Command("status", "SettingsDialog 열림 상태 확인");
        settingsStatusCommand.AddOption(jsonOption);
        settingsStatusCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_STATUS", "settings-dialog status"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Settings();
                var isOpen = controller.IsSettingsDialogOpen();

                if (json)
                {
                    PrintSuccess(new
                    {
                        dialogType = "SettingsDialog",
                        isOpen = isOpen
                    });
                }
                else
                {
                    Console.WriteLine(isOpen ? "[settings-dialog-status] SettingsDialog is open" : "[settings-dialog-status] SettingsDialog is not open");
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-status] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsDialogCommand.AddCommand(settingsStatusCommand);

        // settings-dialog path: Path 제어 (읽기/쓰기)
        var settingsPathCommand = new Command("path", "SettingsDialog Path 설정 제어");

        // settings-dialog path get-all: 모든 경로 읽기
        var settingsPathGetAllCommand = new Command("get-all", "모든 경로 읽기 (Line 1, Line 2, Output, Quarantine)");
        settingsPathGetAllCommand.AddOption(jsonOption);
        settingsPathGetAllCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_PATH_GET_ALL", "settings-dialog path get-all"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Settings();
                var line1Paths = controller.GetLine1Paths();
                var line2Paths = controller.GetLine2Paths();
                var outputPath = controller.GetOutputPath();
                var quarantinePath = controller.GetQuarantinePath();

                if (json)
                {
                    PrintSuccess(new
                    {
                        line1 = line1Paths,
                        line2 = line2Paths,
                        output = outputPath,
                        quarantine = quarantinePath
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-path get-all] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsPathCommand.AddCommand(settingsPathGetAllCommand);

        // settings-dialog path get-line1: Line 1 경로 읽기
        var settingsPathGetLine1Command = new Command("get-line1", "Line 1 경로 읽기");
        settingsPathGetLine1Command.AddOption(jsonOption);
        settingsPathGetLine1Command.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_PATH_GET_LINE1", "settings-dialog path get-line1"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Settings();
                var paths = controller.GetLine1Paths();

                if (json)
                {
                    PrintSuccess(new
                    {
                        line = "Line 1",
                        count = paths.Count,
                        paths = paths
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-path get-line1] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsPathCommand.AddCommand(settingsPathGetLine1Command);

        // settings-dialog path get-line2: Line 2 경로 읽기
        var settingsPathGetLine2Command = new Command("get-line2", "Line 2 경로 읽기");
        settingsPathGetLine2Command.AddOption(jsonOption);
        settingsPathGetLine2Command.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_PATH_GET_LINE2", "settings-dialog path get-line2"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Settings();
                var paths = controller.GetLine2Paths();

                if (json)
                {
                    PrintSuccess(new
                    {
                        line = "Line 2",
                        count = paths.Count,
                        paths = paths
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-path get-line2] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsPathCommand.AddCommand(settingsPathGetLine2Command);

        // settings-dialog path get-output: 출력 경로 읽기
        var settingsPathGetOutputCommand = new Command("get-output", "출력 경로 읽기");
        settingsPathGetOutputCommand.AddOption(jsonOption);
        settingsPathGetOutputCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_PATH_GET_OUTPUT", "settings-dialog path get-output"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Settings();
                var path = controller.GetOutputPath();

                if (json)
                {
                    PrintSuccess(new
                    {
                        pathType = "output",
                        path = path
                    });
                }
                else
                {
                    Console.WriteLine($"[settings-dialog-path get-output] Output Path: {path}");
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-path get-output] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsPathCommand.AddCommand(settingsPathGetOutputCommand);

        // settings-dialog path get-quarantine: 격리 경로 읽기
        var settingsPathGetQuarantineCommand = new Command("get-quarantine", "격리 경로 읽기");
        settingsPathGetQuarantineCommand.AddOption(jsonOption);
        settingsPathGetQuarantineCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_PATH_GET_QUARANTINE", "settings-dialog path get-quarantine"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Settings();
                var path = controller.GetQuarantinePath();

                if (json)
                {
                    PrintSuccess(new
                    {
                        pathType = "quarantine",
                        path = path
                    });
                }
                else
                {
                    Console.WriteLine($"[settings-dialog-path get-quarantine] Quarantine Path: {path}");
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-path get-quarantine] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsPathCommand.AddCommand(settingsPathGetQuarantineCommand);

        // settings-dialog path set: 경로 설정
        var settingsPathKeyArgument = new Argument<string>("key", "Path key (e.g., nir1, normal1, cam1-6)");
        var settingsPathValueArgument = new Argument<string>("value", "Path value to set");
        var settingsPathSetCommand = new Command("set", "경로 설정");
        settingsPathSetCommand.AddArgument(settingsPathKeyArgument);
        settingsPathSetCommand.AddArgument(settingsPathValueArgument);
        settingsPathSetCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_PATH_SET", "settings-dialog path set"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var key = context.ParseResult.GetValueForArgument(settingsPathKeyArgument);
                var value = context.ParseResult.GetValueForArgument(settingsPathValueArgument);

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
                        context.ExitCode = INVALID_ARGUMENT;
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
                    context.ExitCode = INVALID_ARGUMENT;
                    return;
                }

                if (result)
                {
                    Console.WriteLine($"[settings-dialog-path set] Success: {key} set to '{value}'");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    Console.WriteLine($"[settings-dialog-path set] Failed: Could not set {key}");
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-path set] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsPathCommand.AddCommand(settingsPathSetCommand);

        settingsDialogCommand.AddCommand(settingsPathCommand);

        // settings-dialog checkbox: CheckBox 제어
        var settingsCheckboxCommand = new Command("checkbox", "SettingsDialog CheckBox 제어");

        // settings-dialog checkbox get: CheckBox 상태 읽기
        var checkboxNameArgument = new Argument<string>("name", "CheckBox 이름 (use_folder_suffix, use_disk_cache, etc.)");
        var checkboxGetCommand = new Command("get", "CheckBox 상태 읽기");
        checkboxGetCommand.AddArgument(checkboxNameArgument);
        checkboxGetCommand.AddOption(jsonOption);
        checkboxGetCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_CHECKBOX_GET", "settings-dialog checkbox get"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var name = context.ParseResult.GetValueForArgument(checkboxNameArgument);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                using var controller = new Settings();
                var state = controller.GetCheckBoxState(null, name);

                if (json)
                {
                    PrintSuccess(new
                    {
                        checkbox = name,
                        isChecked = state
                    });
                }
                else
                {
                    Console.WriteLine($"[settings-dialog-checkbox get] '{name}': {(state ? "Checked" : "Unchecked")}");
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-checkbox get] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsCheckboxCommand.AddCommand(checkboxGetCommand);

        // settings-dialog checkbox set: CheckBox 상태 설정
        var checkboxValueArgument = new Argument<bool>("value", "CheckBox 값 (true/false)");
        var checkboxSetCommand = new Command("set", "CheckBox 상태 설정");
        checkboxSetCommand.AddArgument(checkboxNameArgument);
        checkboxSetCommand.AddArgument(checkboxValueArgument);
        checkboxSetCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_CHECKBOX_SET", "settings-dialog checkbox set"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var name = context.ParseResult.GetValueForArgument(checkboxNameArgument);
                var value = context.ParseResult.GetValueForArgument(checkboxValueArgument);

                using var controller = new Settings();
                var result = controller.SetCheckBoxState(null, name, value);
                if (result)
                {
                    Console.WriteLine($"[settings-dialog-checkbox set] Success: '{name}' set to {value}");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    Console.WriteLine($"[settings-dialog-checkbox set] Failed: Could not set '{name}'");
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-checkbox set] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsCheckboxCommand.AddCommand(checkboxSetCommand);

        // settings-dialog checkbox list: 모든 CheckBox 상태 목록
        var checkboxListCommand = new Command("list", "모든 CheckBox 상태 목록 (Advanced tab)");
        checkboxListCommand.AddOption(jsonOption);
        checkboxListCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_CHECKBOX_LIST", "settings-dialog checkbox list"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Settings();
                var settings = controller.GetAdvancedSettings();

                if (json)
                {
                    PrintSuccess(new
                    {
                        source = "AdvancedTab",
                        count = settings.Count,
                        settings = settings
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-checkbox list] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsCheckboxCommand.AddCommand(checkboxListCommand);

        settingsDialogCommand.AddCommand(settingsCheckboxCommand);

        // settings-dialog action: Dialog 동작 버튼 제어
        var settingsActionCommand = new Command("action", "SettingsDialog 동작 버튼 제어");

        // settings-dialog action save: Save/OK 버튼 클릭
        var actionSaveCommand = new Command("save", "Save/OK 버튼 클릭 (dialog closes)");
        actionSaveCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_ACTION_SAVE", "settings-dialog action save"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var controller = new Settings();
                var result = controller.ClickSaveButton();
                Console.WriteLine(result ? "[settings-dialog-action save] Success: Dialog saved and closed" : "[settings-dialog-action save] Failed: Could not click Save button or dialog did not close");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-action save] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsActionCommand.AddCommand(actionSaveCommand);

        // settings-dialog action apply: Apply 버튼 클릭
        var actionApplyCommand = new Command("apply", "Apply 버튼 클릭 (dialog stays open)");
        actionApplyCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_ACTION_APPLY", "settings-dialog action apply"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var controller = new Settings();
                var result = controller.ClickApplyButton();
                Console.WriteLine(result ? "[settings-dialog-action apply] Success: Apply button clicked" : "[settings-dialog-action apply] Failed: Could not click Apply button");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-action apply] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsActionCommand.AddCommand(actionApplyCommand);

        // settings-dialog action cancel: Cancel 버튼 클릭
        var actionCancelCommand = new Command("cancel", "Cancel 버튼 클릭 (dialog closes)");
        actionCancelCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_ACTION_CANCEL", "settings-dialog action cancel"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var controller = new Settings();
                var result = controller.ClickCancelButton();
                Console.WriteLine(result ? "[settings-dialog-action cancel] Success: Dialog cancelled and closed" : "[settings-dialog-action cancel] Failed: Could not click Cancel button or dialog did not close");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-action cancel] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsActionCommand.AddCommand(actionCancelCommand);

        // settings-dialog action reset: Reset/Defaults 버튼 클릭
        var actionResetCommand = new Command("reset", "Reset/Defaults 버튼 클릭 (dialog stays open)");
        actionResetCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "SETTINGS_DIALOG_ACTION_RESET", "settings-dialog action reset"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var controller = new Settings();
                var result = controller.ClickResetButton();
                Console.WriteLine(result ? "[settings-dialog-action reset] Success: Reset button clicked" : "[settings-dialog-action reset] Failed: Could not click Reset button");
                context.ExitCode = result ? SUCCESS : ERROR;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[settings-dialog-action reset] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        settingsActionCommand.AddCommand(actionResetCommand);

        settingsDialogCommand.AddCommand(settingsActionCommand);

        rootCommand.AddCommand(settingsDialogCommand);
    }
}
