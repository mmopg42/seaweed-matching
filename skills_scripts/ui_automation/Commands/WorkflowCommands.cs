using System.CommandLine;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using Workflow = SkillsScripts.UiAutomation.ChronoWorkflowController;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;

namespace UiAutomation.Commands;

/// <summary>
/// Workflow and log panel commands for ChronoView automation.
/// Provides commands to control WorkflowPanel (camera operations, path management)
/// and read LogPanel data (get logs, filter, search).
/// </summary>
public class WorkflowCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_INVALID_ARGUMENT = 4;

    /// <summary>
    /// Registers all workflow and log panel commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // JSON output option
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // workflow 명령: ChronoWorkflowController 기반 워크플로우 패널 제어
        var workflowCommand = new Command("workflow", "WorkflowPanel 제어 (ChronoWorkflowController)");

        // workflow launch-general: General Camera 버튼 클릭
        var wfLaunchGeneralCommand = new Command("launch-general", "General Camera 버튼 클릭");
        wfLaunchGeneralCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickGeneralCameraButton();
            if (result)
            {
                Console.WriteLine("[workflow-launch-general] Success: General Camera button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-launch-general] Failed: Could not click General Camera button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfLaunchGeneralCommand);

        // workflow launch-nir: NIR 1 Camera 버튼 클릭
        var wfLaunchNirCommand = new Command("launch-nir", "NIR 1 Camera 버튼 클릭");
        wfLaunchNirCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickNirCameraButton();
            if (result)
            {
                Console.WriteLine("[workflow-launch-nir] Success: NIR 1 Camera button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-launch-nir] Failed: Could not click NIR 1 Camera button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfLaunchNirCommand);

        // workflow launch-nir2: NIR 2 Camera 버튼 클릭
        var wfLaunchNir2Command = new Command("launch-nir2", "NIR 2 Camera 버튼 클릭");
        wfLaunchNir2Command.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ClickNir2CameraButton();
            if (result)
            {
                Console.WriteLine("[workflow-launch-nir2] Success: NIR 2 Camera button clicked");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-launch-nir2] Failed: Could not click NIR 2 Camera button");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfLaunchNir2Command);

        // workflow toggle-filtering: NIR Filtering 토글
        var wfToggleFilteringCommand = new Command("toggle-filtering", "NIR Filtering 토글");
        wfToggleFilteringCommand.SetHandler(() =>
        {
            using var controller = new Workflow();
            var result = controller.ToggleNir2Filtering();
            if (result)
            {
                Console.WriteLine("[workflow-toggle-filtering] Success: NIR Filtering toggled");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine("[workflow-toggle-filtering] Failed: Could not toggle NIR Filtering");
                Environment.Exit(EXIT_ERROR);
            }
        });
        workflowCommand.AddCommand(wfToggleFilteringCommand);

        // workflow camera-states: 모든 카메라 상태 읽기
        var wfCameraStatesCommand = new Command("camera-states", "모든 카메라 상태 읽기");
        wfCameraStatesCommand.AddOption(jsonOption);
        wfCameraStatesCommand.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var states = controller.GetCameraStates();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "WorkflowPanel",
                        count = states.Count,
                        states = states
                    }
                });
            }
            else
            {
                Console.WriteLine($"[workflow-camera-states] Found {states.Count} camera state(s):");
                foreach (var kvp in states)
                {
                    Console.WriteLine($"  - {kvp.Key}: {kvp.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowCommand.AddCommand(wfCameraStatesCommand);

        // workflow path: Path 제어 (읽기/쓰기)
        var workflowPathCommand = new Command("path", "WorkflowPanel Path 설정 제어");

        // workflow path get-line1: Line 1 경로 읽기
        var pathGetLine1Command = new Command("get-line1", "Line 1 경로 모두 읽기");
        pathGetLine1Command.AddOption(jsonOption);
        pathGetLine1Command.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var paths = controller.GetLine1Paths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        line = "Line1",
                        count = paths.Count,
                        paths = paths
                    }
                });
            }
            else
            {
                Console.WriteLine("[workflow-path get-line1] Line 1 Paths:");
                Console.WriteLine($"  SampleName: {paths.GetValueOrDefault("SampleName", "(not found)")}");
                Console.WriteLine($"  MoveNIR: {paths.GetValueOrDefault("MoveNir", "(not found)")}");
                Console.WriteLine($"  MoveAllData: {paths.GetValueOrDefault("MoveAllData", "(not found)")}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowPathCommand.AddCommand(pathGetLine1Command);

        // workflow path get-line2: Line 2 경로 읽기
        var pathGetLine2Command = new Command("get-line2", "Line 2 경로 모두 읽기");
        pathGetLine2Command.AddOption(jsonOption);
        pathGetLine2Command.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var paths = controller.GetLine2Paths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        line = "Line2",
                        count = paths.Count,
                        paths = paths
                    }
                });
            }
            else
            {
                Console.WriteLine("[workflow-path get-line2] Line 2 Paths:");
                Console.WriteLine($"  SampleName: {paths.GetValueOrDefault("SampleName", "(not found)")}");
                Console.WriteLine($"  MoveNIR: {paths.GetValueOrDefault("MoveNir", "(not found)")}");
                Console.WriteLine($"  MoveAllData: {paths.GetValueOrDefault("MoveAllData", "(not found)")}");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowPathCommand.AddCommand(pathGetLine2Command);

        // workflow path get-all: 모든 라인 경로 읽기
        var pathGetAllCommand = new Command("get-all", "모든 Line 1/Line 2 경로 읽기");
        pathGetAllCommand.AddOption(jsonOption);
        pathGetAllCommand.SetHandler((json) =>
        {
            using var controller = new Workflow();
            var allPaths = controller.GetAllPaths();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        count = allPaths.Count,
                        paths = allPaths
                    }
                });
            }
            else
            {
                Console.WriteLine("[workflow-path get-all] All Paths:");
                foreach (var linePaths in allPaths)
                {
                    Console.WriteLine($"\n{linePaths.Key}:");
                    foreach (var path in linePaths.Value)
                    {
                        Console.WriteLine($"  {path.Key}: {path.Value}");
                    }
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        workflowPathCommand.AddCommand(pathGetAllCommand);

        // workflow path set-line1: Line 1 특정 경로 설정
        var pathTypeArgument = new Argument<string>("type", "Path type (samplename|movenir|movealldata)");
        var pathValueArgument = new Argument<string>("value", "Path value to set");
        var pathSetLine1Command = new Command("set-line1", "Line 1 특정 경로 설정");
        pathSetLine1Command.AddArgument(pathTypeArgument);
        pathSetLine1Command.AddArgument(pathValueArgument);
        pathSetLine1Command.SetHandler((type, value) =>
        {
            using var controller = new Workflow();
            var result = controller.SetLine1Path(type, value);
            if (result)
            {
                Console.WriteLine($"[workflow-path set-line1] Success: {type} set to '{value}'");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine($"[workflow-path set-line1] Failed: Could not set {type}");
                Environment.Exit(EXIT_ERROR);
            }
        }, pathTypeArgument, pathValueArgument);
        workflowPathCommand.AddCommand(pathSetLine1Command);

        // workflow path set-line2: Line 2 특정 경로 설정
        var pathSetLine2Command = new Command("set-line2", "Line 2 특정 경로 설정");
        pathSetLine2Command.AddArgument(pathTypeArgument);
        pathSetLine2Command.AddArgument(pathValueArgument);
        pathSetLine2Command.SetHandler((type, value) =>
        {
            using var controller = new Workflow();
            var result = controller.SetLine2Path(type, value);
            if (result)
            {
                Console.WriteLine($"[workflow-path set-line2] Success: {type} set to '{value}'");
                Environment.Exit(EXIT_SUCCESS);
            }
            else
            {
                Console.WriteLine($"[workflow-path set-line2] Failed: Could not set {type}");
                Environment.Exit(EXIT_ERROR);
            }
        }, pathTypeArgument, pathValueArgument);
        workflowPathCommand.AddCommand(pathSetLine2Command);

        workflowCommand.AddCommand(workflowPathCommand);

        rootCommand.AddCommand(workflowCommand);

        // logs 명령: LogPanel 데이터 읽기 및 필터링
        var logsCommand = new Command("logs", "LogPanel 데이터 읽기 및 필터링");

        // logs get: 모든 로그 메시지 가져오기
        var logsGetCommand = new Command("get", "모든 로그 메시지 가져오기");
        logsGetCommand.AddOption(jsonOption);
        logsGetCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var logs = reader.GetAllLogMessages();

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        count = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                Console.WriteLine($"[logs-get] Found {logs.Count} log message(s):");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        logsCommand.AddCommand(logsGetCommand);

        // logs tail: 최근 N개 로그 메시지 가져오기
        var countArgument = new Argument<int>("count", "가져올 로그 개수 (기본값: 10)")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var logsTailCommand = new Command("tail", "최근 N개 로그 메시지 가져오기");
        logsTailCommand.AddArgument(countArgument);
        logsTailCommand.AddOption(jsonOption);
        logsTailCommand.SetHandler((count, json) =>
        {
            var actualCount = count > 0 ? count : 10;
            using var reader = new DataReader();
            var logs = reader.GetLatestLogs(actualCount);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        requested = actualCount,
                        returned = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                Console.WriteLine($"[logs-tail] Latest {logs.Count} log message(s):");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, countArgument, jsonOption);
        logsCommand.AddCommand(logsTailCommand);

        // logs filter: 로그 레벨로 필터링
        var levelOption = new Option<string?>(
            ["--level", "-l"],
            () => null,
            "필터링할 로그 레벨 (Debug, Info, Warning, Error)"
        );
        var logsFilterCommand = new Command("filter", "로그 레벨로 필터링");
        logsFilterCommand.AddOption(levelOption);
        logsFilterCommand.AddOption(jsonOption);
        logsFilterCommand.SetHandler((level, json) =>
        {
            using var reader = new DataReader();
            var logs = reader.GetLogsByLevel(level);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        filter = new { level = level },
                        count = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                var levelText = string.IsNullOrWhiteSpace(level) ? "All" : level;
                Console.WriteLine($"[logs-filter] Filtered by level '{levelText}': {logs.Count} message(s)");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, levelOption, jsonOption);
        logsCommand.AddCommand(logsFilterCommand);

        // logs search: 로그 메시지 검색
        var searchTextArgument = new Argument<string>("text", "검색할 텍스트");
        var logsSearchCommand = new Command("search", "로그 메시지 검색");
        logsSearchCommand.AddArgument(searchTextArgument);
        logsSearchCommand.AddOption(jsonOption);
        logsSearchCommand.SetHandler((text, json) =>
        {
            using var reader = new DataReader();
            var logs = reader.SearchLogs(text);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "LogPanel",
                        search = text,
                        count = logs.Count,
                        logs = logs
                    }
                });
            }
            else
            {
                Console.WriteLine($"[logs-search] Searched for '{text}': {logs.Count} message(s) found");
                foreach (var log in logs)
                {
                    var severity = log.GetValueOrDefault("Severity", "");
                    var time = log.GetValueOrDefault("Time", "");
                    var source = log.GetValueOrDefault("Source", "");
                    var message = log.GetValueOrDefault("Message", "");
                    Console.WriteLine($"  [{severity}] {time} | {source} | {message}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, searchTextArgument, jsonOption);
        logsCommand.AddCommand(logsSearchCommand);

        rootCommand.AddCommand(logsCommand);
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
