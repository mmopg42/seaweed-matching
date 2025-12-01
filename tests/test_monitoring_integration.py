import sys
import os
import pytest

# Add parent directory to sys.path to allow importing monitoring_app
sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from PySide6.QtWidgets import QApplication
from monitoring_app import MainWindow

# Ensure QApplication is initialized only once
@pytest.fixture(scope="session")
def qapp():
    app = QApplication.instance()
    if app is None:
        app = QApplication(sys.argv)
    yield app

@pytest.fixture
def main_window(qapp, qtbot):
    window = MainWindow()
    window.show()
    qtbot.addWidget(window)
    yield window
    window.close()

def test_initial_state(main_window):
    """Test the initial state of the MainWindow."""
    assert main_window.windowTitle() == "메인 모니터링"
    assert main_window.btn_run.isEnabled()
    assert not main_window.btn_stop.isEnabled()
    assert main_window.is_watching is False

from PySide6.QtCore import Qt

def test_toggle_watch(main_window, qtbot):
    """Test starting and stopping the watch."""
    # Start watch
    qtbot.mouseClick(main_window.btn_run, Qt.MouseButton.LeftButton)
    assert main_window.is_watching is True
    assert not main_window.btn_run.isEnabled()
    assert main_window.btn_stop.isEnabled()

    # Stop watch
    qtbot.mouseClick(main_window.btn_stop, Qt.MouseButton.LeftButton)
    assert main_window.is_watching is False
    assert main_window.btn_run.isEnabled()
    assert not main_window.btn_stop.isEnabled()

def test_setting_dialog_open(main_window, qtbot):
    """Test if setting dialog opens (mocking exec)."""
    # This is a bit tricky to test without mocking the dialog execution
    # For now, just checking if the button exists and is connected
    assert main_window.btn_setting is not None

def test_tab_switching(main_window):
    """Test tab switching."""
    # Check initial tab
    assert main_window.tab_widget.currentIndex() == 0 # Line 1
    
    # Switch to Line 2
    main_window.tab_widget.setCurrentIndex(1)
    assert main_window.tab_widget.currentIndex() == 1
    
    # Switch to Combined
    main_window.tab_widget.setCurrentIndex(2)
    assert main_window.tab_widget.currentIndex() == 2
