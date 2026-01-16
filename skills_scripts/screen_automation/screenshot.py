"""
Screen Capture Module
Provides functionality to capture screenshots of the screen or specific regions.
"""

import pyautogui
from pathlib import Path
from datetime import datetime
from typing import Optional, Tuple


class Screenshot:
    """Screenshot capture functionality."""

    def __init__(self, output_dir: str = "./screenshots"):
        """
        Initialize Screenshot module.

        Args:
            output_dir: Directory to save screenshots (default: ./screenshots)
        """
        self.output_dir = Path(output_dir)
        self.output_dir.mkdir(parents=True, exist_ok=True)

    def capture_full_screen(self, filename: Optional[str] = None) -> str:
        """
        Capture the entire screen.

        Args:
            filename: Custom filename (without extension). If None, uses timestamp.

        Returns:
            Full path to the saved screenshot
        """
        if filename is None:
            filename = datetime.now().strftime("%Y%m%d_%H%M%S")

        filepath = self.output_dir / f"{filename}.png"
        screenshot = pyautogui.screenshot()
        screenshot.save(str(filepath))
        print(f"Screenshot saved: {filepath}")
        return str(filepath)

    def capture_region(
        self,
        x: int,
        y: int,
        width: int,
        height: int,
        filename: Optional[str] = None
    ) -> str:
        """
        Capture a specific region of the screen.

        Args:
            x: Left coordinate
            y: Top coordinate
            width: Region width
            height: Region height
            filename: Custom filename (without extension).

        Returns:
            Full path to the saved screenshot
        """
        if filename is None:
            filename = datetime.now().strftime("%Y%m%d_%H%M%S_region")

        filepath = self.output_dir / f"{filename}.png"
        screenshot = pyautogui.screenshot(region=(x, y, width, height))
        screenshot.save(str(filepath))
        print(f"Region screenshot saved: {filepath}")
        return str(filepath)

    def get_screen_size(self) -> Tuple[int, int]:
        """
        Get the primary screen size.

        Returns:
            Tuple of (width, height)
        """
        size = pyautogui.size()
        print(f"Screen size: {size.width}x{size.height}")
        return size.width, size.height

    def capture_with_cursor(
        self,
        filename: Optional[str] = None,
        highlight_region: Optional[Tuple[int, int, int, int]] = None
    ) -> str:
        """
        Capture screen and optionally highlight a region.

        Args:
            filename: Custom filename.
            highlight_region: Optional (x, y, width, height) to highlight.

        Returns:
            Full path to the saved screenshot
        """
        from PIL import Image, ImageDraw

        if filename is None:
            filename = datetime.now().strftime("%Y%m%d_%H%M%S_annotated")

        filepath = self.output_dir / f"{filename}.png"
        screenshot = pyautogui.screenshot()

        if highlight_region:
            draw = ImageDraw.Draw(screenshot)
            x, y, w, h = highlight_region
            draw.rectangle([x, y, x + w, y + h], outline="red", width=3)

        screenshot.save(str(filepath))
        print(f"Annotated screenshot saved: {filepath}")
        return str(filepath)


def main():
    """CLI interface for screenshot functionality."""
    import argparse

    parser = argparse.ArgumentParser(description="Screen Capture Tool")
    parser.add_argument("-o", "--output", default="./screenshots",
                        help="Output directory for screenshots")
    parser.add_argument("-f", "--filename", help="Custom filename (without extension)")
    parser.add_argument("-r", "--region", nargs=4, type=int, metavar=("X", "Y", "W", "H"),
                        help="Capture region: x y width height")
    parser.add_argument("-s", "--size", action="store_true",
                        help="Show screen size and exit")

    args = parser.parse_args()
    screenshot = Screenshot(args.output)

    if args.size:
        screenshot.get_screen_size()
    elif args.region:
        screenshot.capture_region(*args.region, args.filename)
    else:
        screenshot.capture_full_screen(args.filename)


if __name__ == "__main__":
    main()
