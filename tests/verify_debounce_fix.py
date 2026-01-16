import time
import os
import shutil
from datetime import datetime

# Configuration
WATCH_ROOT = r"C:\workspace\seaweed\program\data\Normal_1"
TIMESTAMP = datetime.now().strftime("%y%m%du%H%M%S") # using 'u' to avoid timezone confusion in regex for now, or standard 'T'
TIMESTAMP = datetime.now().strftime("%y%m%dT%H%M%S")
FOLDER_NAME = f"C{TIMESTAMP}_0"
FOLDER_PATH = os.path.join(WATCH_ROOT, FOLDER_NAME)

def setup():
    if not os.path.exists(WATCH_ROOT):
        os.makedirs(WATCH_ROOT)
    print(f"Monitoring Root: {WATCH_ROOT}")

def run_test():
    print(f"1. Creating Normal Folder: {FOLDER_NAME}")
    os.makedirs(FOLDER_PATH)
    print("   -> Folder created. Expect 'Group Created' in UI (with No Image).")
    
    # Wait 0.6 seconds. 
    # Old behavior: EventProcessor debounce is 2.0s. It would likely block any immediate re-check.
    # New behavior: EventProcessor debounce is ~0.2s. This check should be allowed.
    print("2. Waiting 0.6 seconds (Testing < 2s debounce)...")
    time.sleep(0.6)
    
    print("3. Creating 'stitched_original.png'")
    # Create a dummy image
    image_path = os.path.join(FOLDER_PATH, "stitched_original.png")
    with open(image_path, "wb") as f:
        f.write(b"\x00" * 1024) # 1KB dummy content
        
    print(f"   -> Image created at {image_path}")
    print("   -> Expect '[이미지 발견!]' log in UI immediately.")
    print("      (If 2s debounce bug exists, this might be delayed or skipped until next poll cycle)")

def cleanup():
    input("Press Enter to cleanup and exit...")
    if os.path.exists(FOLDER_PATH):
        shutil.rmtree(FOLDER_PATH)
    print("Cleanup done.")

if __name__ == "__main__":
    setup()
    run_test()
    cleanup()
