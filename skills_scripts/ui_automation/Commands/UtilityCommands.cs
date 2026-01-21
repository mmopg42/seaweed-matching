using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;
using static UiAutomation.Commands.ExitCodes;

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
        configPathCommand.SetHandler((InvocationContext context) =>
        {
            try
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
            try
            {
                var json = context.ParseResult.GetValueForOption(jsonConfigOption);

                var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"[Config] File not found: {configPath}");
                    context.ExitCode = NOT_FOUND;
                    return;
                }

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
        configGetCommand.AddOption(keyOption);
        configGetCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var key = context.ParseResult.GetValueForOption(keyOption);

                if (string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("[Config] --key parameter is required");
                    context.ExitCode = INVALID_ARGUMENT;
                    return;
                }

                var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var configPath = Path.Combine(localAppDataPath, "prische", "ChronoView", "config.json");

                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"[Config] File not found: {configPath}");
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
                        Console.WriteLine($"[Config] Key not found: {key}");
                        context.ExitCode = NOT_FOUND;
                        return;
                    }
                }

                Console.WriteLine($"[{key}] {current}");
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
