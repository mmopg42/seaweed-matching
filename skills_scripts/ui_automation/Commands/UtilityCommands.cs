using System.CommandLine;
using System.Text.Json;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;
using Finder = SkillsScripts.UiAutomation.ChronoWindowFinder;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;

namespace UiAutomation.Commands;

/// <summary>
/// Utility and diagnostic commands for ChronoView automation.
/// Provides utility commands for inspecting UI structure and reading configuration files:
/// - inspect: UI element structure inspection (workflow, log)
/// - config: Configuration file direct reading (path, read, get)
/// </summary>
public class UtilityCommands : ICommandHandler
{
    // Exit code constants
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_TIMEOUT = 3;
    private const int EXIT_INVALID_ARGUMENT = 4;

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
    }
}
