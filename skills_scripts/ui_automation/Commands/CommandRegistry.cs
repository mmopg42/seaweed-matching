using System.CommandLine;

namespace UiAutomation.Commands;

/// <summary>
/// Centralized registry for command handlers.
/// Provides a decoupled way to register command handlers with the root command,
/// allowing Program.cs to remain minimal as commands are extracted into separate modules.
/// </summary>
public class CommandRegistry
{
    private readonly List<ICommandHandler> _handlers = new();

    /// <summary>
    /// Registers a command handler with the registry.
    /// </summary>
    /// <param name="handler">The command handler to register.</param>
    public void RegisterHandler(ICommandHandler handler)
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _handlers.Add(handler);
    }

    /// <summary>
    /// Registers all commands from registered handlers with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterAllCommands(RootCommand rootCommand)
    {
        if (rootCommand == null)
        {
            throw new ArgumentNullException(nameof(rootCommand));
        }

        foreach (var handler in _handlers)
        {
            handler.RegisterCommands(rootCommand);
        }
    }

    /// <summary>
    /// Gets the number of registered handlers.
    /// </summary>
    /// <returns>The count of registered command handlers.</returns>
    public int GetHandlerCount() => _handlers.Count;
}
