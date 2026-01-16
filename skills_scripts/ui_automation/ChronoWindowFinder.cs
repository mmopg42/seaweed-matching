using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// </remarks>
        /// <returns>The SetupWindow if found, null otherwise</returns>
        public Window? FindSetupWindow()
        {
            return FindWindowByTitle("Setup", substring: true);
        }

        /// <summary>
        /// Finds the ChronoView SettingsDialog.
        /// </summary>
        /// <remarks>
        /// The title comes from localization resource {x:Static res:Strings.Dialog_Settings}.
        /// Uses substring search for "Settings" (English) or "설정" (Korean) fallback.
        /// The dialog is shown via SettingsDialog.xaml with WindowStartupLocation="CenterOwner".
        /// </remarks>
        /// <returns>The SettingsDialog if found, null otherwise</returns>
        public Window? FindSettingsDialog()
        {
            // Try English first
            var window = FindWindowByTitle("Settings", substring: true);
            if (window != null)
            {
                return window;
            }

            // Fallback to Korean title
            return FindWindowByTitle("설정", substring: true);
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
    }
}
