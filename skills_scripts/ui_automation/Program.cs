using System.CommandLine;

namespace UiAutomation;

/// <summary>
/// Windows UI Automation CLI - FlaUI 5.x 기반
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Windows UI Automation - FlaUI 5.x 기반 CLI 도구");

        // list 명령: 모든 윈도우 나열
        var listCommand = new Command("list", "모든 윈도우 나열");
        listCommand.SetHandler(() =>
        {
            Console.WriteLine("Listing all windows...");
            // TODO: FlaUI를 사용한 윈도우 목록 구현
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
            if (!string.IsNullOrEmpty(title))
            {
                Console.WriteLine($"Finding window by title: {title}");
                // TODO: FlaUI를 사용한 제목 검색 구현
            }
            else if (!string.IsNullOrEmpty(process))
            {
                Console.WriteLine($"Finding window by process: {process}");
                // TODO: FlaUI를 사용한 프로세스 검색 구현
            }
            else
            {
                Console.WriteLine("Please specify --title or --process option");
            }
        }, titleOption, processOption);
        rootCommand.AddCommand(findCommand);

        return await rootCommand.InvokeAsync(args);
    }
}
