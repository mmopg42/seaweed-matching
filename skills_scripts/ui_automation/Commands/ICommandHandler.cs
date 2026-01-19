using System.CommandLine;

namespace UiAutomation.Commands;

/// <summary>
/// Defines a command handler that registers its commands with the root command.
/// Implementations of this interface encapsulate groups of related CLI commands
/// and handle their registration with the System.CommandLine RootCommand.
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Registers all commands managed by this handler with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    void RegisterCommands(RootCommand rootCommand);
}
