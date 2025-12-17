"""
Timestamp Swapper - Dummy Data Generator

Swaps timestamps between NIR files and Normal folders to create valid test data.
- Parses timestamps from filenames
- Sorts by timestamp
- NIR: Treats .spc and .txt as a set (same basename)
- Swaps timestamps by copying files and renaming them
"""

import os
import re
import shutil
import sys
from datetime import datetime
from pathlib import Path
from collections import defaultdict


def extract_timestamp(name, data_type):
    """Extract timestamp from filename (same logic as data_simulator.py)"""
    try:
        if data_type == 'nir':
            # Try YYYYMMDD format first (e.g., run_120251201T140542)
            match = re.search(r'(\d{4})(\d{2})(\d{2})T(\d{6})', name)
            if match:
                try:
                    year = int(match.group(1))
                    month = int(match.group(2))
                    day = int(match.group(3))
                    time_part = match.group(4)
                    hour = int(time_part[0:2])
                    minute = int(time_part[2:4])
                    second = int(time_part[4:6])
                    return datetime(year, month, day, hour, minute, second)
                except ValueError:
                    pass  # Invalid date, try next format

            # NIR: Old format (YY prefix + MMDD??)
            match = re.search(r'(\d{2})(\d{6})T(\d{6})', name)
            if match:
                year = int('20' + match.group(1))
                date_part = match.group(2)
                time_part = match.group(3)
                month = int(date_part[0:2])
                day = int(date_part[2:4])
                hour = int(time_part[0:2])
                minute = int(time_part[2:4])
                second = int(time_part[4:6])
                return datetime(year, month, day, hour, minute, second)

        elif data_type == 'normal':
            # Normal folder: C251201T140543_0 → YYMMDD + HHMMSS
            match = re.search(r'C(\d{6})T(\d{6})', name)
            if match:
                date_part = match.group(1)
                time_part = match.group(2)
                year = int('20' + date_part[0:2])
                month = int(date_part[2:4])
                day = int(date_part[4:6])
                hour = int(time_part[0:2])
                minute = int(time_part[2:4])
                second = int(time_part[4:6])
                return datetime(year, month, day, hour, minute, second)

    except Exception as e:
        print(f"Warning: Error parsing timestamp from {name}: {e}")
    return None


def get_timestamped_nir_sets(nir_folder):
    """Get NIR file sets (.spc + .txt) with parsed timestamps, sorted by timestamp

    Handles naming patterns where .spc and .txt have different suffixes:
    - run_120251201T140542.spc (basename without A)
    - run_120251201T140542A.txt (basename with A)
    """
    if not os.path.exists(nir_folder):
        return []

    # Collect all .spc and .txt files
    spc_files = {}
    txt_files = {}

    for filename in os.listdir(nir_folder):
        if filename.endswith('.spc'):
            basename = Path(filename).stem
            spc_files[basename] = filename
        elif filename.endswith('.txt'):
            basename = Path(filename).stem
            txt_files[basename] = filename

    # Match .spc files with corresponding .txt files
    items = []

    for spc_basename, spc_filename in spc_files.items():
        # Try exact match first
        if spc_basename in txt_files:
            txt_filename = txt_files[spc_basename]
        # Try with 'A' suffix
        elif spc_basename + 'A' in txt_files:
            txt_filename = txt_files[spc_basename + 'A']
        else:
            print(f"Warning: No matching .txt file for {spc_filename}")
            continue

        # Parse timestamp from .spc basename (the one without 'A')
        timestamp = extract_timestamp(spc_basename, 'nir')
        if timestamp:
            items.append({
                'basename': spc_basename,  # Use .spc basename (without 'A')
                'spc_file': spc_filename,
                'txt_file': txt_filename,
                'timestamp': timestamp
            })
        else:
            print(f"Warning: Could not parse timestamp from NIR basename: {spc_basename}")

    # Sort by timestamp
    items.sort(key=lambda x: x['timestamp'])
    return items


