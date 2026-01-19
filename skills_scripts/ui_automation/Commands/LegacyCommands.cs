using System.CommandLine;

namespace UiAutomation.Commands;

/// <summary>
/// Legacy commands from early development (detect, list, find, click).
/// These are superseded by more specific command groups but retained for compatibility.
/// </summary>
public class LegacyCommands : ICommandHandler
{
    /// <summary>
    /// Registers all legacy commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // Commands will be registered in Task 2
    }
}
