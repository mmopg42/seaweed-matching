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
        // Phase 17-01: Register test, scenario, and batch commands
        registry.RegisterHandler(new TestCommands());
        registry.RegisterAllCommands(rootCommand);

        // Parse args to capture global options before command execution
        var parseResult = rootCommand.Parse(args);
        s_isQuiet = parseResult.GetValueForOption(quietOption) == true;
        s_isVerbose = parseResult.GetValueForOption(verboseOption) == true;

        return await rootCommand.InvokeAsync(args);
    }
}
