using System.CommandLine;
using System.Text.Json;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;

namespace UiAutomation.Commands;

/// <summary>
/// Toolbar commands for ChronoView toolbar automation (ChronoToolbarController).
/// Provides commands to interact with toolbar buttons.
/// </summary>
public class ToolbarCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;

    /// <summary>
    /// Registers all toolbar commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // JSON output option
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // toolbar 명령: ChronoToolbarController 기반 통합 툴바 컨트롤
        var toolbarCommand = new Command("toolbar", "툴바 버튼 제어 (ChronoToolbarController)");

        rootCommand.AddCommand(toolbarCommand);
    }

    /// <summary>
    /// Print JSON output with consistent formatting for programmatic consumption
    /// </summary>
    private static void PrintJsonOutput(object data)
    {
        Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        }));
    }

    /// <summary>
    /// Print output only if not in quiet mode
    /// </summary>
    private static void PrintOutput(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Print verbose output only if verbose mode is enabled
    /// </summary>
    private static void PrintVerbose(string message)
    {
        Console.WriteLine($"[VERBOSE] {message}");
    }
}
