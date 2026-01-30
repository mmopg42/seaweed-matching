using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for finding ChronoView windows.
    /// Consolidates all window detection logic into a dedicated, testable class.
    /// </summary>
    public class ChronoWindowFinder
    {
        private readonly UIA3Automation _automation;
        private const int DefaultPollIntervalMs = 200;

        /// <summary>
        /// Initializes a new instance of the ChronoWindowFinder class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for window finding</param>
        public ChronoWindowFinder(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        }

        /// <summary>
        /// Finds the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The MainWindow title is "ChronoView Pro - Desktop Application" (MainWindow.xaml line 14).
        /// Uses substring search for "ChronoView Pro" to ensure reliable detection.
        /// </remarks>
        /// <returns>The MainWindow if found, null otherwise</returns>
        public Window? FindMainWindow()
        {
            return FindWindowByTitle("ChronoView Pro", substring: true);
        }

        /// <summary>
        /// Finds the ChronoView SetupWindow.
        /// </summary>
        /// <remarks>
        /// The SetupWindow has Title="Setup - ChronoView Pro" (SetupWindow.xaml line 4).
        /// It uses WindowStyle="None" and AllowsTransparency="True" which may affect detection.
        /// Uses substring search for "Setup" to ensure reliability.
        /// Excludes VS Code windows (containing "Visual Studio Code", "Code -").
        /// Validates the window is from ChronoView process and has AutomationId "SetupWindow".
        /// </remarks>
        /// <returns>The SetupWindow if found, null otherwise</returns>
        public Window? FindSetupWindow()
        {
            return FindSetupWindowImpl();
        }

        /// <summary>
        /// Implementation of SetupWindow finding with enhanced filtering.
        /// </summary>
        private Window? FindSetupWindowImpl()
        {
            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();

                // Find all windows and filter
                var windowCondition = cf.ByControlType(ControlType.Window);
                var windows = desktop.FindAllChildren(windowCondition);

                Window? bestMatch = null;
                int bestMatchScore = 0;

                foreach (var window in windows)
                {
                    string windowName = window.Name ?? string.Empty;

                    // Skip if no name
                    if (string.IsNullOrEmpty(windowName))
                    {
                        continue;
                    }

                    // Check if title contains "Setup"
                    bool hasSetupTitle = windowName.IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!hasSetupTitle)
                    {
                        continue;
                    }

                    // Calculate match score based on positive and negative indicators
                    int score = 0;

                    // Positive indicators
                    if (windowName.IndexOf("ChronoView", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score += 10; // Strong positive: contains "ChronoView"
                    }
                    if (windowName.IndexOf("Pro", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        score += 5; // Positive: contains "Pro"
                    }

                    // Negative indicators (exclude these windows)
                    if (windowName.IndexOf("Visual Studio Code", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.IndexOf("Code -", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.StartsWith("Code ", StringComparison.OrdinalIgnoreCase) ||
                        windowName.IndexOf(".xaml", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.IndexOf(".cs", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoWindowFinder] Skipping non-ChronoView window: '{windowName}'");
                        continue;
                    }

                    // Check process name (strongest validation)
                    string? processName = GetProcessName(window);
                    if (processName != null)
                    {
                        if (processName.Equals("ChronoView", StringComparison.OrdinalIgnoreCase))
                        {
                            score += 20; // Strongest positive: from ChronoView process
                            Console.WriteLine($"[ChronoWindowFinder] Found ChronoView process window: '{windowName}'");
                        }
                        else if (processName.IndexOf("Code", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 processName.IndexOf("chrome", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine($"[ChronoWindowFinder] Skipping window from process '{processName}': '{windowName}'");
                            continue;
                        }
                    }

                    // Check AutomationId (final validation)
                    string? automationId = window.AutomationId;
                    if (automationId == "SetupWindow")
                    {
                        score += 30; // Absolute match: correct AutomationId
                        Console.WriteLine($"[ChronoWindowFinder] Found window with AutomationId 'SetupWindow': '{windowName}'");
                    }

                    // Update best match
                    if (score > bestMatchScore)
                    {
                        bestMatch = window.AsWindow();
                        bestMatchScore = score;
                    }
                }

                if (bestMatch != null)
                {
                    Console.WriteLine($"[ChronoWindowFinder] Found SetupWindow (score: {bestMatchScore}): '{bestMatch.Name}'");
                    return bestMatch;
                }

                Console.WriteLine($"[ChronoWindowFinder] No valid SetupWindow found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error finding SetupWindow: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the process name for a window element.
        /// </summary>
        private string? GetProcessName(AutomationElement element)
        {
            try
            {
                if (element.Properties.ProcessId.IsSupported)
                {
                    int processId = element.Properties.ProcessId.ValueOrDefault;
                    if (processId > 0)
                    {
                        var process = Process.GetProcessById(processId);
                        return process.ProcessName;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error getting process name: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Finds the ChronoView SettingsDialog.
        /// </summary>
        /// <remarks>
        /// The title comes from localization resource {x:Static res:Strings.Dialog_Settings}.
        /// Uses substring search for "Settings" (English) or "설정" (Korean) fallback.
        /// The dialog is shown via SettingsDialog.xaml with WindowStartupLocation="CenterOwner".
        /// Excludes VS Code windows (containing "Visual Studio Code").
        /// </remarks>
        /// <returns>The SettingsDialog if found, null otherwise</returns>
        public Window? FindSettingsDialog()
        {
            // Try English first (excluding VS Code)
            var window = FindWindowByTitleExcluding("Settings", exclude: "Visual Studio Code", substring: true);
            if (window != null)
            {
                return window;
            }

            // Fallback to Korean title
            return FindWindowByTitle("설정", substring: true);
        }

        /// <summary>
        /// Finds a window by its title text, excluding windows containing certain text.
        /// </summary>
        /// <param name="title">The exact or partial window title</param>
        /// <param name="exclude">Text to exclude from matches</param>
        /// <param name="substring">If true, matches windows containing the title; if false, requires exact match</param>
        /// <returns>The Window element if found, null otherwise</returns>
        private Window? FindWindowByTitleExcluding(string title, string exclude, bool substring = false)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine($"[ChronoWindowFinder] Title is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();

                if (substring)
                {
                    // Find all windows and filter by substring
                    var windowCondition = cf.ByControlType(ControlType.Window);
                    var windows = desktop.FindAllChildren(windowCondition);

                    foreach (var window in windows)
                    {
                        if (!string.IsNullOrEmpty(window.Name) &&
                            window.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Exclude windows containing the exclude text
                            if (!string.IsNullOrEmpty(exclude) &&
                                window.Name.IndexOf(exclude, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Console.WriteLine($"[ChronoWindowFinder] Skipping window containing '{exclude}': '{window.Name}'");
                                continue;
                            }

                            Console.WriteLine($"[ChronoWindowFinder] Found window by substring '{title}': '{window.Name}'");
                            return window.AsWindow();
                        }
                    }

                    Console.WriteLine($"[ChronoWindowFinder] No window found containing title: {title} (excluding: {exclude})");
                    return null;
                }
                else
                {
                    // Exact match with case insensitivity
                    var windowCondition = cf.ByControlType(ControlType.Window)
                        .And(cf.ByName(title, PropertyConditionFlags.IgnoreCase));

                    var window = desktop.FindFirstDescendant(windowCondition)?.AsWindow();

                    if (window == null)
                    {
                        Console.WriteLine($"[ChronoWindowFinder] No window found with exact title: {title}");
                        return null;
                    }

                    Console.WriteLine($"[ChronoWindowFinder] Found window by exact title '{title}': '{window.Name}'");
                    return window;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error finding window by title '{title}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds the ChronoView ImagePreviewWindow.
        /// </summary>
        /// <remarks>
        /// The ImagePreviewWindow has Title="Image Preview" (ImagePreviewWindow.xaml line 4).
        /// It uses WindowStyle="None" which may affect detection.
        /// Uses substring search for "Image Preview" to ensure reliability.
        /// </remarks>
        /// <returns>The ImagePreviewWindow if found, null otherwise</returns>
        public Window? FindImagePreviewWindow()
        {
            return FindWindowByTitle("Image Preview", substring: true);
        }

        /// <summary>
        /// Finds all ChronoView windows (main window, dialogs, and preview windows).
        /// </summary>
        /// <remarks>
        /// Searches for windows containing "ChronoView" in their title.
        /// Returns all matching windows as a List<Window> and logs their count and titles.
        /// </remarks>
        /// <returns>List of all ChronoView windows found</returns>
        public List<Window> FindAllChronoViewWindows()
        {
            var result = new List<Window>();

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();
                var windowCondition = cf.ByControlType(ControlType.Window);
                var windows = desktop.FindAllChildren(windowCondition);

                foreach (var window in windows)
                {
                    if (!string.IsNullOrEmpty(window.Name) &&
                        window.Name.IndexOf("ChronoView", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(window.AsWindow());
                    }
                }

                Console.WriteLine($"[ChronoWindowFinder] Found {result.Count} ChronoView window(s):");
                foreach (var window in result)
                {
                    Console.WriteLine($"  - '{window.Name}'");
                }

                if (result.Count == 0)
                {
                    Console.WriteLine("[ChronoWindowFinder] No ChronoView windows found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error finding ChronoView windows: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Checks if a window with the given title substring exists.
        /// </summary>
        /// <param name="titleSubstring">The substring to search for in window titles</param>
        /// <returns>True if a matching window is found, false otherwise</returns>
        public bool IsWindowOpen(string titleSubstring)
        {
            if (string.IsNullOrWhiteSpace(titleSubstring))
            {
                Console.WriteLine($"[ChronoWindowFinder] Title substring is null or empty");
                return false;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();
                var windowCondition = cf.ByControlType(ControlType.Window);
                var windows = desktop.FindAllChildren(windowCondition);

                foreach (var window in windows)
                {
                    if (!string.IsNullOrEmpty(window.Name) &&
                        window.Name.IndexOf(titleSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoWindowFinder] Window is open: '{window.Name}'");
                        return true;
                    }
                }

                Console.WriteLine($"[ChronoWindowFinder] No window found containing: {titleSubstring}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error checking if window is open: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Finds a window by its title text.
        /// </summary>
        /// <param name="title">The exact or partial window title</param>
        /// <param name="substring">If true, matches windows containing the title; if false, requires exact match</param>
        /// <returns>The Window element if found, null otherwise</returns>
        private Window? FindWindowByTitle(string title, bool substring = false)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine($"[ChronoWindowFinder] Title is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();

                if (substring)
                {
                    // Find all windows and filter by substring
                    var windowCondition = cf.ByControlType(ControlType.Window);
                    var windows = desktop.FindAllChildren(windowCondition);

                    foreach (var window in windows)
                    {
                        if (!string.IsNullOrEmpty(window.Name) &&
                            window.Name.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine($"[ChronoWindowFinder] Found window by substring '{title}': '{window.Name}'");
                            return window.AsWindow();
                        }
                    }

                    Console.WriteLine($"[ChronoWindowFinder] No window found containing title: {title}");
                    return null;
                }
                else
                {
                    // Exact match with case insensitivity
                    var windowCondition = cf.ByControlType(ControlType.Window)
                        .And(cf.ByName(title, PropertyConditionFlags.IgnoreCase));

                    var window = desktop.FindFirstDescendant(windowCondition)?.AsWindow();

                    if (window == null)
                    {
                        Console.WriteLine($"[ChronoWindowFinder] No window found with exact title: {title}");
                        return null;
                    }

                    Console.WriteLine($"[ChronoWindowFinder] Found window by exact title '{title}': '{window.Name}'");
                    return window;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error finding window by title '{title}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Waits for a window with the given title substring to appear.
        /// </summary>
        /// <remarks>
        /// Excludes VS Code windows (containing "Visual Studio Code") from matching.
        /// </remarks>
        /// <param name="titleSubstring">The substring to search for in window titles</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 5000)</param>
        /// <returns>The window if found, null if timeout</returns>
        public Window? WaitForWindow(string titleSubstring, int timeoutMs = 5000)
        {
            if (string.IsNullOrWhiteSpace(titleSubstring))
            {
                Console.WriteLine($"[ChronoWindowFinder] Title substring is null or empty");
                return null;
            }

            var startTime = Stopwatch.StartNew();
            Console.WriteLine($"[ChronoWindowFinder] Waiting for window containing '{titleSubstring}' (timeout: {timeoutMs}ms)");

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();

                while (startTime.ElapsedMilliseconds < timeoutMs)
                {
                    var windowCondition = cf.ByControlType(ControlType.Window);
                    var windows = desktop.FindAllChildren(windowCondition);

                    foreach (var window in windows)
                    {
                        if (!string.IsNullOrEmpty(window.Name) &&
                            window.Name.IndexOf(titleSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // Exclude VS Code windows
                            if (window.Name.IndexOf("Visual Studio Code", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                continue;  // Skip VS Code windows
                            }

                            Console.WriteLine($"[ChronoWindowFinder] Window found after {startTime.ElapsedMilliseconds}ms: '{window.Name}'");
                            return window.AsWindow();
                        }
                    }

                    Thread.Sleep(DefaultPollIntervalMs);
                }

                Console.WriteLine($"[ChronoWindowFinder] Timeout waiting for window containing '{titleSubstring}'");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error waiting for window: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Waits for a window with the given title substring to close.
        /// </summary>
        /// <param name="titleSubstring">The substring to search for in window titles</param>
        /// <param name="timeoutMs">Maximum time to wait in milliseconds (default: 5000)</param>
        /// <returns>True if the window closed, false if timeout</returns>
        public bool WaitForWindowToClose(string titleSubstring, int timeoutMs = 5000)
        {
            if (string.IsNullOrWhiteSpace(titleSubstring))
            {
                Console.WriteLine($"[ChronoWindowFinder] Title substring is null or empty");
                return false;
            }

            var startTime = Stopwatch.StartNew();
            Console.WriteLine($"[ChronoWindowFinder] Waiting for window containing '{titleSubstring}' to close (timeout: {timeoutMs}ms)");

            try
            {
                var cf = _automation.ConditionFactory;
                var desktop = _automation.GetDesktop();

                // First check if window exists
                bool windowExists = false;
                var windowCondition = cf.ByControlType(ControlType.Window);
                var windows = desktop.FindAllChildren(windowCondition);

                foreach (var window in windows)
                {
                    if (!string.IsNullOrEmpty(window.Name) &&
                        window.Name.IndexOf(titleSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        windowExists = true;
                        break;
                    }
                }

                if (!windowExists)
                {
                    Console.WriteLine($"[ChronoWindowFinder] Window containing '{titleSubstring}' not found (already closed?)");
                    return true;
                }

                // Window exists, wait for it to close
                while (startTime.ElapsedMilliseconds < timeoutMs)
                {
                    windows = desktop.FindAllChildren(windowCondition);
                    bool found = false;

                    foreach (var window in windows)
                    {
                        if (!string.IsNullOrEmpty(window.Name) &&
                            window.Name.IndexOf(titleSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Console.WriteLine($"[ChronoWindowFinder] Window closed after {startTime.ElapsedMilliseconds}ms");
                        return true;
                    }

                    Thread.Sleep(DefaultPollIntervalMs);
                }

                Console.WriteLine($"[ChronoWindowFinder] Timeout waiting for window containing '{titleSubstring}' to close");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error waiting for window to close: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the ChronoView MainWindow is ready for interaction.
        /// </summary>
        /// <remarks>
        /// A window is considered ready if:
        /// - It exists (can be found)
        /// - IsEnabled is true (not during startup/shutdown)
        /// - Properties are accessible (has loaded)
        /// </remarks>
        /// <returns>True if the MainWindow is ready, false otherwise</returns>
        public bool IsMainWindowReady()
        {
            try
            {
                var window = FindMainWindow();
                if (window == null)
                {
                    Console.WriteLine($"[ChronoWindowFinder] MainWindow not found");
                    return false;
                }

                // Check IsEnabled
                if (window.Properties.IsEnabled.IsSupported)
                {
                    var isEnabled = window.Properties.IsEnabled.ValueOrDefault;
                    if (!isEnabled)
                    {
                        Console.WriteLine($"[ChronoWindowFinder] MainWindow exists but IsEnabled=false (likely during startup/shutdown)");
                        return false;
                    }
                }

                // Try accessing other properties to verify loaded state
                bool isOffscreen = false;
                if (window.Properties.IsOffscreen.IsSupported)
                {
                    isOffscreen = window.Properties.IsOffscreen.ValueOrDefault;
                }

                // If we got here without exceptions, the window is ready
                Console.WriteLine($"[ChronoWindowFinder] MainWindow is ready (IsEnabled=true, IsOffscreen={isOffscreen})");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWindowFinder] Error checking MainWindow readiness: {ex.Message}");
                return false;
            }
        }
    }
}
