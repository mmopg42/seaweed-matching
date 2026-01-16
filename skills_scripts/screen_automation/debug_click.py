"""
Debug click position by marking it on screenshot
"""

from PIL import Image, ImageDraw
from pathlib import Path
import sys


def mark_click_position(screenshot_path: str, x: int, y: int, radius: int = 20):
    """
    Mark the click position on screenshot with a red circle.

    Args:
        screenshot_path: Path to screenshot image
        x: X coordinate to mark
        y: Y coordinate to mark
        radius: Circle radius
    """
    img = Image.open(screenshot_path)
    draw = ImageDraw.Draw(img)

    # Draw red circle
    draw.ellipse([x - radius, y - radius, x + radius, y + radius],
                 outline="red", width=5)

    # Draw crosshair
    draw.line([x - radius - 10, y, x + radius + 10, y], fill="red", width=3)
    draw.line([x, y - radius - 10, x, y + radius + 10], fill="red", width=3)

    # Add coordinate text
    text = f"({x}, {y})"
    # Approximate text positioning
    draw.text((x + radius + 5, y - 10), text, fill="yellow")

    # Save debug image
    output_path = Path(screenshot_path).parent / f"debug_click_{Path(screenshot_path).stem}.png"
    img.save(output_path)
    print(f"Debug image saved: {output_path}")
    print(f"Marked position: x={x}, y={y}")
    return str(output_path)


if __name__ == "__main__":
    if len(sys.argv) < 4:
        print("Usage: python debug_click.py <screenshot_path> <x> <y> [radius]")
        sys.exit(1)

    screenshot = sys.argv[1]
    x = int(sys.argv[2])
    y = int(sys.argv[3])
    radius = int(sys.argv[4]) if len(sys.argv) > 4 else 20

    mark_click_position(screenshot, x, y, radius)
