using System.CommandLine;
using UiAuto = SkillsScripts.UiAutomation.UiAutomation;

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
        // detect 명령: ChronoView MainWindow 감지 및 정보 출력
        var detectCommand = new Command("detect", "ChronoView MainWindow 감지 및 속성 출력");
        detectCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            automation.PrintMainWindowInfo();
        });
        rootCommand.AddCommand(detectCommand);

        // list 명령: 모든 윈도우 나열
        var listCommand = new Command("list", "모든 윈도우 나열");
        listCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var windows = automation.FindAllChronoViewWindows();
            Console.WriteLine($"Found {windows.Count} ChronoView window(s)");
        });
        rootCommand.AddCommand(listCommand);

        // find 명령: 윈도우 찾기
        var titleOption = new Option<string>(
            ["--title", "-t"],
            "윈도우 제목으로 검색"
        );
        var processOption = new Option<string>(
            ["--process", "-p"],
            "프로세스 이름으로 검색"
        );

        var findCommand = new Command("find", "윈도우 찾기");
        findCommand.AddOption(titleOption);
        findCommand.AddOption(processOption);
        findCommand.SetHandler((title, process) =>
        {
            using var automation = new UiAuto();

            if (!string.IsNullOrEmpty(title))
            {
                Console.WriteLine($"Finding window by title: {title}");
                var window = automation.FindWindowByTitle(title, substring: true);
                if (window != null)
                {
                    Console.WriteLine($"Found: '{window.Name}'");
                    automation.GetWindowProperties(window);
                }
                else
                {
                    Console.WriteLine("Window not found");
                }
            }
            else if (!string.IsNullOrEmpty(process))
            {
                Console.WriteLine($"Finding window by process: {process}");
                var window = automation.FindWindowByProcess(process);
                if (window != null)
                {
                    Console.WriteLine($"Found: '{window.Name}'");
                    automation.GetWindowProperties(window);
                }
                else
                {
                    Console.WriteLine("Window not found");
                }
            }
            else
            {
                Console.WriteLine("Please specify --title or --process option");
            }
        }, titleOption, processOption);
        rootCommand.AddCommand(findCommand);

        // click 명령: Toolbar 버튼 클릭
        var clickCommand = new Command("click", "Toolbar 버튼 클릭");

        // click start: Start 버튼 클릭
        var clickStartCommand = new Command("start", "Start 버튼 클릭");
        clickStartCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-start] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickStartButton(mainWindow);
            Console.WriteLine(result ? "[click-start] Success: Start button clicked" : "[click-start] Failed: Could not click Start button");
        });
        clickCommand.AddCommand(clickStartCommand);

        // click stop: Stop 버튼 클릭
        var clickStopCommand = new Command("stop", "Stop 버튼 클릭");
        clickStopCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-stop] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickStopButton(mainWindow);
            Console.WriteLine(result ? "[click-stop] Success: Stop button clicked" : "[click-stop] Failed: Could not click Stop button");
        });
        clickCommand.AddCommand(clickStopCommand);

        // click settings: Settings (Setup) 버튼 클릭
        var clickSettingsCommand = new Command("settings", "Settings (Setup) 버튼 클릭");
        clickSettingsCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-settings] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickSettingsButton(mainWindow);
            if (result)
            {
                Console.WriteLine("[click-settings] Success: Settings button clicked, SetupWindow should open");
            }
            else
            {
                Console.WriteLine("[click-settings] Failed: Could not click Settings button");
            }
        });
        clickCommand.AddCommand(clickSettingsCommand);

        // click refresh: Refresh 버튼 클릭
        var clickRefreshCommand = new Command("refresh", "Refresh 버튼 클릭");
        clickRefreshCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-refresh] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickRefreshButton(mainWindow);
            Console.WriteLine(result ? "[click-refresh] Success: Refresh button clicked" : "[click-refresh] Failed: Could not click Refresh button");
        });
        clickCommand.AddCommand(clickRefreshCommand);

        // click move: Move 버튼 클릭
        var clickMoveCommand = new Command("move", "Move 버튼 클릭");
        clickMoveCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-move] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickMoveButton(mainWindow);
            Console.WriteLine(result ? "[click-move] Success: Move button clicked" : "[click-move] Failed: Could not click Move button");
        });
        clickCommand.AddCommand(clickMoveCommand);

        // click delete: Delete 버튼 클릭
        var clickDeleteCommand = new Command("delete", "Delete 버튼 클릭");
        clickDeleteCommand.SetHandler(() =>
        {
            using var automation = new UiAuto();
            var mainWindow = automation.FindChronoViewMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[click-delete] Failed: MainWindow not found");
                return;
            }

            var result = automation.ClickDeleteButton(mainWindow);
            Console.WriteLine(result ? "[click-delete] Success: Delete button clicked" : "[click-delete] Failed: Could not click Delete button");
        });
        clickCommand.AddCommand(clickDeleteCommand);

        rootCommand.AddCommand(clickCommand);
    }
}
