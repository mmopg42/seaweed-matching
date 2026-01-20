using System;
using System.Collections.Generic;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Conditions;
using FlaUI.UIA3;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// High-level API for ChronoView SetupWindow automation.
    /// Consolidates all SetupWindow interaction logic into a dedicated, testable class.
    /// </summary>
    /// <remarks>
    /// This class provides a complete API for SetupWindow interactions including:
    /// - Finding the SetupWindow via ChronoWindowFinder
    /// - Clicking the Settings button to open SettingsDialog
    /// - Launching camera programs (General, NIR1, NIR2)
    /// - Toggling NIR filtering state
    /// - Starting monitoring (transition to MainWindow)
    /// - Closing the SetupWindow
    /// - Reading camera button states
    /// - Waiting for MainWindow after Start button click
    ///
    /// SetupWindow button AutomationId values (from SetupWindow.xaml):
    /// - SetupSettingsButton (gear icon in title bar)
    /// - SetupGeneralCameraButton
    /// - SetupNir1CameraButton
    /// - SetupNir2CameraButton
    /// - SetupNirFilteringButton
    /// - SetupStartButton
    /// - SetupMinimizeButton
    /// - SetupCloseButton
    /// </remarks>
    public class ChronoSetupWindowController : IDisposable
    {
        private readonly UIA3Automation _automation;
        private readonly ChronoWindowFinder _windowFinder;
        private const int DefaultPollIntervalMs = 200;

        /// <summary>
        /// Initializes a new instance of the ChronoSetupWindowController class.
        /// </summary>
        /// <param name="automation">The UIA3Automation instance to use for UI automation</param>
        public ChronoSetupWindowController(UIA3Automation automation)
        {
            _automation = automation ?? throw new ArgumentNullException(nameof(automation));
            _windowFinder = new ChronoWindowFinder(automation);
        }

        /// <summary>
        /// Initializes a new instance of the ChronoSetupWindowController class,
        /// creating its own UIA3Automation instance.
        /// </summary>
        public ChronoSetupWindowController() : this(new UIA3Automation())
        {
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
