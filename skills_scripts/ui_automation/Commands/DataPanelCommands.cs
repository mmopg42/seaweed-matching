using System.CommandLine;
using System.Text.Json;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using DataReader = SkillsScripts.UiAutomation.ChronoDataPanelReader;

namespace UiAutomation.Commands;

/// <summary>
/// Data panel commands for ChronoView data panel automation (ChronoDataPanelReader).
/// Provides commands to read StatisticsPanel and FileGroupDataGrid data.
/// </summary>
public class DataPanelCommands : ICommandHandler
{
    // Exit code constants matching Program.cs
    private const int EXIT_SUCCESS = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_NOT_FOUND = 2;
    private const int EXIT_INVALID_ARGUMENT = 4;

    /// <summary>
    /// Registers all data panel commands with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    public void RegisterCommands(RootCommand rootCommand)
    {
        // JSON output option
        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output in JSON format for programmatic access"
        );

        // stats 명령: StatisticsPanel 데이터 읽기
        var statsCommand = new Command("stats", "StatisticsPanel 데이터 읽기");
        statsCommand.AddOption(jsonOption);
        statsCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "MainWindow not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    Console.WriteLine("[stats] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var statistics = reader.GetAllStatistics(mainWindow);
            if (statistics == null)
            {
                if (json)
                {
                    PrintJsonOutput(new
                    {
                        success = false,
                        error = "StatisticsPanel not found",
                        errorCode = EXIT_NOT_FOUND
                    });
                }
                else
                {
                    Console.WriteLine("[stats] Failed: Could not extract statistics (StatisticsPanel not found)");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new
                    {
                        source = "StatisticsPanel",
                        statistics = statistics
                    }
                });
            }
            else
            {
                Console.WriteLine("[stats] Statistics from StatisticsPanel:");
                Console.WriteLine("\nFile Counts:");
                foreach (var kvp in statistics.Where(k => k.Key.StartsWith("NIR") || k.Key.StartsWith("Normal") || k.Key.StartsWith("Cam")))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine("\nMatching Status:");
                foreach (var kvp in statistics.Where(k => !k.Key.StartsWith("NIR") && !k.Key.StartsWith("Normal") && !k.Key.StartsWith("Cam") && k.Key != "일반2"))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        rootCommand.AddCommand(statsCommand);

        // datagrid 명령: DataGrid 데이터 읽기
        var datagridCommand = new Command("datagrid", "DataGrid 데이터 읽기");

        // datagrid headers: DataGrid 헤더 읽기
        var dgHeadersCommand = new Command("headers", "DataGrid 헤더 읽기");
        dgHeadersCommand.AddOption(jsonOption);
        dgHeadersCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-headers] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = reader.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-headers] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var headers = reader.GetDataGridHeaders(dataGrid);
            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { columnCount = headers.Count, columns = headers }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-headers] Found {headers.Count} columns:");
                foreach (var header in headers)
                {
                    Console.WriteLine($"  - {header}");
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgHeadersCommand);

        // datagrid rows: 데이터 행 개수 확인
        var dgRowsCommand = new Command("rows", "데이터 행 개수 확인");
        dgRowsCommand.AddOption(jsonOption);
        dgRowsCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-rows] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = reader.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-rows] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var rowCount = reader.GetDataRowCount(dataGrid);
            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { rowCount = rowCount }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-rows] DataGrid has {rowCount} data rows");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgRowsCommand);

        // datagrid data: 모든 데이터 추출
        var dgDataCommand = new Command("data", "모든 DataGrid 데이터 추출");
        dgDataCommand.AddOption(jsonOption);
        dgDataCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-data] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var allData = reader.GetAllData(mainWindow);
            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { rowCount = allData.Count, data = allData }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-data] Extracted {allData.Count} rows");
                if (allData.Count > 0)
                {
                    var headers = allData[0].Keys.ToList();
                    Console.WriteLine("  Headers: " + string.Join(", ", headers));
                    foreach (var row in allData)
                    {
                        Console.WriteLine("  Row: " + string.Join(" | ", row.Values));
                    }
                }
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgDataCommand);

        // datagrid info: Show headers and row count
        var dgInfoCommand = new Command("info", "DataGrid 헤더 및 행 개수 요약");
        dgInfoCommand.AddOption(jsonOption);
        dgInfoCommand.SetHandler((json) =>
        {
            using var reader = new DataReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-info] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = reader.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-info] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var headers = reader.GetDataGridHeaders(dataGrid);
            var rowCount = reader.GetDataRowCount(dataGrid);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = true,
                    data = new { columnCount = headers.Count, rowCount = rowCount, columns = headers }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-info] DataGrid: {headers.Count} columns, {rowCount} rows");
                Console.WriteLine("  Columns: " + string.Join(", ", headers));
            }
            Environment.Exit(EXIT_SUCCESS);
        }, jsonOption);
        datagridCommand.AddCommand(dgInfoCommand);

        // datagrid cell: Get specific cell value
        var rowArgument = new Argument<int>("row", "행 인덱스 (0-based)");
        var colArgument = new Argument<int>("col", "열 인덱스 (0-based)");
        var dgCellCommand = new Command("cell", "특정 셀 값 가져오기");
        dgCellCommand.AddArgument(rowArgument);
        dgCellCommand.AddArgument(colArgument);
        dgCellCommand.AddOption(jsonOption);
        dgCellCommand.SetHandler((row, col, json) =>
        {
            using var reader = new DataReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "MainWindow not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-cell] Failed: MainWindow not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var dataGrid = reader.FindDataGrid(mainWindow);
            if (dataGrid == null)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = "DataGrid not found", errorCode = EXIT_NOT_FOUND });
                }
                else
                {
                    Console.WriteLine("[datagrid-cell] Failed: DataGrid not found");
                }
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var cf = dataGrid.Automation.ConditionFactory;
            var rows = dataGrid.FindAllChildren(cf.ByControlType(ControlType.DataItem));

            if (row < 0 || row >= rows.Length)
            {
                if (json)
                {
                    PrintJsonOutput(new { success = false, error = $"Row index {row} out of range", errorCode = EXIT_INVALID_ARGUMENT });
                }
                else
                {
                    Console.WriteLine($"[datagrid-cell] Failed: Row index {row} out of range (0-{rows.Length - 1})");
                }
                Environment.Exit(EXIT_INVALID_ARGUMENT);
                return;
            }

            var rowElement = rows[row];
            var cellText = reader.GetCellText(rowElement, col);

            if (json)
            {
                PrintJsonOutput(new
                {
                    success = cellText != null,
                    data = new { row = row, column = col, value = cellText }
                });
            }
            else
            {
                Console.WriteLine($"[datagrid-cell] Row {row}, Column {col}: '{cellText ?? "(null)"}'");
            }
            Environment.Exit(EXIT_SUCCESS);
        }, rowArgument, colArgument, jsonOption);
        datagridCommand.AddCommand(dgCellCommand);

        // datagrid export: Export all data as JSON
        var dgExportCommand = new Command("export", "모든 DataGrid 데이터를 JSON으로 내보내기");
        dgExportCommand.SetHandler(() =>
        {
            using var reader = new DataReader();
            var mainWindow = reader.FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[datagrid-export] Failed: MainWindow not found");
                Environment.Exit(EXIT_NOT_FOUND);
                return;
            }

            var allData = reader.GetAllData(mainWindow);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                success = true,
                data = new
                {
                    rowCount = allData.Count,
                    exportedAt = DateTime.UtcNow.ToString("o"),
                    data = allData
                }
            }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.Exit(EXIT_SUCCESS);
        });
        datagridCommand.AddCommand(dgExportCommand);

        rootCommand.AddCommand(datagridCommand);
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
