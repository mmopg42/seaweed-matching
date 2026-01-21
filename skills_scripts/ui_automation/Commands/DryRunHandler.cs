using System.CommandLine;
using System.CommandLine.Invocation;
using static UiAutomation.Commands.JsonResponseHelper;
using static UiAutomation.Commands.ExitCodes;

namespace UiAutomation.Commands;

/// <summary>
/// Base helper for handling dry-run mode in command handlers.
/// Provides consistent dry-run behavior across all commands.
/// </summary>
public static class DryRunHandler
{
    /// <summary>
    /// Checks if dry-run mode is active and handles accordingly.
    /// Returns true if caller should skip real execution.
    /// </summary>
    /// <param name="context">Invocation context for accessing parsed options</param>
    /// <param name="skillName">Skill name to validate</param>
    /// <param name="cliTemplate">CLI command template (e.g., "app launch")</param>
    /// <param name="args">Optional arguments dictionary</param>
    /// <returns>True if in dry-run mode (caller should skip execution), false otherwise</returns>
    public static bool CheckDryRun(
        InvocationContext context,
        string skillName,
        string cliTemplate,
        Dictionary<string, object>? args = null)
    {
        // Access dry-run option from global options
        // The option is registered globally, so we can get it from root command
        var dryRunOption = context.ParseResult.RootCommandResult.Command
            .Options.OfType<Option<bool>>()
            .FirstOrDefault(o => o.HasAlias("--dry-run"));

        if (dryRunOption == null || !context.ParseResult.GetValueForOption(dryRunOption))
        {
            return false; // Not in dry-run mode, proceed with execution
        }

        // Dry-run mode: validate and return command info
        args ??= new Dictionary<string, object>();

        // Validate skill name
        var validation = DryRunValidator.ValidateSkill(skillName);
        if (!validation.IsValid)
        {
            // Skill validation failed - print error
            PrintError(
                validation.ErrorMessage ?? "Unknown skill",
                validation.ErrorCode ?? INVALID_ARGUMENT,
                validation.Suggestion
            );
            context.ExitCode = validation.ErrorCode ?? INVALID_ARGUMENT;
            return true; // Skip execution
        }

        // Build full CLI command
        var cliCommand = args.Count > 0
            ? $"{cliTemplate} {FormatArgs(args)}"
            : cliTemplate;

        // Print dry-run response
        PrintDryRun(skillName, cliCommand, args);

        context.ExitCode = SUCCESS;
        return true; // Skip real execution
    }

    /// <summary>
    /// Formats arguments dictionary for CLI display.
    /// </summary>
    private static string FormatArgs(Dictionary<string, object> args)
    {
        var parts = new List<string>();
        foreach (var (key, value) in args)
        {
            if (value is bool b && b)
            {
                parts.Add($"--{key}");
            }
            else if (value is string s)
            {
                parts.Add($"--{key} \"{s}\"");
            }
            else if (value is int i)
            {
                parts.Add($"--{key} {i}");
            }
            else if (value is IEnumerable<int> ints)
            {
                parts.Add($"--{key} {string.Join(",", ints)}");
            }
            else
            {
                parts.Add($"--{key} {value}");
            }
        }
        return string.Join(" ", parts);
    }

