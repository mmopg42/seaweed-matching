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
        /// Releases resources used by the UIA3 automation.
        /// </summary>
        public void Dispose()
        {
            _automation?.Dispose();
        }
    }
}
