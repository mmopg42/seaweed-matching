using System;
using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// FlaUI-based UI automation helper for window finding and element inspection.
    /// Designed for ChronoView automation testing.
    /// </summary>
    public class UiAutomation : IDisposable
    {
        private readonly UIA3Automation _automation;

        /// <summary>
        /// Initializes a new instance of the UiAutomation class.
        /// </summary>
        public UiAutomation()
        {
            _automation = new UIA3Automation();
        }

        /// <summary>
        /// Finds the main window of a process by its name.
        /// </summary>
        /// <param name="processName">Name of the process (e.g., "ChronoView")</param>
        /// <returns>The Window element if found, null otherwise</returns>
        public Window? FindWindowByProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                Console.WriteLine($"[UiAutomation] Process name is null or empty");
                return null;
            }

            try
            {
                // Get processes by name
                Process[] processes = Process.GetProcessesByName(processName);

                if (processes.Length == 0)
                {
                    Console.WriteLine($"[UiAutomation] No process found with name: {processName}");
                    return null;
                }

                // Use the first process found
                int processId = processes[0].Id;
                Console.WriteLine($"[UiAutomation] Found process '{processName}' with ID: {processId}");

                // Find the window using UIA3 with lambda expression
                var cf = _automation.ConditionFactory;
                var windowCondition = cf.ByControlType(ControlType.Window).And(cf.ByProcessId(processId));

                var window = _automation.GetDesktop().FindFirstDescendant(windowCondition)?.AsWindow();

                if (window == null)
                {
                    Console.WriteLine($"[UiAutomation] No window found for process '{processName}' (ID: {processId})");
                    return null;
                }

                Console.WriteLine($"[UiAutomation] Found window: '{window.Name}' (Handle: {window.NativeWindowHandle})");
                return window;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding window by process '{processName}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a window by its title text.
        /// </summary>
        /// <param name="title">The exact or partial window title</param>
        /// <param name="substring">If true, matches windows containing the title; if false, requires exact match</param>
        /// <returns>The Window element if found, null otherwise</returns>
        public Window? FindWindowByTitle(string title, bool substring = false)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine($"[UiAutomation] Title is null or empty");
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
                            Console.WriteLine($"[UiAutomation] Found window by substring '{title}': '{window.Name}'");
                            return window.AsWindow();
                        }
                    }

                    Console.WriteLine($"[UiAutomation] No window found containing title: {title}");
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
                        Console.WriteLine($"[UiAutomation] No window found with exact title: {title}");
                        return null;
                    }

                    Console.WriteLine($"[UiAutomation] Found window by exact title '{title}': '{window.Name}'");
                    return window;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UiAutomation] Error finding window by title '{title}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Lists all child elements of an automation element in a hierarchical format.
        /// Useful for inspecting UI structure during development.
        /// </summary>
        /// <param name="parent">The parent automation element</param>
        /// <param name="maxDepth">Maximum depth to traverse (default: 3)</param>
        /// <param name="currentDepth">Current depth in recursion (internal use)</param>
        /// <param name="indent">Indentation string for current level (internal use)</param>
        public void ListElements(AutomationElement? parent, int maxDepth = 3, int currentDepth = 0, string indent = "")
        {
            if (parent == null)
            {
                Console.WriteLine("[UiAutomation] Parent element is null");
                return;
            }

            // Base element info
            if (currentDepth == 0)
            {
                Console.WriteLine($"[UiAutomation] UI Tree for '{parent.Name ?? "(unnamed)"}' (max depth: {maxDepth}):");
                PrintElementInfo(parent, indent);
            }

            // Check max depth
            if (currentDepth >= maxDepth)
            {
                return;
            }

            // Find children
            AutomationElement[] children;
            try
            {
                children = parent.FindAllChildren();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{indent}  [Error reading children: {ex.Message}]");
                return;
            }

            if (children.Length == 0)
            {
                Console.WriteLine($"{indent}  (No children)");
                return;
            }

            // Print each child
            foreach (var child in children)
            {
                PrintElementInfo(child, indent + "  ");

                // Recursively traverse child elements
                ListElements(child, maxDepth, currentDepth + 1, indent + "  ");
            }
        }

        /// <summary>
        /// Prints information about an automation element.
        /// </summary>
        private void PrintElementInfo(AutomationElement element, string indent)
        {
            var controlType = element.ControlType.ToString();
            var name = element.Name ?? "(unnamed)";
            var automationId = element.AutomationId ?? "";
            var className = element.ClassName ?? "";

            var info = $"{indent}[{controlType}]";
            if (!string.IsNullOrEmpty(name))
            {
                info += $" Name: '{name}'";
            }
            if (!string.IsNullOrEmpty(automationId))
            {
                info += $" AutomationId: '{automationId}'";
            }
            if (!string.IsNullOrEmpty(className))
            {
                info += $" Class: '{className}'";
            }

            Console.WriteLine(info);
        }

        /// <summary>
        /// Releases resources used by the UIA3 automation.
        /// </summary>
        public void Dispose()
        {
            _automation?.Dispose();
        }
    }
}
