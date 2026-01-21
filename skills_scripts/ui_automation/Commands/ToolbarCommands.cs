using System.CommandLine;
using System.CommandLine.Invocation;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;
using static UiAutomation.Commands.ExitCodes;
using static UiAutomation.Commands.JsonResponseHelper;
using static UiAutomation.Commands.DryRunHandler;

namespace UiAutomation.Commands;

/// <summary>
/// Toolbar commands for ChronoView toolbar automation (ChronoToolbarController).
/// Provides commands to interact with toolbar buttons.
/// </summary>
public class ToolbarCommands : ICommandHandler
{
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

        // toolbar start: Start 버튼 클릭
        var toolbarStartCommand = new Command("start", "Start 버튼 클릭");
        toolbarStartCommand.AddOption(jsonOption);
        toolbarStartCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_START", "toolbar start"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Toolbar();
                var result = controller.ClickStartButton();
                if (result)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            clicked = true,
                            button = "Start"
                        });
                    }
                    else
                    {
                        PrintOutput("[toolbar-start] Success: Start button clicked");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("'Start' button not found or not clickable", NOT_FOUND,
                            "Check if MainWindow is active. Try 'windows main --json' first.");
                    }
                    else
                    {
                        PrintOutput("[toolbar-start] Failed: Could not click Start button");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-start] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarStartCommand);

        // toolbar stop: Stop 버튼 클릭
        var toolbarStopCommand = new Command("stop", "Stop 버튼 클릭");
        toolbarStopCommand.AddOption(jsonOption);
        toolbarStopCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_STOP", "toolbar stop"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Toolbar();
                var result = controller.ClickStopButton();
                if (result)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            clicked = true,
                            button = "Stop"
                        });
                    }
                    else
                    {
                        PrintOutput("[toolbar-stop] Success: Stop button clicked");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("'Stop' button not found or not clickable", NOT_FOUND,
                            "Check if MainWindow is active. Try 'windows main --json' first.");
                    }
                    else
                    {
                        PrintOutput("[toolbar-stop] Failed: Could not click Stop button");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-stop] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarStopCommand);

        // toolbar settings: Settings (Setup) 버튼 클릭
        var toolbarSettingsCommand = new Command("settings", "Settings (Setup) 버튼 클릭");
        toolbarSettingsCommand.AddOption(jsonOption);
        toolbarSettingsCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_SETTINGS", "toolbar settings"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Toolbar();
                var result = controller.ClickSettingsButton();
                if (result)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            clicked = true,
                            button = "Settings"
                        });
                    }
                    else
                    {
                        PrintOutput("[toolbar-settings] Success: Settings button clicked");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("'Settings' button not found or not clickable", NOT_FOUND,
                            "Check if MainWindow is active. Try 'windows main --json' first.");
                    }
                    else
                    {
                        PrintOutput("[toolbar-settings] Failed: Could not click Settings button");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-settings] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarSettingsCommand);

        // toolbar refresh: Refresh 버튼 클릭
        var toolbarRefreshCommand = new Command("refresh", "Refresh 버튼 클릭");
        toolbarRefreshCommand.AddOption(jsonOption);
        toolbarRefreshCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_REFRESH", "toolbar refresh"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Toolbar();
                var result = controller.ClickRefreshButton();
                if (result)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            clicked = true,
                            button = "Refresh"
                        });
                    }
                    else
                    {
                        PrintOutput("[toolbar-refresh] Success: Refresh button clicked");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("'Refresh' button not found or not clickable", NOT_FOUND,
                            "Check if MainWindow is active. Try 'windows main --json' first.");
                    }
                    else
                    {
                        PrintOutput("[toolbar-refresh] Failed: Could not click Refresh button");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-refresh] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarRefreshCommand);

        // toolbar move: Move 버튼 클릭
        var toolbarMoveCommand = new Command("move", "Move 버튼 클릭");
        toolbarMoveCommand.AddOption(jsonOption);
        toolbarMoveCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_MOVE", "toolbar move"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Toolbar();
                var result = controller.ClickMoveButton();
                if (result)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            clicked = true,
                            button = "Move"
                        });
                    }
                    else
                    {
                        PrintOutput("[toolbar-move] Success: Move button clicked");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("'Move' button not found or not clickable", NOT_FOUND,
                            "Check if MainWindow is active. Try 'windows main --json' first.");
                    }
                    else
                    {
                        PrintOutput("[toolbar-move] Failed: Could not click Move button");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-move] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarMoveCommand);

        // toolbar delete: Delete 버튼 클릭
        var toolbarDeleteCommand = new Command("delete", "Delete 버튼 클릭");
        toolbarDeleteCommand.AddOption(jsonOption);
        toolbarDeleteCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_DELETE", "toolbar delete"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);
                using var controller = new Toolbar();
                var result = controller.ClickDeleteButton();
                if (result)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            clicked = true,
                            button = "Delete"
                        });
                    }
                    else
                    {
                        PrintOutput("[toolbar-delete] Success: Delete button clicked");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError("'Delete' button not found or not clickable", NOT_FOUND,
                            "Check if MainWindow is active. Try 'windows main --json' first.");
                    }
                    else
                    {
                        PrintOutput("[toolbar-delete] Failed: Could not click Delete button");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-delete] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarDeleteCommand);

        // toolbar list: 모든 툴바 버튼 나열
        var toolbarListCommand = new Command("list", "모든 툴바 버튼 나열");
        toolbarListCommand.AddOption(jsonOption);
        toolbarListCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_LIST", "toolbar list"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);

                using var controller = new Toolbar();
                var buttons = controller.GetAvailableButtons();

                if (json)
                {
                    PrintSuccess(new
                    {
                        count = buttons.Length,
                        buttons = buttons
                    });
                }
                else
                {
                    PrintOutput($"[toolbar-list] Found {buttons.Length} toolbar button(s):");
                    foreach (var button in buttons)
                    {
                        PrintOutput($"  - '{button}'");
                    }
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-list] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarListCommand);

        // toolbar click: 지정한 텍스트의 버튼 클릭
        var buttonTextArgument = new Argument<string>("text", "버튼 텍스트 (예: '시작', '중지', '설정')");
        var toolbarClickCommand = new Command("click", "지정한 텍스트의 버튼 클릭");
        toolbarClickCommand.AddArgument(buttonTextArgument);
        toolbarClickCommand.AddOption(jsonOption);
        toolbarClickCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_CLICK", "toolbar click"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var text = context.ParseResult.GetValueForArgument(buttonTextArgument);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                using var controller = new Toolbar();
                var result = controller.ClickToolbarButton(text);
                if (result)
                {
                    if (json)
                    {
                        PrintSuccess(new
                        {
                            clicked = true,
                            button = text
                        });
                    }
                    else
                    {
                        PrintOutput($"[toolbar-click] Success: Button '{text}' clicked");
                    }
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    if (json)
                    {
                        PrintError($"Button '{text}' not found", NOT_FOUND,
                            "Use 'toolbar list --json' to see available buttons.");
                    }
                    else
                    {
                        PrintOutput($"[toolbar-click] Failed: Could not click button '{text}'");
                    }
                    context.ExitCode = ERROR;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-click] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarClickCommand);

        // toolbar enabled: 버튼 활성화 상태 확인
        var toolbarEnabledCommand = new Command("enabled", "버튼 활성화 상태 확인");
        toolbarEnabledCommand.AddArgument(buttonTextArgument);
        toolbarEnabledCommand.AddOption(jsonOption);
        toolbarEnabledCommand.SetHandler((InvocationContext context) =>
        {
            // Dry-run check - return early if in dry-run mode
            if (CheckDryRun(context, "TOOLBAR_ENABLED", "toolbar enabled"))
            {
                return; // Dry-run response already printed
            }

            try
            {
                var text = context.ParseResult.GetValueForArgument(buttonTextArgument);
                var json = context.ParseResult.GetValueForOption(jsonOption);

                using var controller = new Toolbar();
                var isEnabled = controller.IsButtonEnabled(text);
                if (json)
                {
                    PrintSuccess(new
                    {
                        button = text,
                        enabled = isEnabled
                    });
                }
                else
                {
                    PrintOutput(isEnabled ? $"[toolbar-enabled] Button '{text}' is enabled" : $"[toolbar-enabled] Button '{text}' is disabled");
                }
                context.ExitCode = SUCCESS;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[toolbar-enabled] Error: {ex.Message}");
                context.ExitCode = ERROR;
            }
        });
        toolbarCommand.AddCommand(toolbarEnabledCommand);

        rootCommand.AddCommand(toolbarCommand);
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