def get_timestamped_normal_folders(normal_folder):
    """Get Normal folders with parsed timestamps, sorted by timestamp"""
    if not os.path.exists(normal_folder):
        return []

    items = []
    for name in os.listdir(normal_folder):
        folder_path = os.path.join(normal_folder, name)
        if os.path.isdir(folder_path):
            timestamp = extract_timestamp(name, 'normal')
            if timestamp:
                items.append({
                    'name': name,
                    'timestamp': timestamp
                })
            else:
                print(f"Warning: Could not parse timestamp from Normal folder: {name}")

    # Sort by timestamp
    items.sort(key=lambda x: x['timestamp'])
    return items


def extract_timestamp_pattern(name, data_type):
    """Extract the timestamp pattern from filename for replacement

    Returns: (prefix, timestamp_part, suffix)
    - NIR: ('run_', '120251201T140542', '') or ('run_', '120251201T140542', 'A')
    - Normal: ('C', '251201T140543', '_0')
    """
    try:
        if data_type == 'nir':
            # Try YYYYMMDD format first
            match = re.search(r'(.*)(\d{4}\d{2}\d{2}T\d{6})(.*)', name)
            if match:
                return (match.group(1), match.group(2), match.group(3))

            # Try old format (YY prefix)
            match = re.search(r'(.*)(\d{2}\d{6}T\d{6})(.*)', name)
            if match:
                return (match.group(1), match.group(2), match.group(3))

        elif data_type == 'normal':
            # Normal: C251201T140543_0
            match = re.search(r'(C)(\d{6}T\d{6})(.*)', name)
            if match:
                return (match.group(1), match.group(2), match.group(3))
    except Exception as e:
        print(f"Warning: Error extracting pattern from {name}: {e}")

    return None


def swap_timestamps_and_copy(source_nir, source_normal, target_base):
    """Swap timestamps between NIR files and Normal folders, copy to target"""

    # Get timestamped items (sorted by timestamp)
    nir_items = get_timestamped_nir_sets(source_nir)
    normal_items = get_timestamped_normal_folders(source_normal)

    if not nir_items:
        print(f"ERROR: No NIR file sets with valid timestamps found in {source_nir}")
        return False

    if not normal_items:
        print(f"ERROR: No Normal folders with valid timestamps found in {source_normal}")
        return False

    print(f"Found {len(nir_items)} NIR file sets with timestamps")
    print(f"Found {len(normal_items)} Normal folders with timestamps")
    print()

    # Show timestamp ranges
    print(f"NIR timestamp range: {nir_items[0]['timestamp']} to {nir_items[-1]['timestamp']}")
    print(f"Normal timestamp range: {normal_items[0]['timestamp']} to {normal_items[-1]['timestamp']}")
    print()

    # Determine how many pairs to process
    pair_count = min(len(nir_items), len(normal_items))
    print(f"Will process {pair_count} pairs (sorted by timestamp)")
    print()

    # Create target directories
    target_nir = os.path.join(target_base, 'nir')
    target_normal = os.path.join(target_base, 'normal')
    os.makedirs(target_nir, exist_ok=True)
    os.makedirs(target_normal, exist_ok=True)

    # Process each pair
    success_count = 0

    for i in range(pair_count):
        nir_item = nir_items[i]
        normal_item = normal_items[i]

        nir_basename = nir_item['basename']
        normal_folder = normal_item['name']

        # Extract timestamp patterns
        nir_pattern = extract_timestamp_pattern(nir_basename, 'nir')
        normal_pattern = extract_timestamp_pattern(normal_folder, 'normal')

        if not nir_pattern or not normal_pattern:
            print(f"✗ Error: Could not extract timestamp patterns for pair {i+1}")
            continue

        nir_prefix, nir_timestamp, nir_suffix = nir_pattern
        normal_prefix, normal_timestamp, normal_suffix = normal_pattern

        try:
            # Create new NIR names with Normal's timestamp (no suffix from normal)
            new_nir_basename = f"{nir_prefix}{normal_timestamp}"

            # Copy NIR .spc file with swapped timestamp
            source_spc = os.path.join(source_nir, nir_item['spc_file'])
            target_spc_name = f"{new_nir_basename}.spc"
            target_spc = os.path.join(target_nir, target_spc_name)
            shutil.copy2(source_spc, target_spc)

            # Copy NIR .txt file with swapped timestamp (keep 'A' suffix if it exists)
            source_txt = os.path.join(source_nir, nir_item['txt_file'])
            # Check if original txt had 'A' suffix
            if nir_item['txt_file'].endswith('A.txt'):
                target_txt_name = f"{new_nir_basename}A.txt"
            else:
                target_txt_name = f"{new_nir_basename}.txt"
            target_txt = os.path.join(target_nir, target_txt_name)
            shutil.copy2(source_txt, target_txt)

            # Create new Normal folder name with NIR's timestamp
            new_normal_name = f"{normal_prefix}{nir_timestamp}{normal_suffix}"

            # Copy Normal folder with swapped timestamp
            source_normal_folder = os.path.join(source_normal, normal_folder)
            target_normal_folder = os.path.join(target_normal, new_normal_name)
            shutil.copytree(source_normal_folder, target_normal_folder)

            print(f"Pair {i+1}/{pair_count}:")
            print(f"  NIR set: {nir_basename} ({nir_item['timestamp']})")
            print(f"    {nir_item['spc_file']} -> {target_spc_name}")
            print(f"    {nir_item['txt_file']} -> {target_txt_name}")
            print(f"  Normal: {normal_folder}/ ({normal_item['timestamp']})")
            print(f"    -> {new_normal_name}/")
            print()

            success_count += 1

        except Exception as e:
            print(f"✗ Error processing pair {i+1}: {e}")
            print()

    print(f"Completed: {success_count}/{pair_count} pairs processed successfully")
    return True


