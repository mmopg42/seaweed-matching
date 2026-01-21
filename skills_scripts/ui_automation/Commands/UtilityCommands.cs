using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;
using static UiAutomation.Commands.ExitCodes;
using static UiAutomation.Commands.JsonResponseHelper;
using static UiAutomation.Commands.DryRunHandler;

namespace UiAutomation.Commands;

/// <summary>
/// Utility and diagnostic commands for ChronoView automation.
/// Provides utility commands for inspecting UI structure and reading configuration files:
/// - inspect: UI element structure inspection (workflow, log)
/// - config: Configuration file direct reading (path, read, get)
/// </summary>
public class UtilityCommands : ICommandHandler
{
    /// <summary>
    /// Registers all inspect and config commands with the root command.
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
        // inspect 명령: UI 요소 구조 검사
        // ============================================================

        var inspectCommand = new Command("inspect", "UI 요소 구조 검사");

        // inspect workflow: WorkflowPanel 구조 검사
        var inspectWorkflowCommand = new Command("workflow", "WorkflowPanel 구조 검사");
        inspectWorkflowCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "UTILITY_INSPECT_WORKFLOW", "inspect workflow"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var automation = new UiAuto();
                var mainWindow = automation.FindChronoViewMainWindow();
                if (mainWindow == null)
                {
                    Console.WriteLine("[inspect-workflow] Failed: MainWindow not found");
                    context.ExitCode = NOT_FOUND;
                    return;
                }

                var workflowPanel = automation.FindWorkflowPanel(mainWindow);
                if (workflowPanel == null)
                {
                    Console.WriteLine("[inspect-workflow] Failed: WorkflowPanel not found");
                    context.ExitCode = NOT_FOUND;
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[inspect-workflow] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        inspectCommand.AddCommand(inspectWorkflowCommand);

        // inspect log: LogPanel 구조 검사
        var inspectLogCommand = new Command("log", "LogPanel 구조 검사");
        inspectLogCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "UTILITY_INSPECT_LOG", "inspect log"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                using var reader = new SkillsScripts.UiAutomation.ChronoDataPanelReader();
                var mainWindow = reader.FindMainWindow();
                if (mainWindow == null)
                {
                    Console.WriteLine("[inspect-log] Failed: MainWindow not found");
                    context.ExitCode = NOT_FOUND;
                    return;
                }

                var logPanel = reader.FindLogPanel(mainWindow);
                if (logPanel == null)
                {
                    Console.WriteLine("[inspect-log] Failed: LogPanel not found");
                    context.ExitCode = NOT_FOUND;
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
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[inspect-log] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        inspectCommand.AddCommand(inspectLogCommand);

        rootCommand.AddCommand(inspectCommand);

        // ============================================================
        // config: Config 파일 직접 읽기 (UI Automation 없이 파일 시스템에서 직접 확인)
        // ============================================================

        var configCommand = new Command("config", "Config 파일 직접 읽기");

        // config path: Config 파일 위치 확인
        var configPathCommand = new Command("path", "Config 파일 위치 확인");
        var jsonPathOption = new Option<bool>(["--json", "-j"], "JSON 형식으로 출력");
        configPathCommand.AddOption(jsonPathOption);
        configPathCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "UTILITY_CONFIG_PATH", "config path"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonPathOption);
                var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");
                var exists = File.Exists(configPath);

                if (json)
                {
                    PrintSuccess(new
                    {
                        path = configPath,
                        exists = exists,
                        size = exists ? new FileInfo(configPath).Length : 0,
                        modified = exists ? new FileInfo(configPath).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss") : null
                    });
                }
                else
                {
                    Console.WriteLine($"[Config Path] {configPath}");
                    Console.WriteLine($"[Exists] {exists}");

                    if (exists)
                    {
                        var fileInfo = new FileInfo(configPath);
                        Console.WriteLine($"[Size] {fileInfo.Length} bytes");
                        Console.WriteLine($"[Modified] {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    }
                }

                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[config path] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        configCommand.AddCommand(configPathCommand);

        // config read: Config 파일 내용 읽기
        var configReadCommand = new Command("read", "Config 파일 내용 읽기");
        var jsonConfigOption = new Option<bool>(["--json", "-j"], "JSON 형식으로 출력");
        configReadCommand.AddOption(jsonConfigOption);
        configReadCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "UTILITY_CONFIG_READ", "config read"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonConfigOption);

                var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");

                if (!File.Exists(configPath))
                {
                    if (json)
                    {
                        PrintError($"Config file not found: {configPath}", NOT_FOUND,
                            "Ensure ChronoView has been run at least once to generate config.");
                    }
                    else
                    {
                        Console.WriteLine($"[Config] File not found: {configPath}");
                    }
                    context.ExitCode = NOT_FOUND;
                    return;
                }

                var jsonContent = File.ReadAllText(configPath);

                if (json)
                {
                    // Parse and return in standard format
                    using var jsonDoc = JsonDocument.Parse(jsonContent);
                    PrintSuccess(new
                    {
                        path = configPath,
                        content = jsonDoc.RootElement
                    });
                }
                else
                {
                    // Pretty print JSON for human reading
                    using var jsonDoc = JsonDocument.Parse(jsonContent);
                    var prettyJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    Console.WriteLine($"[Config] Reading from: {configPath}");
                    Console.WriteLine();
                    Console.WriteLine("=== Raw JSON ===");
                    Console.WriteLine(prettyJson);
                }

                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[config read] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        configCommand.AddCommand(configReadCommand);

        // config get: 특정 설정 값 읽기 (경로 등)
        var configGetCommand = new Command("get", "특정 설정 값 읽기");
        var keyOption = new Option<string>(["--key", "-k"], "설정 키 (예: folderPaths.line1SampleName)");
        var jsonGetOption = new Option<bool>(["--json", "-j"], "JSON 형식으로 출력");
        configGetCommand.AddOption(keyOption);
        configGetCommand.AddOption(jsonGetOption);
        configGetCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "UTILITY_CONFIG_GET", "config get"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var key = context.ParseResult.GetValueForOption(keyOption);
                var json = context.ParseResult.GetValueForOption(jsonGetOption);

                if (string.IsNullOrEmpty(key))
                {
                    if (json)
                    {
                        PrintError("--key parameter is required", INVALID_ARGUMENT,
                            "Provide a config key like: folderPaths.line1SampleName");
                    }
                    else
                    {
                        Console.WriteLine("[Config] --key parameter is required");
                    }
                    context.ExitCode = INVALID_ARGUMENT;
                    return;
                }

                var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");

                if (!File.Exists(configPath))
                {
                    if (json)
                    {
                        PrintError($"Config file not found: {configPath}", NOT_FOUND,
                            "Ensure ChronoView has been run at least once to generate config.");
                    }
                    else
                    {
                        Console.WriteLine($"[Config] File not found: {configPath}");
                    }
                    context.ExitCode = NOT_FOUND;
                    return;
                }

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
                        if (json)
                        {
                            PrintError($"Config key not found: {key}", NOT_FOUND,
                                "Use 'config read --json' to see available keys.");
                        }
                        else
                        {
                            Console.WriteLine($"[Config] Key not found: {key}");
                        }
                        context.ExitCode = NOT_FOUND;
                        return;
                    }
                }

                if (json)
                {
                    PrintSuccess(new
                    {
                        key = key,
                        value = current
                    });
                }
                else
                {
                    Console.WriteLine($"[{key}] {current}");
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[config get] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        configCommand.AddCommand(configGetCommand);

        rootCommand.AddCommand(configCommand);
    }
}
