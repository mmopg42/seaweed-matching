"""
Mouse Click Automation Module
Provides functionality to click at specified screen coordinates.
"""

import pyautogui
import time
from typing import Tuple, Optional


class Clicker:
    """Mouse click automation functionality."""

    # Safety settings
    SAFE_MOVE_DURATION = 0.2  # Smooth mouse movement
    DEFAULT_CLICK_DELAY = 0.1

    def __init__(self, safe_mode: bool = True):
        """
        Initialize Clicker module.

        Args:
            safe_mode: If True, enables fail-safe (move mouse to corner to abort)
        """
        self.safe_mode = safe_mode
        if safe_mode:
            pyautogui.FAILSAFE = True
            print("Safe mode enabled: Move mouse to corner to abort")

    def click_at(self, x: int, y: int, button: str = "left",
                 clicks: int = 1, interval: float = 0.0) -> None:
        """
        Click at the specified coordinates.

        Args:
            x: X coordinate
            y: Y coordinate
            button: Mouse button ('left', 'right', 'middle')
            clicks: Number of clicks (1 for single, 2 for double)
            interval: Delay between clicks if clicks > 1
        """
        print(f"Clicking at ({x}, {y}) with {button} button ({clicks} click(s))")
        pyautogui.click(x=x, y=y, clicks=clicks, interval=interval, button=button)

    def move_to(self, x: int, y: int, duration: Optional[float] = None) -> None:
        """
        Move mouse to coordinates without clicking.

        Args:
            x: X coordinate
            y: Y coordinate
            duration: Movement duration in seconds (default: SAFE_MOVE_DURATION)
        """
        if duration is None:
            duration = self.SAFE_MOVE_DURATION
        print(f"Moving to ({x}, {y}) over {duration}s")
        pyautogui.moveTo(x, y, duration=duration)

    def click_sequence(self, coordinates: list[Tuple[int, int]],
                       delay_between: float = 0.5) -> None:
        """
        Click multiple coordinates in sequence.

        Args:
            coordinates: List of (x, y) tuples
            delay_between: Delay between each click in seconds
        """
        print(f"Clicking {len(coordinates)} positions in sequence")
        for i, (x, y) in enumerate(coordinates, 1):
            print(f"[{i}/{len(coordinates)}] Clicking at ({x}, {y})")
            pyautogui.click(x, y)
            if i < len(coordinates):
                time.sleep(delay_between)
        print("Sequence completed")

    def drag_to(self, start_x: int, start_y: int,
                end_x: int, end_y: int, duration: float = 0.5) -> None:
        """
        Drag from start to end coordinates.

        Args:
            start_x: Start X coordinate
            start_y: Start Y coordinate
            end_x: End X coordinate
            end_y: End Y coordinate
            duration: Drag duration in seconds
        """
        print(f"Dragging from ({start_x}, {start_y}) to ({end_x}, {end_y})")
        pyautogui.dragTo(end_x, end_y, duration=duration, button="left")

    def scroll_at(self, x: int, y: int, clicks: int) -> None:
        """
        Scroll at the specified coordinates.

        Args:
            x: X coordinate
            y: Y coordinate
            clicks: Number of scroll clicks (positive=up, negative=down)
        """
        print(f"Scrolling {clicks} clicks at ({x}, {y})")
        pyautogui.scroll(clicks, x, y)

    def get_position(self) -> Tuple[int, int]:
        """
        Get current mouse position.

        Returns:
            Tuple of (x, y) coordinates
        """
        x, y = pyautogui.position()
        print(f"Current mouse position: ({x}, {y})")
        return x, y

    def wait_and_click(self, x: int, y: int, delay: float) -> None:
        """
        Wait for specified delay, then click.

        Args:
            x: X coordinate
            y: Y coordinate
            delay: Delay in seconds before clicking
        """
        print(f"Waiting {delay}s before clicking at ({x}, {y})")
        time.sleep(delay)
        self.click_at(x, y)


def main():
    """CLI interface for clicker functionality."""
    import argparse

    parser = argparse.ArgumentParser(description="Mouse Click Automation Tool")
    parser.add_argument("-x", type=int, required=True, help="X coordinate")
    parser.add_argument("-y", type=int, required=True, help="Y coordinate")
    parser.add_argument("-b", "--button", default="left",
                        choices=["left", "right", "middle"],
                        help="Mouse button (default: left)")
    parser.add_argument("-c", "--clicks", type=int, default=1,
                        help="Number of clicks (default: 1)")
    parser.add_argument("-d", "--delay", type=float, default=0.0,
                        help="Delay before clicking in seconds")
    parser.add_argument("--no-safe", action="store_true",
                        help="Disable safe mode")
    parser.add_argument("--position", action="store_true",
                        help="Show current mouse position and exit")

    args = parser.parse_args()

    clicker = Clicker(safe_mode=not args.no_safe)

    if args.position:
        clicker.get_position()
    else:
        if args.delay > 0:
            clicker.wait_and_click(args.x, args.y, args.delay)
        else:
            clicker.click_at(args.x, args.y, button=args.button, clicks=args.clicks)


if __name__ == "__main__":
    main()
