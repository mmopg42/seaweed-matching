using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;
using Toolbar = SkillsScripts.UiAutomation.ChronoToolbarController;
using static UiAutomation.Commands.ExitCodes;

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
        toolbarStartCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new Toolbar();
                var result = controller.ClickStartButton();
                if (result)
                {
                    PrintOutput("[toolbar-start] Success: Start button clicked");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    PrintOutput("[toolbar-start] Failed: Could not click Start button");
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
        toolbarStopCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new Toolbar();
                var result = controller.ClickStopButton();
                if (result)
                {
                    PrintOutput("[toolbar-stop] Success: Stop button clicked");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    PrintOutput("[toolbar-stop] Failed: Could not click Stop button");
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
        toolbarSettingsCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new Toolbar();
                var result = controller.ClickSettingsButton();
                if (result)
                {
                    PrintOutput("[toolbar-settings] Success: Settings button clicked");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    PrintOutput("[toolbar-settings] Failed: Could not click Settings button");
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
        toolbarRefreshCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new Toolbar();
                var result = controller.ClickRefreshButton();
                if (result)
                {
                    PrintOutput("[toolbar-refresh] Success: Refresh button clicked");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    PrintOutput("[toolbar-refresh] Failed: Could not click Refresh button");
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
        toolbarMoveCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new Toolbar();
                var result = controller.ClickMoveButton();
                if (result)
                {
                    PrintOutput("[toolbar-move] Success: Move button clicked");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    PrintOutput("[toolbar-move] Failed: Could not click Move button");
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
        toolbarDeleteCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                using var controller = new Toolbar();
                var result = controller.ClickDeleteButton();
                if (result)
                {
                    PrintOutput("[toolbar-delete] Success: Delete button clicked");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    PrintOutput("[toolbar-delete] Failed: Could not click Delete button");
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
            try
            {
                var json = context.ParseResult.GetValueForOption(jsonOption);

                using var controller = new Toolbar();
                var buttons = controller.GetAvailableButtons();

                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = true,
                        data = new
                        {
                            count = buttons.Length,
                            buttons = buttons
                        }
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
        toolbarClickCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var text = context.ParseResult.GetValueForArgument(buttonTextArgument);

                using var controller = new Toolbar();
                var result = controller.ClickToolbarButton(text);
                if (result)
                {
                    PrintOutput($"[toolbar-click] Success: Button '{text}' clicked");
                    context.ExitCode = SUCCESS;
                }
                else
                {
                    PrintOutput($"[toolbar-click] Failed: Could not click button '{text}'");
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
        toolbarEnabledCommand.SetHandler((InvocationContext context) =>
        {
            try
            {
                var text = context.ParseResult.GetValueForArgument(buttonTextArgument);

                using var controller = new Toolbar();
                var isEnabled = controller.IsButtonEnabled(text);
                PrintOutput(isEnabled ? $"[toolbar-enabled] Button '{text}' is enabled" : $"[toolbar-enabled] Button '{text}' is disabled");
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
