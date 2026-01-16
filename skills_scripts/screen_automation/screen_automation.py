"""
Screen Automation Tool
Combined screenshot and click functionality for screen automation tasks.
"""

from pathlib import Path
from typing import Optional, Tuple
import json

from .screenshot import Screenshot
from .clicker import Clicker


class ScreenAutomation:
    """Combined screen automation functionality."""

    def __init__(self, output_dir: str = "./screenshots", safe_mode: bool = True):
        """
        Initialize Screen Automation.

        Args:
            output_dir: Directory for screenshots
            safe_mode: Enable fail-safe for mouse operations
        """
        self.screenshot = Screenshot(output_dir)
        self.clicker = Clicker(safe_mode=safe_mode)

    def capture_and_click(self, x: int, y: int,
                          capture_filename: Optional[str] = None,
                          delay: float = 0.5) -> Tuple[str, Tuple[int, int]]:
        """
        Capture screen then click at coordinates.

        Args:
            x: X coordinate to click
            y: Y coordinate to click
            capture_filename: Filename for screenshot
            delay: Delay between capture and click

        Returns:
            Tuple of (screenshot_path, (x, y))
        """
        filepath = self.screenshot.capture_full_screen(capture_filename)
        if delay > 0:
            import time
            time.sleep(delay)
        self.clicker.click_at(x, y)
        return filepath, (x, y)

    def save_click_positions(self, positions: dict, filename: str = "click_positions.json") -> str:
        """
        Save click positions to JSON file.

        Args:
            positions: Dictionary of position names to (x, y) coordinates
            filename: Output filename

        Returns:
            Path to saved file
        """
        output_path = Path(self.screenshot.output_dir) / filename
        with open(output_path, "w") as f:
            json.dump(positions, f, indent=2)
        print(f"Click positions saved to: {output_path}")
        return str(output_path)

    def load_click_positions(self, filename: str = "click_positions.json") -> dict:
        """
        Load click positions from JSON file.

        Args:
            filename: Input filename

        Returns:
            Dictionary of position names to coordinates
        """
        input_path = Path(self.screenshot.output_dir) / filename
        with open(input_path) as f:
            positions = json.load(f)
        print(f"Loaded {len(positions)} positions from: {input_path}")
        return positions

    def replay_clicks(self, positions: dict,
                      delay_between: float = 0.5) -> None:
        """
        Replay clicks from saved positions.

        Args:
            positions: Dictionary of position names to (x, y) coordinates
            delay_between: Delay between clicks
        """
        print(f"Replaying {len(positions)} click positions...")
        for name, coords in positions.items():
            print(f"Clicking '{name}' at {coords}")
            self.clicker.click_at(coords[0], coords[1])
            import time
            time.sleep(delay_between)


def main():
    """CLI interface for screen automation."""
    import argparse

    parser = argparse.ArgumentParser(description="Screen Automation Tool")
    subparsers = parser.add_subparsers(dest="command", help="Available commands")

    # Capture command
    capture_parser = subparsers.add_parser("capture", help="Take screenshot")
    capture_parser.add_argument("-o", "--output", default="./screenshots")
    capture_parser.add_argument("-f", "--filename")

    # Click command
    click_parser = subparsers.add_parser("click", help="Click at position")
    click_parser.add_argument("-x", type=int, required=True)
    click_parser.add_argument("-y", type=int, required=True)
    click_parser.add_argument("--safe", action="store_true", default=True)

    # Position command
    pos_parser = subparsers.add_parser("position", help="Get mouse position")

    args = parser.parse_args()

    automation = ScreenAutomation()

    if args.command == "capture":
        automation.screenshot.capture_full_screen(args.filename)
    elif args.command == "click":
        automation.clicker.click_at(args.x, args.y)
    elif args.command == "position":
        automation.clicker.get_position()
    else:
        parser.print_help()


if __name__ == "__main__":
    main()
