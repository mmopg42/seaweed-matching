using System.CommandLine;
using UiAutomation.Commands;

namespace UiAutomation;

/// <summary>
/// Windows UI Automation CLI - FlaUI 5.x 기반
/// </summary>
class Program
{
    // Global options state
    static bool s_isQuiet = false;
    static bool s_isVerbose = false;

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

        // Register modular command handlers
        registry.RegisterHandler(new LegacyCommands());
        registry.RegisterHandler(new WindowsCommands());
        registry.RegisterHandler(new ToolbarCommands());
        registry.RegisterHandler(new DataPanelCommands());
        registry.RegisterHandler(new WorkflowCommands());
        registry.RegisterHandler(new SettingsCommands());
        registry.RegisterHandler(new FileOpsCommands());
        registry.RegisterHandler(new TestCommands());
        registry.RegisterHandler(new UtilityCommands());
        registry.RegisterHandler(new AppLifecycleCommands());
        registry.RegisterAllCommands(rootCommand);

        // Parse args to capture global options before command execution
        var parseResult = rootCommand.Parse(args);
        s_isQuiet = parseResult.GetValueForOption(quietOption) == true;
        s_isVerbose = parseResult.GetValueForOption(verboseOption) == true;

        return await rootCommand.InvokeAsync(args);
    }
}
