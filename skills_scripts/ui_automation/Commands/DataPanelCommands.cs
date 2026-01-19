using System.CommandLine;
using System.Text.Json;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;

namespace UiAutomation.Commands;

/// <summary>
/// Data panel commands for ChronoView data panel automation (ChronoDataPanelReader).
/// Provides commands to read StatisticsPanel and FileGroupDataGrid data.
/// </summary>
public class DataPanelCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_INVALID_ARGUMENT = 4;

    /// <summary>
    /// Registers all data panel commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // Commands will be registered in Task 2
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
