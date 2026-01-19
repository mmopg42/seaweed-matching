using System.CommandLine;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using Settings = SkillsScripts.UiAutomation.ChronoSettingsController;
using ConsoleLogs = SkillsScripts.UiAutomation.ConsoleLogsReader;

namespace UiAutomation.Commands;

/// <summary>
/// Settings dialog and console log commands for ChronoView automation.
/// Provides commands to control SettingsDialog (open, close, inspect, status, paths, checkboxes, actions)
/// and read console log files (list, tail, search).
/// </summary>
public class SettingsCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_INVALID_ARGUMENT = 4;

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

        // TODO: Task 2 will add all console-logs and settings-dialog commands here
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
}
