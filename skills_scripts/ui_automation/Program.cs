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
        // Phase 18-01: Register inspect and config commands
        registry.RegisterHandler(new UtilityCommands());
        registry.RegisterAllCommands(rootCommand);

        // Parse args to capture global options before command execution
        var parseResult = rootCommand.Parse(args);
        s_isQuiet = parseResult.GetValueForOption(quietOption) == true;
        s_isVerbose = parseResult.GetValueForOption(verboseOption) == true;

        return await rootCommand.InvokeAsync(args);
    }
}