    /// <summary>
    /// Skill name mapping from CLI commands to skill names.
    /// Maps command paths to their corresponding skill names from test-executor-skills.md.
    /// </summary>
    private static readonly Dictionary<string, string> SkillMapping = new()
    {
        // APP
        ["app launch"] = "APP_LAUNCH",
        ["app stop"] = "APP_STOP",
        ["app restart"] = "APP_RESTART",
        ["app status"] = "APP_STATUS",
        // BATCH
        ["batch select-and-move"] = "BATCH_SELECT_AND_MOVE",
        ["batch select-and-delete"] = "BATCH_SELECT_AND_DELETE",
        ["batch export-all"] = "BATCH_EXPORT_ALL",
        // CONSOLE_LOGS
        ["console-logs list"] = "CONSOLE_LOGS_LIST",
        ["console-logs tail"] = "CONSOLE_LOGS_TAIL",
        ["console-logs search"] = "CONSOLE_LOGS_SEARCH",
        // DATA_PANEL
        ["stats"] = "DATA_PANEL_STATS",
        ["datagrid headers"] = "DATA_PANEL_HEADERS",
        ["datagrid rows"] = "DATA_PANEL_ROWS",
        ["datagrid data"] = "DATA_PANEL_DATA",
        ["datagrid info"] = "DATA_PANEL_INFO",
        ["datagrid cell"] = "DATA_PANEL_CELL",
        ["datagrid export"] = "DATA_PANEL_EXPORT",
        // FILE_OPS
        ["file-ops select row-index"] = "FILE_OPS_SELECT_ROW_INDEX",
        ["file-ops select group-id"] = "FILE_OPS_SELECT_GROUP_ID",
        ["file-ops select prefix"] = "FILE_OPS_SELECT_PREFIX",
        ["file-ops select-all"] = "FILE_OPS_SELECT_ALL",
        ["file-ops clear-selection"] = "FILE_OPS_CLEAR_SELECTION",
        ["file-ops selected"] = "FILE_OPS_SELECTED",
        ["file-ops move rows"] = "FILE_OPS_MOVE_ROWS",
        ["file-ops move group-ids"] = "FILE_OPS_MOVE_GROUP_IDS",
        ["file-ops move prefix"] = "FILE_OPS_MOVE_PREFIX",
        ["file-ops delete rows"] = "FILE_OPS_DELETE_ROWS",
        ["file-ops delete group-ids"] = "FILE_OPS_DELETE_GROUP_IDS",
        ["file-ops wait move"] = "FILE_OPS_WAIT_MOVE",
        ["file-ops wait delete"] = "FILE_OPS_WAIT_DELETE",
        ["file-ops confirm"] = "FILE_OPS_CONFIRM",
        ["file-ops verify deleted"] = "FILE_OPS_VERIFY_DELETED",
        ["file-ops verify row-count"] = "FILE_OPS_VERIFY_ROW_COUNT",
        // LOGS
        ["logs get"] = "LOGS_GET",
        ["logs tail"] = "LOGS_TAIL",
        ["logs filter"] = "LOGS_FILTER",
        ["logs search"] = "LOGS_SEARCH",
        // SETTINGS_DIALOG
        ["settings-dialog open"] = "SETTINGS_DIALOG_OPEN",
        ["settings-dialog close"] = "SETTINGS_DIALOG_CLOSE",
        ["settings-dialog inspect"] = "SETTINGS_DIALOG_INSPECT",
        ["settings-dialog status"] = "SETTINGS_DIALOG_STATUS",
        ["settings-dialog path get-all"] = "SETTINGS_DIALOG_PATH_GET_ALL",
        ["settings-dialog path get-line1"] = "SETTINGS_DIALOG_PATH_GET_LINE1",
        ["settings-dialog path get-line2"] = "SETTINGS_DIALOG_PATH_GET_LINE2",
        ["settings-dialog path get-output"] = "SETTINGS_DIALOG_PATH_GET_OUTPUT",
        ["settings-dialog path get-quarantine"] = "SETTINGS_DIALOG_PATH_GET_QUARANTINE",
        ["settings-dialog path set"] = "SETTINGS_DIALOG_PATH_SET",
        ["settings-dialog checkbox get"] = "SETTINGS_DIALOG_CHECKBOX_GET",
        ["settings-dialog checkbox set"] = "SETTINGS_DIALOG_CHECKBOX_SET",
        ["settings-dialog checkbox list"] = "SETTINGS_DIALOG_CHECKBOX_LIST",
        ["settings-dialog action save"] = "SETTINGS_DIALOG_ACTION_SAVE",
        ["settings-dialog action apply"] = "SETTINGS_DIALOG_ACTION_APPLY",
        ["settings-dialog action cancel"] = "SETTINGS_DIALOG_ACTION_CANCEL",
        ["settings-dialog action reset"] = "SETTINGS_DIALOG_ACTION_RESET",
        // SETUP
        ["setup verify-config"] = "SETUP_VERIFY_CONFIG",
        ["setup complete-full"] = "SETUP_COMPLETE_FULL",
        ["setup open-settings"] = "SETUP_OPEN_SETTINGS",
        ["setup camera-states"] = "SETUP_CAMERA_STATES",
        // TEST
        ["test connectivity"] = "TEST_CONNECTIVITY",
        ["test capabilities"] = "TEST_CAPABILITIES",
        ["test datagrid"] = "TEST_DATAGRID",
        // TOOLBAR
        ["toolbar start"] = "TOOLBAR_START",
        ["toolbar stop"] = "TOOLBAR_STOP",
        ["toolbar settings"] = "TOOLBAR_SETTINGS",
        ["toolbar refresh"] = "TOOLBAR_REFRESH",
        ["toolbar move"] = "TOOLBAR_MOVE",
        ["toolbar delete"] = "TOOLBAR_DELETE",
        ["toolbar list"] = "TOOLBAR_LIST",
        ["toolbar click"] = "TOOLBAR_CLICK",
        ["toolbar enabled"] = "TOOLBAR_ENABLED",
        // UTILITY
        ["inspect workflow"] = "UTILITY_INSPECT_WORKFLOW",
        ["inspect log"] = "UTILITY_INSPECT_LOG",
        ["config path"] = "UTILITY_CONFIG_PATH",
        ["config read"] = "UTILITY_CONFIG_READ",
        ["config get"] = "UTILITY_CONFIG_GET",
        // WINDOWS
        ["windows main"] = "WINDOWS_MAIN",
        ["windows setup"] = "WINDOWS_SETUP",
        ["windows setup-complete"] = "WINDOWS_SETUP_COMPLETE",
        ["windows settings"] = "WINDOWS_SETTINGS",
        ["windows preview"] = "WINDOWS_PREVIEW",
        ["windows all"] = "WINDOWS_ALL",
        // WORKFLOW
        ["workflow launch-general"] = "WORKFLOW_LAUNCH_GENERAL",
        ["workflow launch-nir"] = "WORKFLOW_LAUNCH_NIR",
        ["workflow launch-nir2"] = "WORKFLOW_LAUNCH_NIR2",
        ["workflow toggle-filtering"] = "WORKFLOW_TOGGLE_FILTERING",
        ["workflow camera-states"] = "WORKFLOW_CAMERA_STATES",
        ["workflow path get-line1"] = "WORKFLOW_PATH_GET_LINE1",
        ["workflow path get-line2"] = "WORKFLOW_PATH_GET_LINE2",
        ["workflow path get-all"] = "WORKFLOW_PATH_GET_ALL",
        ["workflow path set-line1"] = "WORKFLOW_PATH_SET_LINE1",
        ["workflow path set-line2"] = "WORKFLOW_PATH_SET_LINE2",
        ["workflow select-tab"] = "WORKFLOW_SELECT_TAB",
    };

    /// <summary>
    /// Gets the skill name for a CLI command.
    /// </summary>
    public static string? GetSkillName(string cliCommand) =>
        SkillMapping.TryGetValue(cliCommand, out var skill) ? skill : null;
}
