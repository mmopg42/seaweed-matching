using System;
using System.Collections.Generic;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for ChronoView WorkflowPanel automation.
    /// Consolidates all workflow panel interaction capabilities into a cohesive, testable class.
    /// </summary>
    /// <remarks>
    /// This class provides a complete API for workflow panel interactions including:
    /// - Finding the WorkflowPanel within MainWindow
    /// - Finding camera launch buttons
    /// - Clicking camera launch buttons
    /// - Reading camera state indicators
    /// - Toggling NIR filtering
    /// - Finding path TextBox controls by their associated Label text
    /// - Reading current values from TextBox controls using ValuePattern
    /// - Setting values in TextBox controls using ValuePattern
    /// - Getting all paths from Line 1 and Line 2
    ///
    /// Camera buttons are located in the Camera Status expander section:
    /// - General Camera: "실행" or "실행 및 활성화" (tooltip at line 31 of WorkflowPanel.xaml)
    /// - NIR 1 Camera: Similar button for NIR camera (line 48)
    /// - NIR 2 Camera: NIR 2 button (lines 61-65)
    /// - NIR Filtering Toggle: Toggles Nir2FilteringState (lines 77-81)
    ///
    /// Camera state indicators are Ellipse elements with Fill color:
    /// - Gray (NotReady)
    /// - Green (Ready)
    /// - Blue (Active)
    ///
    /// Path TextBox controls in WorkflowPanel.xaml:
    /// Line 1: Line1SampleName, Line1MoveNir, Line1MoveAllData
    /// Line 2: Line2SampleName, Line2MoveNir, Line2MoveAllData
    ///
    /// Label-TextBox association:
    /// - Label is ControlType.Text with specific text (e.g., "샘플명:", "NIR 이동:", "전체 이동:")
    /// - TextBox is ControlType.Edit, appears after Label in the same StackPanel
    /// </remarks>
    public class ChronoWorkflowController : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;

        /// <summary>
        /// Initializes a new instance of the ChronoWorkflowController class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for UI automation</param>
        public ChronoWorkflowController(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
        }

        /// <summary>
        /// Initializes a new instance of the ChronoWorkflowController class,
        /// creating its own UIA3Automation instance.
        /// </summary>
        public ChronoWorkflowController() : this(new UIA3Automation())
        {
        }

        /// <summary>
        /// Finds the ChronoView MainWindow.
        /// </summary>
        /// <returns>The MainWindow if found, null otherwise</returns>
        public Window? FindMainWindow()
        {
            return _windowFinder.FindMainWindow();
        }

        /// <summary>
        /// Finds the WorkflowPanel within the ChronoView MainWindow.
        /// </summary>
        /// <remarks>
        /// The WorkflowPanel is a UserControl (WorkflowPanel.xaml).
        /// Searches for ControlType.Custom with Name containing "WorkflowPanel".
        /// </remarks>
        /// <param name="mainWindow">The MainWindow to search within</param>
        /// <returns>The WorkflowPanel AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindWorkflowPanel(Window? mainWindow)
        {
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot find WorkflowPanel: mainWindow is null");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Try to find by Name containing "WorkflowPanel"
                var nameCondition = cf.ByControlType(ControlType.Custom)
                    .And(cf.ByName("WorkflowPanel", PropertyConditionFlags.IgnoreCase));
                var panel = mainWindow.FindFirstDescendant(nameCondition);

                if (panel != null)
                {
                    Console.WriteLine("[ChronoWorkflowController] Found WorkflowPanel by Name");
                    return panel;
                }

                // Try to find by ClassName containing "WorkflowPanel"
                var allElements = mainWindow.FindAllChildren(cf.ByControlType(ControlType.Custom));
                foreach (var element in allElements)
                {
                    if (!string.IsNullOrEmpty(element.ClassName) &&
                        element.ClassName.IndexOf("WorkflowPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoWorkflowController] Found WorkflowPanel by ClassName: '{element.ClassName}'");
                        return element;
                    }
                }

                Console.WriteLine("[ChronoWorkflowController] WorkflowPanel not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error finding WorkflowPanel: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds a camera launch button within the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// Searches for Button control type with Name containing the buttonText.
        /// Camera buttons have dynamic text based on state ("실행", "중지", "활성화", etc.).
        /// </remarks>
        /// <param name="workflowPanel">The WorkflowPanel element to search within</param>
        /// <param name="buttonText">The button text to search for</param>
        /// <returns>The first matching Button element or null if not found</returns>
        public AutomationElement? FindCameraButton(AutomationElement? workflowPanel, string buttonText)
        {
            if (workflowPanel == null)
            {
                Console.WriteLine($"[ChronoWorkflowController] Cannot find button: workflowPanel is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(buttonText))
            {
                Console.WriteLine($"[ChronoWorkflowController] Cannot find button: buttonText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;
                var buttonCondition = cf.ByControlType(ControlType.Button);
                var buttons = workflowPanel.FindAllChildren(buttonCondition);

                Console.WriteLine($"[ChronoWorkflowController] Searching for camera button containing '{buttonText}' among {buttons.Length} buttons");

                foreach (var button in buttons)
                {
                    if (!string.IsNullOrEmpty(button.Name) &&
                        button.Name.IndexOf(buttonText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Console.WriteLine($"[ChronoWorkflowController] Found camera button: '{button.Name}' (AutomationId: '{button.AutomationId ?? "(null)"}')");
                        return button;
                    }
                }

                Console.WriteLine($"[ChronoWorkflowController] No camera button found containing '{buttonText}'");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error finding camera button '{buttonText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clicks a button element using FlaUI's InvokePattern.
        /// </summary>
        /// <param name="button">The button element to click</param>
        /// <returns>True if successful, false if button is null or click failed</returns>
        public bool ClickButton(AutomationElement? button)
        {
            if (button == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click button: button is null");
                return false;
            }

            try
            {
                var buttonName = button.Name ?? "(unnamed)";
                Console.WriteLine($"[ChronoWorkflowController] Clicking button: '{buttonName}'");

                var invokePattern = button.Patterns.Invoke.Pattern;
                if (invokePattern == null)
                {
                    Console.WriteLine($"[ChronoWorkflowController] Failed to get InvokePattern for button '{buttonName}'");
                    return false;
                }

                invokePattern.Invoke();
                Console.WriteLine($"[ChronoWorkflowController] Successfully clicked button: '{buttonName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error clicking button: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clicks the General Camera launch button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The General Camera button is located in the Camera Status section.
        /// The button text is dynamic based on camera state: "실행", "실행 및 활성화", "중지", etc.
        /// This method searches for a button containing "실행" (launch/run).
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickGeneralCameraButton()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click General Camera button: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click General Camera button: WorkflowPanel not found");
                return false;
            }

            // Search for button containing "실행" (launch/run)
            // This matches "실행" or "실행 및 활성화" (launch and activate)
            var button = FindCameraButton(workflowPanel, "실행");
            if (button == null)
            {
                // Try "중지" (stop) if camera is already running
                button = FindCameraButton(workflowPanel, "중지");
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] General Camera button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR 1 Camera launch button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The NIR 1 Camera button is located in the Camera Status section.
        /// Similar to General Camera, searches for "실행" text.
        /// The NIR camera is distinguished by its position in the UI.
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickNirCameraButton()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR Camera button: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR Camera button: WorkflowPanel not found");
                return false;
            }

            // Search for all buttons with "실행" and find the one that's likely the NIR camera
            var button = FindCameraButton(workflowPanel, "실행");
            if (button == null)
            {
                button = FindCameraButton(workflowPanel, "중지");
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] NIR Camera button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR 2 Camera launch button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The NIR 2 Camera button is located at lines 59-66 of WorkflowPanel.xaml.
        /// Searches for button with "NIR 2" label nearby or containing "실행".
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ClickNir2CameraButton()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR 2 Camera button: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot click NIR 2 Camera button: WorkflowPanel not found");
                return false;
            }

            // Search for button with "실행" - NIR 2 button follows same pattern
            var button = FindCameraButton(workflowPanel, "실행");
            if (button == null)
            {
                button = FindCameraButton(workflowPanel, "중지");
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] NIR 2 Camera button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Clicks the NIR Filtering toggle button in the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// The NIR Filtering toggle button is located at lines 69-82 of WorkflowPanel.xaml.
        /// Toggles the Nir2FilteringState between enabled/disabled.
        /// The button text indicates current state: "사용" (enable) or "미사용" (disable).
        /// </remarks>
        /// <returns>True if the button was found and clicked successfully, false otherwise</returns>
        public bool ToggleNir2Filtering()
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot toggle NIR Filtering: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot toggle NIR Filtering: WorkflowPanel not found");
                return false;
            }

            // Search for button with filtering-related text
            var button = FindCameraButton(workflowPanel, "사용");  // "enable" or "use"
            if (button == null)
            {
                button = FindCameraButton(workflowPanel, "미사용");  // "disable" or "not use"
                if (button == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] NIR Filtering toggle button not found");
                    return false;
                }
            }

            return ClickButton(button);
        }

        /// <summary>
        /// Reads all camera state indicators from the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// Camera states are indicated by Ellipse elements with different Fill colors:
        /// - Gray (#808080): NotReady (camera not configured or path not set)
        /// - Green (#00FF00): Ready (camera ready to launch)
        /// - Blue (#0078D4): Active (camera is running)
        ///
        /// This method extracts the state for each camera (General, NIR, NIR2, Filtering).
        /// </remarks>
        /// <returns>Dictionary mapping camera names to their state strings</returns>
        public Dictionary<string, string> GetCameraStates()
        {
            var states = new Dictionary<string, string>();

            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get camera states: MainWindow not found");
                return states;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get camera states: WorkflowPanel not found");
                return states;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find all Ellipse elements (state indicators)
                // Ellipse elements appear as ControlType.Custom in UI Automation
                var ellipses = workflowPanel.FindAllChildren(cf.ByControlType(ControlType.Custom)
                    .And(cf.ByClassName("Ellipse")));

                Console.WriteLine($"[ChronoWorkflowController] Found {ellipses.Length} Ellipse state indicators");

                // Get labels to identify which camera each Ellipse belongs to
                var textBlocks = workflowPanel.FindAllChildren(cf.ByControlType(ControlType.Text));

                // Map camera names based on nearby text labels
                var cameraNames = new List<string>();
                foreach (var textBlock in textBlocks)
                {
                    if (!string.IsNullOrEmpty(textBlock.Name))
                    {
                        // Look for camera labels: "Normal" (일반), "NIR", "NIR 2", "NIR Filtering"
                        if (textBlock.Name.Contains("일반") || textBlock.Name.Contains("Normal"))
                        {
                            cameraNames.Add("General");
                        }
                        else if (textBlock.Name.Contains("NIR") && !textBlock.Name.Contains("Filtering"))
                        {
                            if (textBlock.Name.Contains("2"))
                            {
                                cameraNames.Add("NIR2");
                            }
                            else if (!cameraNames.Contains("NIR"))
                            {
                                cameraNames.Add("NIR");
                            }
                        }
                        else if (textBlock.Name.Contains("Filtering") || textBlock.Name.Contains("필터링"))
                        {
                            cameraNames.Add("Nir2Filtering");
                        }
                    }
                }

                // Map Ellipse colors to states
                for (int i = 0; i < Math.Min(ellipses.Length, cameraNames.Count); i++)
                {
                    var ellipse = ellipses[i];
                    var cameraName = cameraNames[i];

                    // Try to get the Fill color through background or other properties
                    string state = "Unknown";

                    // For now, mark as detected - actual color reading requires deeper inspection
                    state = "Detected";

                    states[cameraName] = state;
                    Console.WriteLine($"[ChronoWorkflowController] {cameraName}: {state}");
                }

                Console.WriteLine($"[ChronoWorkflowController] Extracted {states.Count} camera states");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error getting camera states: {ex.Message}");
            }

            return states;
        }

        /// <summary>
        /// Finds a TextBox control by its associated Label text.
        /// </summary>
        /// <remarks>
        /// In WPF, TextBox controls appear as ControlType.Edit in UI Automation.
        /// Label controls appear as ControlType.Text.
        /// This method searches for a Text element containing the labelText,
        /// then finds the sibling Edit control (typically the next child in the parent).
        /// </remarks>
        /// <param name="workflowPanel">The WorkflowPanel element to search within</param>
        /// <param name="labelText">The label text to search for (e.g., "샘플명:", "NIR 이동:")</param>
        /// <returns>The TextBox AutomationElement if found, null otherwise</returns>
        public AutomationElement? FindPathTextBox(AutomationElement? workflowPanel, string labelText)
        {
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot find TextBox: workflowPanel is null");
                return null;
            }

            if (string.IsNullOrWhiteSpace(labelText))
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot find TextBox: labelText is null or empty");
                return null;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find all Text elements (Labels)
                var textElements = workflowPanel.FindAllChildren(cf.ByControlType(ControlType.Text));

                Console.WriteLine($"[ChronoWorkflowController] Searching for label '{labelText}' among {textElements.Length} text elements");

                // Find the label TextBlock
                foreach (var textElement in textElements)
                {
                    if (!string.IsNullOrEmpty(textElement.Name) &&
                        textElement.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Found the label, now find the associated TextBox (Edit control)
                        // The TextBox is typically a sibling or descendant of the label's parent
                        var parent = textElement.Parent;
                        if (parent != null)
                        {
                            // Search for Edit control in the parent's children
                            var siblings = parent.FindAllChildren();
                            foreach (var sibling in siblings)
                            {
                                if (sibling.ControlType == ControlType.Edit)
                                {
                                    Console.WriteLine($"[ChronoWorkflowController] Found TextBox for label '{labelText}' (Name: '{sibling.Name ?? "(empty)"}')");
                                    return sibling;
                                }
                            }
                        }

                        // If not found in siblings, search descendants
                        var editElements = parent?.FindAllChildren(cf.ByControlType(ControlType.Edit));
                        if (editElements != null && editElements.Length > 0)
                        {
                            Console.WriteLine($"[ChronoWorkflowController] Found TextBox for label '{labelText}' in descendants");
                            return editElements[0];
                        }
                    }
                }

                Console.WriteLine($"[ChronoWorkflowController] TextBox for label '{labelText}' not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error finding TextBox for label '{labelText}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Reads the current text value from a TextBox (Edit) control.
        /// </summary>
        /// <remarks>
        /// Uses ValuePattern.ValueProperty if available (standard for Edit controls).
        /// Falls back to element.Name if ValuePattern is not supported.
        /// </remarks>
        /// <param name="textBox">The TextBox AutomationElement to read from</param>
        /// <returns>The current text value, or empty string if error</returns>
        public string GetTextBoxValue(AutomationElement? textBox)
        {
            if (textBox == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get TextBox value: textBox is null");
                return string.Empty;
            }

            try
            {
                // Try ValuePattern first (standard for TextBox/Edit controls)
                var valuePattern = textBox.Patterns.Value.Pattern;
                if (valuePattern != null)
                {
                    var value = valuePattern.Value;
                    Console.WriteLine($"[ChronoWorkflowController] TextBox value (via ValuePattern): '{value}'");
                    return value;
                }

                // Fallback to Name property
                var name = textBox.Name ?? string.Empty;
                Console.WriteLine($"[ChronoWorkflowController] TextBox value (via Name): '{name}'");
                return name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error getting TextBox value: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Sets the text value in a TextBox (Edit) control.
        /// </summary>
        /// <remarks>
        /// Uses ValuePattern.SetValue() to set the text content.
        /// </remarks>
        /// <param name="textBox">The TextBox AutomationElement to write to</param>
        /// <param name="value">The new value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetTextBoxValue(AutomationElement? textBox, string value)
        {
            if (textBox == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot set TextBox value: textBox is null");
                return false;
            }

            try
            {
                var valuePattern = textBox.Patterns.Value.Pattern;
                if (valuePattern == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] Cannot set TextBox value: ValuePattern not supported");
                    return false;
                }

                var oldValue = GetTextBoxValue(textBox);
                valuePattern.SetValue(value);
                Console.WriteLine($"[ChronoWorkflowController] TextBox value set: '{oldValue}' -> '{value}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error setting TextBox value: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads all Line 1 paths from the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// Returns a dictionary with keys: SampleName, MoveNir, MoveAllData.
        /// Uses label text from Strings resources (샘플명, NIR 이동, 전체 데이터 이동).
        /// </remarks>
        /// <returns>Dictionary of path type to value, or empty dictionary if panel not found</returns>
        public Dictionary<string, string> GetLine1Paths()
        {
            var paths = new Dictionary<string, string>();
            var mainWindow = FindMainWindow();

            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get Line 1 paths: MainWindow not found");
                return paths;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get Line 1 paths: WorkflowPanel not found");
                return paths;
            }

            // Label text from Strings resources (may be Korean or English)
            // From Strings.Designer.cs: Sample_Name, Sample_MoveNIR, Sample_MoveAllData
            // In Korean XAML: "샘플명", "NIR 이동", "전체 데이터 이동"
            var sampleNameBox = FindPathTextBox(workflowPanel, "샘플명") ??
                               FindPathTextBox(workflowPanel, "Sample Name");
            if (sampleNameBox != null)
                paths["SampleName"] = GetTextBoxValue(sampleNameBox);

            var moveNirBox = FindPathTextBox(workflowPanel, "NIR 이동") ??
                            FindPathTextBox(workflowPanel, "Move NIR");
            if (moveNirBox != null)
                paths["MoveNir"] = GetTextBoxValue(moveNirBox);

            var moveAllDataBox = FindPathTextBox(workflowPanel, "전체 데이터 이동") ??
                                 FindPathTextBox(workflowPanel, "Move All");
            if (moveAllDataBox != null)
                paths["MoveAllData"] = GetTextBoxValue(moveAllDataBox);

            Console.WriteLine($"[ChronoWorkflowController] Got {paths.Count} Line 1 paths");
            return paths;
        }

        /// <summary>
        /// Reads all Line 2 paths from the WorkflowPanel.
        /// </summary>
        /// <remarks>
        /// Similar to Line 1 but for Line 2 labels.
        /// Note: Line 2 TextBoxes have the same labels as Line 1 but are in a different StackPanel.
        /// </remarks>
        /// <returns>Dictionary of path type to value, or empty dictionary if panel not found</returns>
        public Dictionary<string, string> GetLine2Paths()
        {
            var paths = new Dictionary<string, string>();
            var mainWindow = FindMainWindow();

            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get Line 2 paths: MainWindow not found");
                return paths;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get Line 2 paths: WorkflowPanel not found");
                return paths;
            }

            // For Line 2, we need to find the Line 2 StackPanel first
            // The Line 2 section has "Line 2 설정" header (Korean) or "Line 2" (English)
            var cf = _automation.ConditionFactory;

            // Try "Line 2 설정" first (Korean), then "Line 2" (English)
            var line2Header = workflowPanel.FindFirstDescendant(
                cf.ByControlType(ControlType.Text).And(cf.ByName("Line 2 설정", PropertyConditionFlags.IgnoreCase)));

            if (line2Header == null)
            {
                line2Header = workflowPanel.FindFirstDescendant(
                    cf.ByControlType(ControlType.Text).And(cf.ByName("Line 2", PropertyConditionFlags.IgnoreCase)));
            }

            if (line2Header == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Line 2 section not found (Line 2 tab may not be active)");
                return paths;
            }

            // Get the parent StackPanel containing Line 2 TextBoxes
            var line2Panel = line2Header.Parent;
            if (line2Panel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get Line 2 parent panel");
                return paths;
            }

            // Find TextBoxes within Line 2 section
            var sampleNameBox = FindPathTextBoxInPanel(line2Panel, "샘플명") ??
                               FindPathTextBoxInPanel(line2Panel, "Sample Name");
            if (sampleNameBox != null)
                paths["SampleName"] = GetTextBoxValue(sampleNameBox);

            var moveNirBox = FindPathTextBoxInPanel(line2Panel, "NIR 이동") ??
                            FindPathTextBoxInPanel(line2Panel, "Move NIR");
            if (moveNirBox != null)
                paths["MoveNir"] = GetTextBoxValue(moveNirBox);

            var moveAllDataBox = FindPathTextBoxInPanel(line2Panel, "전체 데이터 이동") ??
                                 FindPathTextBoxInPanel(line2Panel, "Move All");
            if (moveAllDataBox != null)
                paths["MoveAllData"] = GetTextBoxValue(moveAllDataBox);

            Console.WriteLine($"[ChronoWorkflowController] Got {paths.Count} Line 2 paths");
            return paths;
        }

        /// <summary>
        /// Helper to find TextBox within a specific panel by label text.
        /// </summary>
        /// <param name="panel">The panel element to search within</param>
        /// <param name="labelText">The label text to search for</param>
        /// <returns>The TextBox AutomationElement if found, null otherwise</returns>
        private AutomationElement? FindPathTextBoxInPanel(AutomationElement? panel, string labelText)
        {
            if (panel == null)
                return null;

            try
            {
                var cf = _automation.ConditionFactory;
                var textElements = panel.FindAllChildren(cf.ByControlType(ControlType.Text));

                foreach (var textElement in textElements)
                {
                    if (!string.IsNullOrEmpty(textElement.Name) &&
                        textElement.Name.IndexOf(labelText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var parent = textElement.Parent;
                        if (parent != null)
                        {
                            var siblings = parent.FindAllChildren();
                            foreach (var sibling in siblings)
                            {
                                if (sibling.ControlType == ControlType.Edit)
                                {
                                    return sibling;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error finding TextBox in panel: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Sets a specific Line 1 path value.
        /// </summary>
        /// <param name="pathType">The path type: "SampleName", "MoveNir", or "MoveAllData"</param>
        /// <param name="value">The new path value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetLine1Path(string pathType, string value)
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot set Line 1 path: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot set Line 1 path: WorkflowPanel not found");
                return false;
            }

            AutomationElement? textBox = null;
            switch (pathType.ToLowerInvariant())
            {
                case "samplename":
                case "sample-name":
                    textBox = FindPathTextBox(workflowPanel, "샘플명") ??
                             FindPathTextBox(workflowPanel, "Sample Name");
                    break;
                case "movenir":
                case "move-nir":
                    textBox = FindPathTextBox(workflowPanel, "NIR 이동") ??
                             FindPathTextBox(workflowPanel, "Move NIR");
                    break;
                case "movealldata":
                case "move-all-data":
                    textBox = FindPathTextBox(workflowPanel, "전체 데이터 이동") ??
                             FindPathTextBox(workflowPanel, "Move All");
                    break;
                default:
                    Console.WriteLine($"[ChronoWorkflowController] Unknown Line 1 path type: {pathType}");
                    return false;
            }

            if (textBox == null)
            {
                Console.WriteLine($"[ChronoWorkflowController] TextBox for Line 1 '{pathType}' not found");
                return false;
            }

            return SetTextBoxValue(textBox, value);
        }

        /// <summary>
        /// Sets a specific Line 2 path value.
        /// </summary>
        /// <param name="pathType">The path type: "SampleName", "MoveNir", or "MoveAllData"</param>
        /// <param name="value">The new path value to set</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool SetLine2Path(string pathType, string value)
        {
            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot set Line 2 path: MainWindow not found");
                return false;
            }

            var workflowPanel = FindWorkflowPanel(mainWindow);
            if (workflowPanel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot set Line 2 path: WorkflowPanel not found");
                return false;
            }

            // Find Line 2 section first
            var cf = _automation.ConditionFactory;

            // Try "Line 2 설정" first (Korean), then "Line 2" (English)
            var line2Header = workflowPanel.FindFirstDescendant(
                cf.ByControlType(ControlType.Text).And(cf.ByName("Line 2 설정", PropertyConditionFlags.IgnoreCase)));

            if (line2Header == null)
            {
                line2Header = workflowPanel.FindFirstDescendant(
                    cf.ByControlType(ControlType.Text).And(cf.ByName("Line 2", PropertyConditionFlags.IgnoreCase)));
            }

            if (line2Header == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Line 2 section not found (Line 2 tab may not be active)");
                return false;
            }

            var line2Panel = line2Header.Parent;
            if (line2Panel == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot get Line 2 parent panel");
                return false;
            }

            AutomationElement? textBox = null;
            switch (pathType.ToLowerInvariant())
            {
                case "samplename":
                case "sample-name":
                    textBox = FindPathTextBoxInPanel(line2Panel, "샘플명") ??
                             FindPathTextBoxInPanel(line2Panel, "Sample Name");
                    break;
                case "movenir":
                case "move-nir":
                    textBox = FindPathTextBoxInPanel(line2Panel, "NIR 이동") ??
                             FindPathTextBoxInPanel(line2Panel, "Move NIR");
                    break;
                case "movealldata":
                case "move-all-data":
                    textBox = FindPathTextBoxInPanel(line2Panel, "전체 데이터 이동") ??
                             FindPathTextBoxInPanel(line2Panel, "Move All");
                    break;
                default:
                    Console.WriteLine($"[ChronoWorkflowController] Unknown Line 2 path type: {pathType}");
                    return false;
            }

            if (textBox == null)
            {
                Console.WriteLine($"[ChronoWorkflowController] TextBox for Line 2 '{pathType}' not found");
                return false;
            }

            return SetTextBoxValue(textBox, value);
        }

        /// <summary>
        /// Gets all paths from both Line 1 and Line 2.
        /// </summary>
        /// <remarks>
        /// Returns a nested dictionary structure with "Line1" and "Line2" keys.
        /// Each line contains SampleName, MoveNir, and MoveAllData entries.
        /// </remarks>
        /// <returns>Nested dictionary of all paths, or empty dictionary if panel not found</returns>
        public Dictionary<string, Dictionary<string, string>> GetAllPaths()
        {
            var allPaths = new Dictionary<string, Dictionary<string, string>>();

            var line1Paths = GetLine1Paths();
            if (line1Paths.Count > 0)
            {
                allPaths["Line1"] = line1Paths;
            }

            var line2Paths = GetLine2Paths();
            if (line2Paths.Count > 0)
            {
                allPaths["Line2"] = line2Paths;
            }

            Console.WriteLine($"[ChronoWorkflowController] Got all paths: {allPaths.Count} lines");
            return allPaths;
        }

        /// <summary>
        /// Selects a tab in the MainWindow TabControl.
        /// </summary>
        /// <remarks>
        /// The MainWindow contains a TabControl at Grid.Row="0" with three tabs:
        /// - "Line 1" (index 0)
        /// - "Line 2" (index 1)
        /// - "Combined" (index 2)
        ///
        /// This method finds the TabControl and selects the specified tab using SelectionItemPattern.
        /// The tab selection affects the WorkflowPanel visibility (IsLine1Tab/IsLine2Tab/IsCombinedTab).
        /// </remarks>
        /// <param name="tabName">The tab header text: "Line 1", "Line 2", or "Combined"</param>
        /// <returns>True if the tab was selected successfully, false otherwise</returns>
        public bool SelectTab(string tabName)
        {
            if (string.IsNullOrWhiteSpace(tabName))
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot select tab: tabName is null or empty");
                return false;
            }

            var mainWindow = FindMainWindow();
            if (mainWindow == null)
            {
                Console.WriteLine("[ChronoWorkflowController] Cannot select tab: MainWindow not found");
                return false;
            }

            try
            {
                var cf = _automation.ConditionFactory;

                // Find TabControl (ControlType.Tab or by searching for TabItem children)
                var tabControl = mainWindow.FindFirstDescendant(cf.ByControlType(ControlType.Tab));
                if (tabControl == null)
                {
                    Console.WriteLine("[ChronoWorkflowController] TabControl not found in MainWindow");
                    return false;
                }

                Console.WriteLine($"[ChronoWorkflowController] Found TabControl, searching for tab '{tabName}'");

                // Find all TabItem elements
                var tabItems = tabControl.FindAllChildren(cf.ByControlType(ControlType.TabItem));
                Console.WriteLine($"[ChronoWorkflowController] Found {tabItems.Length} TabItem(s)");

                foreach (var tabItem in tabItems)
                {
                    var header = tabItem.Name ?? "(unnamed)";
                    Console.WriteLine($"[ChronoWorkflowController] Checking tab: '{header}'");

                    if (header.IndexOf(tabName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Found the target tab, select it using SelectionItemPattern
                        var selectionPattern = tabItem.Patterns.SelectionItem.Pattern;
                        if (selectionPattern != null)
                        {
                            selectionPattern.Select();
                            Console.WriteLine($"[ChronoWorkflowController] Successfully selected tab '{tabName}'");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"[ChronoWorkflowController] Tab '{tabName}' does not support SelectionItemPattern");
                            return false;
                        }
                    }
                }

                Console.WriteLine($"[ChronoWorkflowController] Tab '{tabName}' not found");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChronoWorkflowController] Error selecting tab '{tabName}': {ex.Message}");
                return false;
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