def main():
    """Main entry point"""
    print("=" * 60)
    print("Timestamp Swapper - Dummy Data Generator")
    print("=" * 60)
    print()

    # Get NIR source folder
    if len(sys.argv) > 1:
        source_nir = sys.argv[1]
    else:
        source_nir = input("Enter NIR source folder path: ").strip()

    if not source_nir:
        print("ERROR: NIR source folder is required")
        return

    if not os.path.exists(source_nir):
        print(f"ERROR: NIR source folder not found: {source_nir}")
        return

    # Get Normal source folder
    if len(sys.argv) > 2:
        source_normal = sys.argv[2]
    else:
        source_normal = input("Enter Normal source folder path: ").strip()

    if not source_normal:
        print("ERROR: Normal source folder is required")
        return

    if not os.path.exists(source_normal):
        print(f"ERROR: Normal source folder not found: {source_normal}")
        return

    # Get target folder
    if len(sys.argv) > 3:
        target_base = sys.argv[3]
    else:
        target_base = input("Enter target folder path: ").strip()

    if not target_base:
        print("ERROR: Target folder is required")
        return

    # Create target folder if it doesn't exist
    os.makedirs(target_base, exist_ok=True)

    print()
    print(f"NIR source:    {source_nir}")
    print(f"Normal source: {source_normal}")
    print(f"Target:        {target_base}")
    print(f"  -> Will create: {os.path.join(target_base, 'nir')}")
    print(f"  -> Will create: {os.path.join(target_base, 'normal')}")
    print()

    # Confirm
    confirm = input("Proceed? (y/n): ").strip().lower()
    if confirm != 'y':
        print("Cancelled")
        return

    print()
    print("Processing...")
    print()

    # Execute swap
    swap_timestamps_and_copy(source_nir, source_normal, target_base)

    print()
    print("Done!")


if __name__ == "__main__":
    main()
