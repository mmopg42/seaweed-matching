"""
Data Simulator - Timestamp-based File Move Tool

Simulates real-time data generation by moving files from source to target
at precise timestamp intervals for ChronoView testing.
"""

import os
import sys
import re
import time
import json
import random
import statistics
import threading
import shutil
import subprocess
import argparse
from collections import deque
from datetime import datetime
import uuid

# GUI imports (only imported when needed)
try:
    import tkinter as tk
    from tkinter import ttk, filedialog, scrolledtext, messagebox
    GUI_AVAILABLE = True
except ImportError:
    GUI_AVAILABLE = False

try:
    from PIL import Image
    PIL_AVAILABLE = True
except Exception:
    PIL_AVAILABLE = False


class DataSimulator:
    def __init__(self):
        # Get script directory for relative paths (works with EXE packaging)
        if getattr(sys, 'frozen', False):
            # Running as compiled EXE
            self.script_dir = os.path.dirname(sys.executable)
        else:
            # Running as script
            self.script_dir = os.path.dirname(os.path.abspath(__file__))
        
        # Config directory: use script_dir for EXE, or original path for script
        if getattr(sys, 'frozen', False):
            self.config_dir = self.script_dir
        else:
            self.config_dir = r"C:\workspace\seaweed\gui_kiro\task_helper\data_test"
        self.config_file = os.path.join(self.config_dir, "simulator_config.json")
        
        # Dummy cache files (support PyInstaller onefile via _MEIPASS)
        resource_dir = getattr(sys, '_MEIPASS', self.script_dir)
        self.dummy_profile_file = os.path.join(resource_dir, "dummy_profile.json")
        self.dummy_manifest_file = os.path.join(resource_dir, "dummy_manifest.json")

        # Default values
        self.original_base = ""  # Original folder (for reset source)
        self.source_line1 = r"Z:\윤태경\seaweed\program\data\2025_A046"  # Line1 source folder
        self.source_line2 = ""  # Line2 source folder
        self.target_base = ""
        self.move_folder = ""  # Move folder (under target)
        self.trash_folder = ""  # Trash folder (under target)
        self.is_running = False
        self.simulation_thread = None
        self.moved_items = []  # Track moved items for reset (target → source mapping)
        self.generate_outliers = False
        self.split_normal_folders = False
        self.delay_normal_creation = False
        self.use_dummy_data = False  # Dummy mode flag
        self.outlier_window_size = 100
        self.outlier_min_samples = 30
        self.outlier_z_threshold = 3.0
        self.normal_image_count = 0
        self._normal_widths = deque(maxlen=self.outlier_window_size)
        self._normal_heights = deque(maxlen=self.outlier_window_size)
        self._outlier_warning_logged = False
        self._outlier_warning_logged = False
        self._outlier_lock = threading.Lock()
        self.current_simulation_id = None

        # State file for cross-process status queries
        self.state_file = os.path.join(self.config_dir, "simulation_state.json")

        # Load saved configuration
        self.load_config()

    def load_config(self):
        """Load configuration from JSON file"""
        try:
            if os.path.exists(self.config_file):
                with open(self.config_file, 'r', encoding='utf-8') as f:
                    config = json.load(f)
                    self.original_base = config.get('original_base', self.original_base)
                    self.source_line1 = config.get('source_line1', config.get('source_base', self.source_line1))  # Backward compatibility
                    self.source_line2 = config.get('source_line2', self.source_line2)
                    self.target_base = config.get('target_base', self.target_base)
                    self.move_folder = config.get('move_folder', self.move_folder)
                    self.trash_folder = config.get('trash_folder', self.trash_folder)
                    self.generate_outliers = config.get('generate_outliers', self.generate_outliers)
                    self.split_normal_folders = config.get('split_normal_folders', self.split_normal_folders)
                    self.delay_normal_creation = config.get('delay_normal_creation', self.delay_normal_creation)
                    self.use_dummy_data = config.get('use_dummy_data', self.use_dummy_data)
        except Exception as e:
            print(f"Failed to load config: {e}")

    def save_config(self):
        """Save configuration to JSON file"""
        try:
            os.makedirs(self.config_dir, exist_ok=True)
            config = {
                'original_base': self.original_base,
                'source_line1': self.source_line1,
                'source_line2': self.source_line2,
                'target_base': self.target_base,
                'move_folder': self.move_folder,
                'trash_folder': self.trash_folder
            }
            with open(self.config_file, 'w', encoding='utf-8') as f:
                json.dump(config, f, indent=2, ensure_ascii=False)
        except Exception as e:
            print(f"Failed to save config: {e}")

    def extract_timestamp(self, name, data_type):
        """Extract timestamp from filename"""
        try:
            if data_type == 'nir':
                # Try YYYYMMDD format first (e.g., run_120251201T140542)
                # Matches 4-digit year, 2-digit month, 2-digit day followed by T and 6-digit time
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

            elif data_type == 'cam':
                # Camera: 20251201_140548_897.bmp → YYYYMMDD_HHMMSS_MS
                match = re.search(r'(\d{8})_(\d{6})_(\d{3})', name)
                if match:
                    date_part = match.group(1)
                    time_part = match.group(2)
                    year = int(date_part[0:4])
                    month = int(date_part[4:6])
                    day = int(date_part[6:8])
                    hour = int(time_part[0:2])
                    minute = int(time_part[2:4])
                    second = int(time_part[4:6])
                    ms = int(match.group(3))
                    return datetime(year, month, day, hour, minute, second, ms * 1000)

        except Exception as e:
            print(f"Error parsing timestamp from {name}: {e}")
        return None

    def scan_all_data(self):
        """Scan all data folders and extract items with timestamps (deprecated - use scan_line1_data)"""
        # For backward compatibility, use Line1 source
        return self.scan_line1_data(self.source_line1)

    def scan_line1_data(self, source_folder):
        """Scan Line1 data folders (normal/*_0, nir, cam1-3)"""
        items = []

        # Scan NIR folder - goes to target/nir1/
        nir_path = os.path.join(source_folder, 'nir')
        if os.path.exists(nir_path):
            for file_name in os.listdir(nir_path):
                if file_name.endswith('.spc') or file_name.endswith('.txt'):
                    timestamp = self.extract_timestamp(file_name, 'nir')
                    if timestamp:
                        items.append({
                            'name': file_name,
                            'source': os.path.join(nir_path, file_name),
                            'relative_path': os.path.join('nir1', file_name),  # nir1 for Line1
                            'timestamp': timestamp,
                            'type': 'nir1_file'
                        })

        # Scan normal folder - only _0 folders for Line1
        normal_path = os.path.join(source_folder, 'normal')
        if os.path.exists(normal_path):
            # Line1 now always goes to normal1 for consistency with user request
            normal_target_root = 'normal1'
            for folder_name in os.listdir(normal_path):
                if folder_name.endswith('_0'):  # Line1 scans only _0 folders
                    folder_path = os.path.join(normal_path, folder_name)
                    if os.path.isdir(folder_path):
                        timestamp = self.extract_timestamp(folder_name, 'normal')
                        if timestamp:
                            items.append({
                                'name': folder_name,
                                'source': folder_path,
                                'relative_path': os.path.join(normal_target_root, folder_name),
                                'timestamp': timestamp,
                                'type': 'normal_folder'
                            })

        # Scan camera folders - cam1-3 only for Line1
        for cam_idx in range(1, 4):  # cam1, cam2, cam3
            cam_path = os.path.join(source_folder, f'cam{cam_idx}')
            if os.path.exists(cam_path):
                for file_name in os.listdir(cam_path):
                    if file_name.endswith('.bmp'):
                        timestamp = self.extract_timestamp(file_name, 'cam')
                        if timestamp:
                            items.append({
                                'name': file_name,
                                'source': os.path.join(cam_path, file_name),
                                'relative_path': os.path.join(f'cam{cam_idx}', file_name),
                                'timestamp': timestamp,
                                'type': 'cam_file'
                            })

        # Sort by timestamp
        items.sort(key=lambda x: x['timestamp'])
        return items
        items.sort(key=lambda x: x['timestamp'])
        return items

    def scan_line2_data(self, source_folder):
        """Scan Line2 data folders (normal/*_0→_1, nir→nir2, cam1-3→cam4-6)"""
        items = []

        # Scan NIR folder - goes to target/nir2/
        nir_path = os.path.join(source_folder, 'nir')
        if os.path.exists(nir_path):
            for file_name in os.listdir(nir_path):
                if file_name.endswith('.spc') or file_name.endswith('.txt'):
                    timestamp = self.extract_timestamp(file_name, 'nir')
                    if timestamp:
                        items.append({
                            'name': file_name,
                            'source': os.path.join(nir_path, file_name),
                            'relative_path': os.path.join('nir2', file_name),  # nir2 folder for Line2
                            'timestamp': timestamp,
                            'type': 'nir2_file'
                        })

        # Scan normal folder - _0 folders are renamed to _1 for Line2
        normal_path = os.path.join(source_folder, 'normal')
        if os.path.exists(normal_path):
            normal_target_root = 'normal2'
            for folder_name in os.listdir(normal_path):
                if folder_name.endswith('_0'):  # Source has _0 folders
                    folder_path = os.path.join(normal_path, folder_name)
                    if os.path.isdir(folder_path):
                        timestamp = self.extract_timestamp(folder_name, 'normal')
                        if timestamp:
                            # Replace _0 with _1 for Line2
                            target_folder_name = folder_name.replace('_0', '_1')
                            items.append({
                                'name': folder_name,
                                'source': folder_path,
                                'relative_path': os.path.join(normal_target_root, target_folder_name),  # _1 folder in target
                                'timestamp': timestamp,
                                'type': 'normal_folder_line2'
                            })

        # Scan camera folders - cam1-3 go to cam4-6 for Line2
        for cam_idx in range(1, 4):  # cam1, cam2, cam3
            cam_path = os.path.join(source_folder, f'cam{cam_idx}')
            if os.path.exists(cam_path):
                for file_name in os.listdir(cam_path):
                    if file_name.endswith('.bmp'):
                        timestamp = self.extract_timestamp(file_name, 'cam')
                        if timestamp:
                            target_cam_idx = cam_idx + 3  # cam1→cam4, cam2→cam5, cam3→cam6
                            items.append({
                                'name': file_name,
                                'source': os.path.join(cam_path, file_name),
                                'relative_path': os.path.join(f'cam{target_cam_idx}', file_name),  # cam4-6 in target
                                'timestamp': timestamp,
                                'type': 'cam_file_line2'
                            })

        # Sort by timestamp
        items.sort(key=lambda x: x['timestamp'])
        return items

    def reset_outlier_state(self):
        with self._outlier_lock:
            self.normal_image_count = 0
            self._normal_widths.clear()
            self._normal_heights.clear()
            self._outlier_warning_logged = False

    def _get_dummy_profile_path(self):
        """Get path to dummy profile JSON file"""
        return self.dummy_profile_file

    def _get_dummy_manifest_path(self):
        """Get path to dummy manifest JSON file"""
        return self.dummy_manifest_file

    def load_dummy_profile(self):
        """Load dummy profile (image sizes) from JSON file"""
        try:
            profile_path = self._get_dummy_profile_path()
            if os.path.exists(profile_path):
                with open(profile_path, 'r', encoding='utf-8') as f:
                    return json.load(f)
            else:
                return None
        except Exception as e:
            print(f"Failed to load dummy profile: {e}")
            return None

    def load_dummy_manifest(self):
        """Load dummy manifest (timestamp-based item list) from JSON file"""
        try:
            manifest_path = self._get_dummy_manifest_path()
            if os.path.exists(manifest_path):
                with open(manifest_path, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    # Convert timestamp strings back to datetime objects
                    for item in data.get('items', []):
                        if 'timestamp' in item and isinstance(item['timestamp'], str):
                            item['timestamp'] = datetime.fromisoformat(item['timestamp'])
                    return data
            else:
                return None
        except Exception as e:
            print(f"Failed to load dummy manifest: {e}")
            return None

    def build_dummy_cache_from_reference(self, reference_path, log_callback=None):
        """Build dummy cache files from reference data folder"""
        try:
            if not os.path.exists(reference_path):
                if log_callback:
                    log_callback(f"ERROR: Reference path does not exist: {reference_path}")
                return False

            if log_callback:
                log_callback(f"Scanning reference data: {reference_path}")

            # Scan reference data to build manifest
            items = self.scan_line1_data(reference_path)
            if not items:
                if log_callback:
                    log_callback("ERROR: No data found in reference folder!")
                return False

            # Extract image sizes from first normal folder
            profile = {
                'normal_stitched_original': {'width': 1920, 'height': 1080},  # Default
                'cam1': {'width': 1920, 'height': 1080},  # Default
                'cam2': {'width': 1920, 'height': 1080},  # Default
                'cam3': {'width': 1920, 'height': 1080}   # Default
            }

            # Find first normal folder and extract stitched_original.png size
            for item in items:
                if item['type'] == 'normal_folder':
                    normal_folder = item['source']
                    stitched_path = os.path.join(normal_folder, 'stitched_original.png')
                    if os.path.exists(stitched_path):
                        size = self._load_image_size(stitched_path, None)
                        if size:
                            profile['normal_stitched_original'] = {'width': size[0], 'height': size[1]}
                            if log_callback:
                                log_callback(f"Found normal image size: {size[0]}x{size[1]}")
                        break

            # Find first cam files and extract sizes
            for item in items:
                if item['type'] == 'cam_file':
                    cam_path = item['source']
                    if os.path.exists(cam_path):
                        size = self._load_image_size(cam_path, None)
                        if size:
                            # Determine which cam (1, 2, or 3)
                            cam_num = None
                            for idx in range(1, 4):
                                if f'cam{idx}' in cam_path:
                                    cam_num = idx
                                    break
                            if cam_num:
                                profile[f'cam{cam_num}'] = {'width': size[0], 'height': size[1]}
                                if log_callback:
                                    log_callback(f"Found cam{cam_num} image size: {size[0]}x{size[1]}")
                        # Only need one sample per cam
                        if all(f'cam{i}' in profile and profile[f'cam{i}']['width'] != 1920 for i in range(1, 4)):
                            break

            # Save profile
            profile_path = self._get_dummy_profile_path()
            with open(profile_path, 'w', encoding='utf-8') as f:
                json.dump(profile, f, indent=2, ensure_ascii=False)
            if log_callback:
                log_callback(f"Saved dummy profile: {profile_path}")

            # Build manifest (convert timestamps to ISO format strings for JSON)
            manifest_items = []
            for item in items:
                manifest_item = {
                    'name': item['name'],
                    'relative_path': item['relative_path'],
                    'timestamp': item['timestamp'].isoformat(),
                    'type': item['type']
                }
                manifest_items.append(manifest_item)

            manifest = {
                'items': manifest_items,
                'reference_path': reference_path,
                'generated_at': datetime.now().isoformat()
            }

            # Save manifest
            manifest_path = self._get_dummy_manifest_path()
            with open(manifest_path, 'w', encoding='utf-8') as f:
                json.dump(manifest, f, indent=2, ensure_ascii=False)
            if log_callback:
                log_callback(f"Saved dummy manifest: {manifest_path} ({len(manifest_items)} items)")

            return True

        except Exception as e:
            if log_callback:
                log_callback(f"ERROR building dummy cache: {e}")
            return False

    def _create_dummy_image(self, image_path, width, height, log_callback=None):
        """Create a black dummy image at specified path"""
        if not PIL_AVAILABLE:
            if log_callback:
                log_callback(f"ERROR: PIL not available, cannot create dummy image: {image_path}")
            return False
        try:
            # Create black image
            img = Image.new('RGB', (width, height), color='black')
            img.save(image_path)
            return True
        except Exception as e:
            if log_callback:
                log_callback(f"ERROR creating dummy image {image_path}: {e}")
            return False

    def _create_dummy_nir_file(self, file_path, log_callback=None):
        """Create a dummy NIR text file"""
        try:
            # Create minimal dummy content
            with open(file_path, 'w', encoding='utf-8') as f:
                f.write("# Dummy NIR data file\n")
            return True
        except Exception as e:
            if log_callback:
                log_callback(f"ERROR creating dummy NIR file {file_path}: {e}")
            return False

    def _clear_folder_contents(self, folder_path, log_callback=None):
        """Remove all contents inside a folder (leave the folder itself)."""
        if not os.path.exists(folder_path):
            return
        try:
            for root, dirs, files in os.walk(folder_path, topdown=False):
                for file_name in files:
                    file_path = os.path.join(root, file_name)
                    try:
                        os.remove(file_path)
                    except Exception as e:
                        if log_callback:
                            log_callback(f"[DUMMY] Failed to delete file {file_path}: {e}")
                for dir_name in dirs:
                    dir_path = os.path.join(root, dir_name)
                    try:
                        os.rmdir(dir_path)
                    except Exception as e:
                        if log_callback:
                            log_callback(f"[DUMMY] Failed to delete dir {dir_path}: {e}")
        except Exception as e:
            if log_callback:
                log_callback(f"[DUMMY] Failed to clear folder {folder_path}: {e}")

    def _load_image_size(self, image_path, log_callback):
        if not PIL_AVAILABLE:
            if log_callback and not self._outlier_warning_logged:
                log_callback("WARN: Pillow not available; outlier generation disabled.")
                self._outlier_warning_logged = True
            return None
        try:
            with Image.open(image_path) as img:
                return img.size
        except Exception as e:
            if log_callback:
                log_callback(f"[OUTLIER] Failed to read image size: {image_path} - {e}")
            return None

    def _calculate_outlier_dimension(self, mean_value, std_value, current_value, shrink=False):
        # Range: 0.2x ~ 2x, but exclude 0.7x ~ 1.3x (too similar to original)
        # Valid ranges: 0.2 ~ 0.7 (shrink) or 1.3 ~ 2.0 (grow)
        if shrink:
            min_ratio = 0.2
            max_ratio = 0.7
        else:
            min_ratio = 1.3
            max_ratio = 2.0

        min_outlier = int(current_value * min_ratio)
        max_outlier = int(current_value * max_ratio)

        if std_value <= 0:
            if shrink:
                outlier_value = current_value * min_ratio  # Use minimum (0.2x)
            else:
                outlier_value = current_value * max_ratio  # Use maximum (2x)
        else:
            if shrink:
                # Shrink: subtract from mean
                outlier_value = mean_value - (self.outlier_z_threshold + 1) * std_value
            else:
                # Grow: add to mean
                outlier_value = mean_value + (self.outlier_z_threshold + 1) * std_value

        # Ensure outlier stays within valid range
        return max(min_outlier, min(int(round(outlier_value)), max_outlier))

    def _resize_image(self, image_path, new_width, new_height, log_callback):
        try:
            with Image.open(image_path) as img:
                resized = img.resize((new_width, new_height), Image.BILINEAR)
                resized.save(image_path)
            return True
        except PermissionError:
            if log_callback:
                log_callback(f"[OUTLIER] Skipped resize (permission denied): {image_path}")
            return False
        except Exception as e:
            if log_callback:
                log_callback(f"[OUTLIER] Failed to resize image: {image_path} - {e}")
            return False

    def maybe_generate_outlier(self, normal_folder_path, log_callback):
        if not self.generate_outliers:
            return

        with self._outlier_lock:
            image_path = os.path.join(normal_folder_path, "stitched_original.png")
            if not os.path.exists(image_path):
                return

            size = self._load_image_size(image_path, log_callback)
            if not size:
                return

            width, height = size
            self.normal_image_count += 1

            history_ready = len(self._normal_widths) >= (self.outlier_min_samples - 1)
            should_attempt = (
                self.normal_image_count >= self.outlier_min_samples
                and history_ready
                and random.random() < 0.5
            )

            if should_attempt:
                # Randomly choose: width or height
                change_width = random.choice([True, False])
                # Randomly choose: grow or shrink
                shrink = random.choice([True, False])

                if change_width:
                    mean_width = statistics.mean(self._normal_widths)
                    std_width = statistics.pstdev(self._normal_widths) if len(self._normal_widths) > 1 else 0.0
                    new_width = self._calculate_outlier_dimension(mean_width, std_width, width, shrink)
                    new_height = height  # Keep original height
                else:
                    mean_height = statistics.mean(self._normal_heights)
                    std_height = statistics.pstdev(self._normal_heights) if len(self._normal_heights) > 1 else 0.0
                    new_width = width  # Keep original width
                    new_height = self._calculate_outlier_dimension(mean_height, std_height, height, shrink)

                if (new_width, new_height) != (width, height):
                    direction = "shrink" if shrink else "grow"
                    axis = "width" if change_width else "height"
                    if self._resize_image(image_path, new_width, new_height, log_callback):
                        if log_callback:
                            log_callback(
                                f"[OUTLIER] {os.path.basename(normal_folder_path)} "
                                f"stitched_original.png {width}x{height} -> {new_width}x{new_height} ({axis} {direction})"
                            )
                        width, height = new_width, new_height

            self._normal_widths.append(width)
            self._normal_heights.append(height)

    def _get_delayed_normal_staging_path(self, target_path):
        relative_path = os.path.relpath(target_path, self.target_base)
        return os.path.join(self.target_base, "_delay_normal", relative_path)

    def _move_folder_contents(self, source_folder, target_folder, log_callback):
        moved = 0
        failed = 0

        if not os.path.exists(source_folder):
            return moved, failed

        try:
            for entry in os.listdir(source_folder):
                source_path = os.path.join(source_folder, entry)
                target_path = os.path.join(target_folder, entry)
                try:
                    os.rename(source_path, target_path)
                    moved += 1
                except Exception as e:
                    failed += 1
                    if log_callback:
                        log_callback(f"[DELAY] Failed to move {entry}: {str(e)}")
        except Exception as e:
            failed += 1
            if log_callback:
                log_callback(f"[DELAY] Failed to read delayed folder: {str(e)}")

        try:
            os.rmdir(source_folder)
        except Exception:
            pass

        return moved, failed

    def _schedule_delayed_normal_creation(self, source_path, target_path, line_label, log_callback):
        delay_seconds = random.randint(10, 40)
        staging_path = self._get_delayed_normal_staging_path(target_path)
        
        # Cleanup staging if exists (leftover from previous run or incomplete reset)
        if os.path.exists(staging_path):
            try:
                if os.path.isdir(staging_path):
                    shutil.rmtree(staging_path)
                else:
                    os.remove(staging_path)
            except Exception as e:
                if log_callback:
                     log_callback(f"[{line_label}] WARN: Cleanup stale staging {staging_path}: {e}")

        os.makedirs(os.path.dirname(staging_path), exist_ok=True)
        os.rename(source_path, staging_path)
        os.makedirs(target_path, exist_ok=True)

        current_sim_id = self.current_simulation_id

        def worker():
            try:
                time.sleep(delay_seconds)
                # Validation: check if simulation is still valid
                if self.current_simulation_id != current_sim_id:
                     if log_callback:
                        log_callback(f"[{line_label}] [DELAY] Cancelled (simulation restarted): {os.path.basename(target_path)}")
                     return

                moved, failed = self._move_folder_contents(staging_path, target_path, log_callback)
                if log_callback:
                    log_callback(
                        f"[{line_label}] [DELAY] {os.path.basename(target_path)} "
                        f"files created after {delay_seconds}s ({moved} moved, {failed} failed)"
                    )
                self.maybe_generate_outlier(target_path, log_callback)
            except Exception as e:
                if log_callback:
                    log_callback(
                        f"[{line_label}] [DELAY] Failed to create files for "
                        f"{os.path.basename(target_path)}: {str(e)}"
                    )

        threading.Thread(target=worker, daemon=True).start()
        return delay_seconds

    def _schedule_delayed_dummy_normal_creation(self, target_path, width, height, line_label, log_callback):
        """Delay creation of dummy normal image to mimic delayed normal creation."""
        delay_seconds = random.randint(10, 40)
        current_sim_id = self.current_simulation_id

        def worker():
            try:
                time.sleep(delay_seconds)
                # Validation: check if simulation is still valid
                if self.current_simulation_id != current_sim_id:
                    if log_callback:
                        log_callback(f"[{line_label}] [DUMMY][DELAY] Cancelled (simulation restarted): {os.path.basename(target_path)}")
                    return

                stitched_path = os.path.join(target_path, "stitched_original.png")
                self._create_dummy_image(stitched_path, width, height, log_callback)
                if log_callback:
                    log_callback(
                        f"[{line_label}] [DUMMY][DELAY] files created after {delay_seconds}s: "
                        f"{os.path.basename(target_path)}"
                    )
            except Exception as e:
                if log_callback:
                    log_callback(f"[{line_label}] [DUMMY][DELAY] Failed to create files for {os.path.basename(target_path)}: {str(e)}")

        threading.Thread(target=worker, daemon=True).start()
        return delay_seconds

    def run_simulation(self, log_callback, progress_callback, complete_callback):
        """Run the simulation by moving files at exact timestamps (deprecated - use run_line1_simulation)"""
        # For backward compatibility, use Line1 simulation
        self.run_line1_simulation(log_callback, progress_callback, complete_callback)

    def run_line1_simulation(self, log_callback, progress_callback, complete_callback):
        """Run Line1 simulation by moving files at exact timestamps"""
        # Clean up old state file from previous simulation
        old_state = self._get_state_file_path()
        if os.path.exists(old_state):
            try:
                os.remove(old_state)
            except Exception:
                pass

        self.is_running = True

        # Initialize simulation state
        self.current_simulation_id = str(uuid.uuid4())
        total_items = 0
        items_moved = 0

        try:
            if not self.target_base:
                log_callback("ERROR: Target folder not set!")
                complete_callback()
                return

            # Dummy mode: skip source check and load from cache
            if self.use_dummy_data:
                log_callback("[LINE1] [DUMMY MODE] Using dummy data cache")
                manifest_data = self.load_dummy_manifest()
                if not manifest_data:
                    log_callback("ERROR: Dummy manifest not found! Please build cache first.")
                    complete_callback()
                    return
                items = manifest_data.get('items', [])
                if not items:
                    log_callback("ERROR: No items in dummy manifest!")
                    complete_callback()
                    return
                log_callback(f"[LINE1] [DUMMY MODE] Loaded {len(items)} items from cache")
            else:
                if not self.source_line1:
                    log_callback("ERROR: Source Line1 folder not set!")
                    complete_callback()
                    return

                self.reset_outlier_state()

                log_callback(f"[LINE1] Scanning source folders: {self.source_line1}")
                items = self.scan_line1_data(self.source_line1)

                if not items:
                    log_callback("ERROR: No Line1 data found in source folders!")
                    complete_callback()
                    return

                log_callback(f"[LINE1] Found {len(items)} items to move")

            # Set total items and update initial state
            total_items = len(items)
            self._update_state("running", 0, 0, self.current_simulation_id)

            self.reset_outlier_state()

            # Get the first timestamp as reference (t0)
            t0 = items[0]['timestamp']
            log_callback(f"Reference time (t0): {t0.strftime('%Y-%m-%d %H:%M:%S')}")

            # Clear previous moved items tracking
            self.moved_items = []

            # Start simulation immediately
            start_real_time = time.time()
            log_callback("Starting simulation...")

            # Move files at exact timestamps
            for idx, item in enumerate(items):
                if not self.is_running:
                    log_callback("Simulation stopped by user")
                    break

                # Calculate delay: item's timestamp - t0
                delay = (item['timestamp'] - t0).total_seconds()

                # Calculate target time in real world
                target_time = start_real_time + delay
                current_time = time.time()

                # Wait until target time
                if current_time < target_time:
                    wait_time = target_time - current_time

                    # Sleep in small increments to allow cancellation
                    while wait_time > 0 and self.is_running:
                        sleep_duration = min(0.1, wait_time)
                        time.sleep(sleep_duration)
                        current_time = time.time()
                        wait_time = target_time - current_time

                    if not self.is_running:
                        log_callback("Simulation stopped by user")
                        break

                # Move file/folder to target (instant rename on same drive) or create dummy
                try:
                    target_path = os.path.normpath(os.path.join(self.target_base, item['relative_path']))

                    # Create parent directory if needed
                    parent_dir = os.path.dirname(target_path)
                    if parent_dir:
                        os.makedirs(parent_dir, exist_ok=True)

                    actual_time = time.time()
                    delay_seconds = None
                    move_time = 0

                    if self.use_dummy_data:
                        # Dummy mode: create dummy files instead of moving
                        if item['type'] == 'normal_folder':
                            # Create normal folder with stitched_original.png
                            os.makedirs(target_path, exist_ok=True)
                            # Ensure only dummy files exist
                            self._clear_folder_contents(target_path, log_callback)
                            profile = self.load_dummy_profile()
                            img_size = {'width': 1920, 'height': 1080}
                            if profile:
                                img_size = profile.get('normal_stitched_original', img_size)
                            if self.delay_normal_creation:
                                delay_seconds = self._schedule_delayed_dummy_normal_creation(
                                    target_path,
                                    img_size['width'],
                                    img_size['height'],
                                    "LINE1",
                                    log_callback
                                )
                            else:
                                stitched_path = os.path.join(target_path, 'stitched_original.png')
                                self._create_dummy_image(stitched_path, img_size['width'], img_size['height'], log_callback)
                            move_time = time.time() - actual_time
                        elif item['type'] == 'cam_file':
                            # Create dummy cam BMP file
                            profile = self.load_dummy_profile()
                            cam_num = None
                            for idx in range(1, 4):
                                if f'cam{idx}' in item['relative_path']:
                                    cam_num = idx
                                    break
                            if profile and cam_num:
                                img_size = profile.get(f'cam{cam_num}', {'width': 1920, 'height': 1080})
                                self._create_dummy_image(target_path, img_size['width'], img_size['height'], log_callback)
                            else:
                                self._create_dummy_image(target_path, 1920, 1080, log_callback)
                            move_time = time.time() - actual_time
                        elif item['type'] == 'nir1_file':
                            # Create dummy NIR text file
                            self._create_dummy_nir_file(target_path, log_callback)
                            move_time = time.time() - actual_time
                        else:
                            move_time = time.time() - actual_time

                        # Track for reset (no source in dummy mode)
                        self.moved_items.append({
                            'target': target_path,
                            'source': None  # No source in dummy mode
                        })
                    else:
                        # Normal mode: move actual files
                        source_path = os.path.normpath(item['source'])
                        if item['type'] == 'normal_folder' and self.delay_normal_creation:
                            delay_seconds = self._schedule_delayed_normal_creation(
                                source_path,
                                target_path,
                                "LINE1",
                                log_callback
                            )
                            move_time = time.time() - actual_time
                        else:
                            os.rename(source_path, target_path)
                            move_time = time.time() - actual_time

                        # Track for reset
                        self.moved_items.append({
                            'target': target_path,
                            'source': source_path
                        })

                        if item['type'] == 'normal_folder' and not self.delay_normal_creation:
                            self.maybe_generate_outlier(target_path, log_callback)

                    elapsed = actual_time - start_real_time
                    current_time_str = datetime.now().strftime('%H:%M:%S')
                    action_word = "Created" if self.use_dummy_data else "Moved"
                    if delay_seconds is None:
                        log_callback(
                            f"✓ [LINE1] [{current_time_str}] [T+{elapsed:.1f}s] "
                            f"{action_word} in {move_time:.4f}s: {item['name']}"
                        )
                    else:
                        log_callback(
                            f"✓ [LINE1] [{current_time_str}] [T+{elapsed:.1f}s] "
                            f"Created folder in {move_time:.4f}s (files delayed {delay_seconds}s): {item['name']}"
                        )

                except Exception as e:
                    log_callback(f"✗ [LINE1] Error moving {item['name']}: {str(e)}")

                # Update progress
                if self.is_running:
                    progress = (idx + 1) / len(items) * 100
                    progress_callback(progress)

                    # Update state after successful item move
                    items_moved = len(self.moved_items)
                    self._update_state("running", progress, items_moved, self.current_simulation_id)

            if self.is_running:
                action_word = "created" if self.use_dummy_data else "moved"
                items_moved = len(self.moved_items)
                log_callback(f"[LINE1] Simulation completed! {items_moved} items {action_word}.")
                self._update_state("completed", 100, items_moved, self.current_simulation_id)

        except Exception as e:
            items_moved = len(self.moved_items)
            progress = (items_moved / total_items * 100) if total_items > 0 else 0
            self._update_state("error", progress, items_moved, self.current_simulation_id)
            log_callback(f"[LINE1] ERROR: {str(e)}")
        finally:
            self.is_running = False
            complete_callback()

    def run_line2_simulation(self, log_callback, progress_callback, complete_callback):
        """Run Line2 simulation by moving files at exact timestamps"""
        # Clean up old state file from previous simulation
        old_state = self._get_state_file_path()
        if os.path.exists(old_state):
            try:
                os.remove(old_state)
            except Exception:
                pass

        self.is_running = True

        # Initialize simulation state
        self.current_simulation_id = str(uuid.uuid4())
        total_items = 0
        items_moved = 0

        try:
            if not self.target_base:
                log_callback("ERROR: Target folder not set!")
                complete_callback()
                return

            # Dummy mode: skip source check and load from cache (convert Line1 manifest to Line2)
            if self.use_dummy_data:
                log_callback("[LINE2] [DUMMY MODE] Using dummy data cache")
                manifest_data = self.load_dummy_manifest()
                if not manifest_data:
                    log_callback("ERROR: Dummy manifest not found! Please build cache first.")
                    complete_callback()
                    return
                # Convert Line1 items to Line2 format
                line1_items = manifest_data.get('items', [])
                items = []
                for item in line1_items:
                    # Convert Line1 to Line2 mapping
                    if item['type'] == 'nir1_file':
                        # nir1 -> nir2
                        new_item = item.copy()
                        new_item['relative_path'] = item['relative_path'].replace('nir1', 'nir2')
                        new_item['type'] = 'nir2_file'
                        items.append(new_item)
                    elif item['type'] == 'normal_folder':
                        # normal1 -> normal2 (or normal if not split)
                        new_item = item.copy()
                        if self.split_normal_folders:
                            new_item['relative_path'] = item['relative_path'].replace('normal1', 'normal2')
                        else:
                            new_item['relative_path'] = item['relative_path'].replace('normal1', 'normal')
                        new_item['name'] = item['name'].replace('_0', '_1')
                        new_item['relative_path'] = new_item['relative_path'].replace('_0', '_1')
                        new_item['type'] = 'normal_folder_line2'
                        items.append(new_item)
                    elif item['type'] == 'cam_file':
                        # cam1-3 -> cam4-6
                        new_item = item.copy()
                        for idx in range(1, 4):
                            if f'cam{idx}' in item['relative_path']:
                                new_item['relative_path'] = item['relative_path'].replace(f'cam{idx}', f'cam{idx+3}')
                                new_item['type'] = 'cam_file_line2'
                                items.append(new_item)
                                break
                if not items:
                    log_callback("ERROR: No items in dummy manifest!")
                    complete_callback()
                    return
                log_callback(f"[LINE2] [DUMMY MODE] Loaded {len(items)} items from cache (converted from Line1)")
            else:
                if not self.source_line2:
                    log_callback("ERROR: Source Line2 folder not set!")
                    complete_callback()
                    return

                self.reset_outlier_state()

                log_callback(f"[LINE2] Scanning source folders: {self.source_line2}")
                items = self.scan_line2_data(self.source_line2)

                if not items:
                    log_callback("ERROR: No Line2 data found in source folders!")
                    complete_callback()
                    return

                log_callback(f"[LINE2] Found {len(items)} items to move")

            # Set total items and update initial state
            total_items = len(items)
            self._update_state("running", 0, 0, self.current_simulation_id)

            self.reset_outlier_state()

            # Get the first timestamp as reference (t0)
            t0 = items[0]['timestamp']
            log_callback(f"Reference time (t0): {t0.strftime('%Y-%m-%d %H:%M:%S')}")

            # Clear previous moved items tracking
            self.moved_items = []

            # Start simulation immediately
            start_real_time = time.time()
            log_callback("[LINE2] Starting simulation...")

            # Move files at exact timestamps
            for idx, item in enumerate(items):
                if not self.is_running:
                    log_callback("[LINE2] Simulation stopped by user")
                    break

                # Calculate delay: item's timestamp - t0
                delay = (item['timestamp'] - t0).total_seconds()

                # Calculate target time in real world
                target_time = start_real_time + delay
                current_time = time.time()

                # Wait until target time
                if current_time < target_time:
                    wait_time = target_time - current_time

                    # Sleep in small increments to allow cancellation
                    while wait_time > 0 and self.is_running:
                        sleep_duration = min(0.1, wait_time)
                        time.sleep(sleep_duration)
                        current_time = time.time()
                        wait_time = target_time - current_time

                    if not self.is_running:
                        log_callback("[LINE2] Simulation stopped by user")
                        break

                # Move file/folder to target (instant rename on same drive) or create dummy
                try:
                    target_path = os.path.normpath(os.path.join(self.target_base, item['relative_path']))

                    # Create parent directory if needed
                    parent_dir = os.path.dirname(target_path)
                    if parent_dir:
                        os.makedirs(parent_dir, exist_ok=True)

                    actual_time = time.time()
                    delay_seconds = None
                    move_time = 0

                    if self.use_dummy_data:
                        # Dummy mode: create dummy files instead of moving
                        if item['type'] == 'normal_folder_line2':
                            # Create normal folder with stitched_original.png
                            os.makedirs(target_path, exist_ok=True)
                            # Ensure only dummy files exist
                            self._clear_folder_contents(target_path, log_callback)
                            profile = self.load_dummy_profile()
                            img_size = {'width': 1920, 'height': 1080}
                            if profile:
                                img_size = profile.get('normal_stitched_original', img_size)
                            if self.delay_normal_creation:
                                delay_seconds = self._schedule_delayed_dummy_normal_creation(
                                    target_path,
                                    img_size['width'],
                                    img_size['height'],
                                    "LINE2",
                                    log_callback
                                )
                            else:
                                stitched_path = os.path.join(target_path, 'stitched_original.png')
                                self._create_dummy_image(stitched_path, img_size['width'], img_size['height'], log_callback)
                            move_time = time.time() - actual_time
                        elif item['type'] == 'cam_file_line2':
                            # Create dummy cam BMP file
                            profile = self.load_dummy_profile()
                            cam_num = None
                            for idx in range(4, 7):
                                if f'cam{idx}' in item['relative_path']:
                                    # Map cam4-6 back to cam1-3 for profile lookup
                                    cam_num = idx - 3
                                    break
                            if profile and cam_num:
                                img_size = profile.get(f'cam{cam_num}', {'width': 1920, 'height': 1080})
                                self._create_dummy_image(target_path, img_size['width'], img_size['height'], log_callback)
                            else:
                                self._create_dummy_image(target_path, 1920, 1080, log_callback)
                            move_time = time.time() - actual_time
                        elif item['type'] == 'nir2_file':
                            # Create dummy NIR text file
                            self._create_dummy_nir_file(target_path, log_callback)
                            move_time = time.time() - actual_time
                        else:
                            move_time = time.time() - actual_time

                        # Track for reset (no source in dummy mode)
                        self.moved_items.append({
                            'target': target_path,
                            'source': None  # No source in dummy mode
                        })
                    else:
                        # Normal mode: move actual files
                        source_path = os.path.normpath(item['source'])
                        if item['type'] == 'normal_folder_line2' and self.delay_normal_creation:
                            delay_seconds = self._schedule_delayed_normal_creation(
                                source_path,
                                target_path,
                                "LINE2",
                                log_callback
                            )
                            move_time = time.time() - actual_time
                        else:
                            os.rename(source_path, target_path)
                            move_time = time.time() - actual_time

                        # Track for reset
                        self.moved_items.append({
                            'target': target_path,
                            'source': source_path
                        })

                        if item['type'] == 'normal_folder_line2' and not self.delay_normal_creation:
                            self.maybe_generate_outlier(target_path, log_callback)

                    elapsed = actual_time - start_real_time
                    current_time_str = datetime.now().strftime('%H:%M:%S')
                    action_word = "Created" if self.use_dummy_data else "Moved"
                    if delay_seconds is None:
                        log_callback(
                            f"✓ [LINE2] [{current_time_str}] [T+{elapsed:.1f}s] "
                            f"{action_word} in {move_time:.4f}s: {item['name']}"
                        )
                    else:
                        log_callback(
                            f"✓ [LINE2] [{current_time_str}] [T+{elapsed:.1f}s] "
                            f"Created folder in {move_time:.4f}s (files delayed {delay_seconds}s): {item['name']}"
                        )

                except Exception as e:
                    log_callback(f"✗ [LINE2] Error moving {item['name']}: {str(e)}")

                # Update progress
                if self.is_running:
                    progress = (idx + 1) / len(items) * 100
                    progress_callback(progress)

                    # Update state after successful item move
                    items_moved = len(self.moved_items)
                    self._update_state("running", progress, items_moved, self.current_simulation_id)

            if self.is_running:
                action_word = "created" if self.use_dummy_data else "moved"
                items_moved = len(self.moved_items)
                log_callback(f"[LINE2] Simulation completed! {items_moved} items {action_word}.")
                self._update_state("completed", 100, items_moved, self.current_simulation_id)

        except Exception as e:
            items_moved = len(self.moved_items)
            progress = (items_moved / total_items * 100) if total_items > 0 else 0
            self._update_state("error", progress, items_moved, self.current_simulation_id)
            log_callback(f"[LINE2] ERROR: {str(e)}")
        finally:
            self.is_running = False
            complete_callback()

    def start(self, log_callback, progress_callback, complete_callback):
        """Start simulation in a separate thread (deprecated - use start_line1)"""
        # For backward compatibility, use Line1 simulation
        self.start_line1(log_callback, progress_callback, complete_callback)

    def start_line1(self, log_callback, progress_callback, complete_callback):
        """Start Line1 simulation in a separate thread"""
        if self.is_running:
            log_callback("Simulation already running!")
            return

            return

        self.is_running = True
        self.current_simulation_id = str(uuid.uuid4())
        self.simulation_thread = threading.Thread(
            target=self.run_line1_simulation,
            args=(log_callback, progress_callback, complete_callback),
            daemon=True
        )
        self.simulation_thread.start()

    def start_line2(self, log_callback, progress_callback, complete_callback):
        """Start Line2 simulation in a separate thread"""
        if self.is_running:
            log_callback("Simulation already running!")
            return

            return

        self.is_running = True
        self.current_simulation_id = str(uuid.uuid4())
        self.simulation_thread = threading.Thread(
            target=self.run_line2_simulation,
            args=(log_callback, progress_callback, complete_callback),
            daemon=True
        )
        self.simulation_thread.start()

    def stop(self):
        """Stop the running simulation"""
        self.is_running = False

    def _get_state_file_path(self):
        """Get the path to the simulation state file.

        Returns:
            Path to simulation_state.json in config_dir
        """
        return os.path.join(self.config_dir, "simulation_state.json")

    def _update_state(self, status, progress, items_created, simulation_id):
        """Write simulation state to file.

        Args:
            status: Current status (running, completed, idle, error)
            progress: Progress percentage (0-100)
            items_created: Number of items created/moved
            simulation_id: UUID of the current simulation
        """
        state = {
            "status": status,
            "progress": round(progress, 1),
            "items_created": items_created,
            "simulation_id": simulation_id,
            "last_activity": datetime.now().isoformat()
        }
        try:
            with open(self._get_state_file_path(), 'w') as f:
                json.dump(state, f, indent=2)
        except Exception as e:
            print(f"[Warning] Failed to write state file: {e}")

    def _load_state(self):
        """Load simulation state from file.

        Returns:
            State dictionary or None if file doesn't exist or is invalid
        """
        state_path = self._get_state_file_path()
        if not os.path.exists(state_path):
            return None
        try:
            with open(state_path, 'r') as f:
                return json.load(f)
        except Exception:
            return None

    def get_status(self):
        """Get current simulation status.

        Returns:
            Dictionary with status, progress, items_created, simulation_id, last_activity
            Returns idle state if no state file exists
        """
        state = self._load_state()
        if state is None:
            return {
                "status": "idle",
                "progress": 0,
                "items_created": 0,
                "simulation_id": None,
                "last_activity": None
            }
        return state

    def cleanup_test_data(self, target_path):
        """Remove all test data from target directory.

        Args:
            target_path: Path to the test data directory to clean

        Returns:
            True if cleanup successful, False otherwise
        """
        import glob
        try:
            if not os.path.exists(target_path):
                print(f"[CLI] Cleanup: Target path does not exist: {target_path}")
                return True

            # Count files before deletion
            file_count = 0
            for root, dirs, files in os.walk(target_path):
                file_count += len(files)

            # Remove all files and directories
            for root, dirs, files in os.walk(target_path, topdown=False):
                for name in files:
                    file_path = os.path.join(root, name)
                    try:
                        os.remove(file_path)
                    except Exception as e:
                        print(f"[CLI] Cleanup: Failed to remove {file_path}: {e}")

                for name in dirs:
                    dir_path = os.path.join(root, name)
                    try:
                        os.rmdir(dir_path)
                    except Exception as e:
                        print(f"[CLI] Cleanup: Failed to remove directory {dir_path}: {e}")

            # Remove the root directory
            try:
                os.rmdir(target_path)
                print(f"[CLI] ✓ Cleanup: Removed {file_count} files from {target_path}")
            except Exception as e:
                print(f"[CLI] Cleanup: Failed to remove root directory: {e}")

            return True

        except Exception as e:
            print(f"[CLI] ✗ Cleanup error: {e}")
            return False

    def get_chronoview_config_path(self):
        """Get the ChronoView config.json path based on platform.

        Returns:
            Path to config.json or None if not found
        """
        if sys.platform == 'win32':
            # Windows: %LOCALAPPDATA%\prische\ChronoView\config.json
            appdata = os.environ.get('LOCALAPPDATA', '')
            if appdata:
                return os.path.join(appdata, 'prische', 'ChronoView', 'config.json')
        elif sys.platform == 'darwin':
            # macOS: ~/Library/Application Support/prische/ChronoView/config.json
            home = os.path.expanduser('~')
            return os.path.join(home, 'Library', 'Application Support', 'prische', 'ChronoView', 'config.json')
        else:
            # Linux: ~/.local/share/prische/ChronoView/config.json
            home = os.path.expanduser('~')
            xdg_data = os.environ.get('XDG_DATA_HOME', os.path.join(home, '.local', 'share'))
            return os.path.join(xdg_data, 'prische', 'ChronoView', 'config.json')
        return None

    def read_chronoview_config(self):
        """Read ChronoView configuration and extract watch folder paths.

        Returns:
            dict with keys: 'normal1', 'normal2', 'camera1-6', 'nir1', 'nir2', 'output'
            Returns None if config not found or invalid
        """
        config_path = self.get_chronoview_config_path()
        if not config_path or not os.path.exists(config_path):
            return None

        try:
            with open(config_path, 'r', encoding='utf-8') as f:
                config = json.load(f)

            # Extract from matchingSettings (primary source - actual paths used by app)
            matching_settings = config.get('matchingSettings', {})

            result = {
                'normal1': matching_settings.get('normal1Path', ''),
                'normal2': matching_settings.get('normal2Path', ''),
                'camera1': matching_settings.get('camera1Path', ''),
                'camera2': matching_settings.get('camera2Path', ''),
                'camera3': matching_settings.get('camera3Path', ''),
                'camera4': matching_settings.get('camera4Path', ''),
                'camera5': matching_settings.get('camera5Path', ''),
                'camera6': matching_settings.get('camera6Path', ''),
                'nir1': matching_settings.get('nir1Path', ''),
                'nir2': matching_settings.get('nir2Path', ''),
                'output': matching_settings.get('outputPath', ''),
                'base_path': config.get('basePath', ''),
            }

            # Fallback to folderPaths for any missing values
            folder_paths = config.get('folderPaths', {})
            if folder_paths:
                # Map folderPaths keys to result keys
                key_mapping = {
                    'normal': 'normal1',
                    'normal2': 'normal2',
                    'nir': 'nir1',
                    'nir2': 'nir2',
                }
                for fp_key, result_key in key_mapping.items():
                    if fp_key in folder_paths and not result.get(result_key):
                        result[result_key] = folder_paths[fp_key]

            return result
        except json.JSONDecodeError as e:
            print(f"[CLI] Failed to parse ChronoView config: {e}")
            return None
        except Exception as e:
            print(f"[CLI] Failed to read ChronoView config: {e}")
            return None

    def get_watch_path_for_line(self, line='line1', cv_config=None):
        """Get the appropriate watch path for a given line.

        Args:
            line: 'line1' or 'line2'
            cv_config: ChronoView config dict (if None, will read)

        Returns:
            Path to use as target for simulation, or None.
            Returns the parent directory of watch paths (the date folder),
            since the simulator adds subdirectories like 'normal1', 'cam1', etc.
        """
        if cv_config is None:
            cv_config = self.read_chronoview_config()

        if not cv_config:
            return None

        # First, try to get base_path directly (this is the date folder)
        path = cv_config.get('base_path')
        if path:
            return path

        # If base_path is not set, derive it from specific watch paths
        # All watch paths share the same parent (date folder)
        # For Line 1: prefer Normal1, fallback to Camera1, then NIR1
        if line == 'line1':
            specific_path = (cv_config.get('normal1') or
                            cv_config.get('camera1') or
                            cv_config.get('nir1'))
        # For Line 2: prefer Normal2, fallback to Camera4, then NIR2
        else:
            specific_path = (cv_config.get('normal2') or
                            cv_config.get('camera4') or
                            cv_config.get('nir2'))

        # Extract parent directory from the specific path
        # e.g., "...\20260120\normal1" -> "...\20260120"
        if specific_path:
            parent_dir = os.path.dirname(specific_path)
            if parent_dir:
                return parent_dir

        return None

    def show_chronoview_config(self):
        """Display ChronoView configuration paths.

        Returns:
            True if config was found and displayed, False otherwise
        """
        cv_config = self.read_chronoview_config()
        if not cv_config:
            print("[CLI] ChronoView config not found or invalid.")
            print("[CLI] Expected location:")
            print(f"     {self.get_chronoview_config_path()}")
            return False

        print("[CLI] ChronoView Configuration:")
        print("[CLI] " + "=" * 50)
        print(f"[CLI] Config file: {self.get_chronoview_config_path()}")
        print("[CLI]")
        print(f"[CLI] Line 1 Paths:")
        print(f"[CLI]   Normal1:  {cv_config.get('normal1') or '(not set)'}")
        print(f"[CLI]   Camera1:  {cv_config.get('camera1') or '(not set)'}")
        print(f"[CLI]   Camera2:  {cv_config.get('camera2') or '(not set)'}")
        print(f"[CLI]   Camera3:  {cv_config.get('camera3') or '(not set)'}")
        print(f"[CLI]   NIR1:     {cv_config.get('nir1') or '(not set)'}")
        print("[CLI]")
        print(f"[CLI] Line 2 Paths:")
        print(f"[CLI]   Normal2:  {cv_config.get('normal2') or '(not set)'}")
        print(f"[CLI]   Camera4:  {cv_config.get('camera4') or '(not set)'}")
        print(f"[CLI]   Camera5:  {cv_config.get('camera5') or '(not set)'}")
        print(f"[CLI]   Camera6:  {cv_config.get('camera6') or '(not set)'}")
        print(f"[CLI]   NIR2:     {cv_config.get('nir2') or '(not set)'}")
        print("[CLI]")
        print(f"[CLI] Common Paths:")
        print(f"[CLI]   Base:     {cv_config.get('base_path') or '(not set)'}")
        print(f"[CLI]   Output:   {cv_config.get('output') or '(not set)'}")
        print("[CLI] " + "=" * 50)

        # Show recommended target for each line
        line1_target = self.get_watch_path_for_line('line1', cv_config)
        line2_target = self.get_watch_path_for_line('line2', cv_config)
        print("[CLI]")
        print("[CLI] Recommended targets for simulation:")
        print(f"[CLI]   Line 1: {line1_target or '(no path configured)'}")
        print(f"[CLI]   Line 2: {line2_target or '(no path configured)'}")

        return True

    def run_cli(self, args):
        """Run simulation in CLI mode (no GUI).

        Args:
            args: argparse.Namespace with CLI arguments
                - mode: 'dummy' or 'real'
                - line: 'line1' or 'line2'
                - target: target folder path
                - source: source folder path (for real mode)
                - read_config: use ChronoView config to auto-detect target
                - show_config: show ChronoView config and exit
        """
        # Handle --show-config: just display config and exit
        if hasattr(args, 'show_config') and args.show_config:
            return 0 if self.show_chronoview_config() else 1

        # Determine target path
        if hasattr(args, 'read_config') and args.read_config:
            # Auto-detect target from ChronoView config
            detected_target = self.get_watch_path_for_line(args.line)
            if not detected_target:
                print(f"[CLI] Error: Could not detect watch path for {args.line} from ChronoView config")
                print("[CLI] Run with --show-config to see available paths")
                return 1
            self.target_base = detected_target
            print(f"[CLI] Auto-detected target from ChronoView config: {self.target_base}")
        elif hasattr(args, 'target') and args.target:
            self.target_base = args.target
        else:
            print("[CLI] Error: --target is required (or use --read-config to auto-detect)")
            return 1

        self.use_dummy_data = (args.mode == 'dummy')

        # Handle delayed creation flag
        if hasattr(args, 'delayed') and args.delayed:
            self.delay_normal_creation = True
            print("[CLI] Delayed file creation enabled (10-40s random delay)")

        if args.mode == 'real':
            if not args.source:
                print("[CLI] Error: --source is required for real mode")
                return 1
            if args.line == 'line1':
                self.source_line1 = args.source
            else:
                self.source_line2 = args.source
        else:
            # Dummy mode doesn't need source
            print(f"[CLI] Dummy data mode: generating files to {self.target_base}")

        # Setup callbacks for CLI output
        # Note: progress_callback signature in existing code: progress_callback(progress_percent)
        # Note: complete_callback signature in existing code: complete_callback() (no args)
        simulation_result = {"success": False, "message": ""}

        def log_callback(message):
            print(f"[CLI] {message}")

        def progress_callback(progress):
            # progress is a number 0-100
            print(f"[CLI] Progress: {progress}%")

        def complete_callback():
            # Just mark that callback was called
            simulation_result["callback_called"] = True

        # Start simulation
        line = args.line
        print(f"[CLI] Starting {line} simulation in {args.mode} mode...")
        print(f"[CLI] Target: {self.target_base}")

        if line == 'line1':
            self.run_line1_simulation(log_callback, progress_callback, complete_callback)
        else:
            self.run_line2_simulation(log_callback, progress_callback, complete_callback)

        # Wait for completion
        if self.simulation_thread:
            self.simulation_thread.join()

        # Determine success: if callback was called, simulation completed
        if simulation_result.get("callback_called", False):
            simulation_result["success"] = True
            simulation_result["message"] = "Simulation completed"
        else:
            simulation_result["success"] = False
            simulation_result["message"] = "Simulation did not complete"

        # Report final result
        if simulation_result["success"]:
            print(f"[CLI] ✓ {simulation_result['message']}")
        else:
            print(f"[CLI] ✗ {simulation_result['message']}")

        return 0 if simulation_result["success"] else 1


class SimulatorGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Data Simulator - Timestamp-based Move")
        self.root.geometry("900x700")

        self.simulator = DataSimulator()
        self.setup_ui()

    def setup_ui(self):
        # Main container
        main_frame = ttk.Frame(self.root, padding="10")
        main_frame.grid(row=0, column=0, sticky=(tk.W, tk.E, tk.N, tk.S))

        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(0, weight=1)
        main_frame.columnconfigure(1, weight=1)
        main_frame.rowconfigure(7, weight=1)  # Log area should expand

        # Original folder
        ttk.Label(main_frame, text="Original Folder:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.original_entry = ttk.Entry(main_frame, width=60)
        self.original_entry.insert(0, self.simulator.original_base)
        self.original_entry.grid(row=0, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_original).grid(row=0, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_original_folder).grid(row=0, column=3, pady=5, padx=(5, 0))

        # Source Line1 folder
        ttk.Label(main_frame, text="Source Line1 Folder:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.source_line1_entry = ttk.Entry(main_frame, width=60)
        self.source_line1_entry.insert(0, self.simulator.source_line1)
        self.source_line1_entry.grid(row=1, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_source_line1).grid(row=1, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_source_line1_folder).grid(row=1, column=3, pady=5, padx=(5, 0))

        # Source Line2 folder
        ttk.Label(main_frame, text="Source Line2 Folder:").grid(row=2, column=0, sticky=tk.W, pady=5)
        self.source_line2_entry = ttk.Entry(main_frame, width=60)
        self.source_line2_entry.insert(0, self.simulator.source_line2)
        self.source_line2_entry.grid(row=2, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_source_line2).grid(row=2, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_source_line2_folder).grid(row=2, column=3, pady=5, padx=(5, 0))

        # Target folder
        ttk.Label(main_frame, text="Target Folder:").grid(row=3, column=0, sticky=tk.W, pady=5)
        self.target_entry = ttk.Entry(main_frame, width=60)
        self.target_entry.insert(0, self.simulator.target_base)
        self.target_entry.grid(row=3, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_target).grid(row=3, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_target_folder).grid(row=3, column=3, pady=5, padx=(5, 0))

        # Move folder
        ttk.Label(main_frame, text="Move Folder:").grid(row=4, column=0, sticky=tk.W, pady=5)
        self.move_entry = ttk.Entry(main_frame, width=60)
        self.move_entry.insert(0, self.simulator.move_folder)
        self.move_entry.grid(row=4, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_move).grid(row=4, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_move_folder).grid(row=4, column=3, pady=5, padx=(5, 0))

        # Trash folder
        ttk.Label(main_frame, text="Trash Folder:").grid(row=5, column=0, sticky=tk.W, pady=5)
        self.trash_entry = ttk.Entry(main_frame, width=60)
        self.trash_entry.insert(0, self.simulator.trash_folder)
        self.trash_entry.grid(row=5, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_trash).grid(row=5, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_trash_folder).grid(row=5, column=3, pady=5, padx=(5, 0))

        # Outlier generation + normal folder split + delayed creation
        options_frame = ttk.Frame(main_frame)
        options_frame.grid(row=6, column=1, columnspan=2, sticky=tk.W, pady=5, padx=5)

        self.split_normal_var = tk.BooleanVar(value=self.simulator.split_normal_folders)
        self.split_normal_check = ttk.Checkbutton(
            options_frame,
            text="normal 폴더 분리",
            variable=self.split_normal_var,
            command=self.toggle_split_normal
        )
        self.split_normal_check.pack(side=tk.LEFT)

        self.delay_var = tk.BooleanVar(value=self.simulator.delay_normal_creation)
        self.delay_check = ttk.Checkbutton(
            options_frame,
            text="지연 생성",
            variable=self.delay_var,
            command=self.toggle_delay_creation
        )
        self.delay_check.pack(side=tk.LEFT, padx=(15, 0))

        self.outlier_var = tk.BooleanVar(value=self.simulator.generate_outliers)
        self.outlier_check = ttk.Checkbutton(
            options_frame,
            text="이상치 생성 (stitched_original.png)",
            variable=self.outlier_var,
            command=self.toggle_outlier
        )
        self.outlier_check.pack(side=tk.LEFT, padx=(15, 0))

        self.dummy_var = tk.BooleanVar(value=self.simulator.use_dummy_data)
        self.dummy_check = ttk.Checkbutton(
            options_frame,
            text="더미 데이터 생성",
            variable=self.dummy_var,
            command=self.toggle_dummy
        )
        self.dummy_check.pack(side=tk.LEFT, padx=(15, 0))

        # Log area
        ttk.Label(main_frame, text="Log:").grid(row=7, column=0, sticky=(tk.W, tk.N), pady=5)
        self.log_text = scrolledtext.ScrolledText(main_frame, width=80, height=25, wrap=tk.WORD)
        self.log_text.grid(row=7, column=1, columnspan=2, sticky=(tk.W, tk.E, tk.N, tk.S), pady=5)

        # Progress bar
        ttk.Label(main_frame, text="Progress:").grid(row=8, column=0, sticky=tk.W, pady=5)
        self.progress = ttk.Progressbar(main_frame, length=400, mode='determinate')
        self.progress.grid(row=8, column=1, columnspan=2, sticky=(tk.W, tk.E), pady=5)

        # Control buttons
        button_frame = ttk.Frame(main_frame)
        button_frame.grid(row=9, column=0, columnspan=3, pady=10)

        self.start_line1_button = ttk.Button(button_frame, text="Start Line1 Simulation", command=self.start_line1_simulation)
        self.start_line1_button.pack(side=tk.LEFT, padx=5)

        self.start_line2_button = ttk.Button(button_frame, text="Start Line2 Simulation", command=self.start_line2_simulation)
        self.start_line2_button.pack(side=tk.LEFT, padx=5)

        self.stop_button = ttk.Button(button_frame, text="Stop", command=self.stop_simulation, state=tk.DISABLED)
        self.stop_button.pack(side=tk.LEFT, padx=5)

        self.reset_button = ttk.Button(button_frame, text="Reset", command=self.reset_target)
        self.reset_button.pack(side=tk.LEFT, padx=5)

    def browse_original(self):
        folder = filedialog.askdirectory(initialdir=self.original_entry.get() or os.getcwd())
        if folder:
            self.original_entry.delete(0, tk.END)
            self.original_entry.insert(0, folder)
            self.simulator.original_base = folder
            self.simulator.save_config()

    def browse_source_line1(self):
        folder = filedialog.askdirectory(initialdir=self.source_line1_entry.get() or os.getcwd())
        if folder:
            self.source_line1_entry.delete(0, tk.END)
            self.source_line1_entry.insert(0, folder)
            self.simulator.source_line1 = folder
            self.simulator.save_config()

    def browse_source_line2(self):
        folder = filedialog.askdirectory(initialdir=self.source_line2_entry.get() or os.getcwd())
        if folder:
            self.source_line2_entry.delete(0, tk.END)
            self.source_line2_entry.insert(0, folder)
            self.simulator.source_line2 = folder
            self.simulator.save_config()

    def browse_target(self):
        folder = filedialog.askdirectory(initialdir=self.target_entry.get() or os.getcwd())
        if folder:
            self.target_entry.delete(0, tk.END)
            self.target_entry.insert(0, folder)
            self.simulator.target_base = folder
            self.simulator.save_config()

    def browse_move(self):
        folder = filedialog.askdirectory(initialdir=self.move_entry.get() or os.getcwd())
        if folder:
            self.move_entry.delete(0, tk.END)
            self.move_entry.insert(0, folder)
            self.simulator.move_folder = folder
            self.simulator.save_config()

    def browse_trash(self):
        folder = filedialog.askdirectory(initialdir=self.trash_entry.get() or os.getcwd())
        if folder:
            self.trash_entry.delete(0, tk.END)
            self.trash_entry.insert(0, folder)
            self.simulator.trash_folder = folder
            self.simulator.save_config()

    def toggle_outlier(self):
        self.simulator.generate_outliers = bool(self.outlier_var.get())
        self.simulator.save_config()

    def toggle_delay_creation(self):
        self.simulator.delay_normal_creation = bool(self.delay_var.get())
        self.simulator.save_config()

    def toggle_split_normal(self):
        self.simulator.split_normal_folders = bool(self.split_normal_var.get())
        self.simulator.save_config()

    def toggle_dummy(self):
        self.simulator.use_dummy_data = bool(self.dummy_var.get())
        self.simulator.save_config()

    def open_original_folder(self):
        """Open original folder in Windows Explorer"""
        folder_path = self.original_entry.get()
        if folder_path and os.path.exists(folder_path):
            try:
                # Windows: explorer 명령어로 폴더 열기
                subprocess.Popen(['explorer', os.path.normpath(folder_path)])
            except Exception as e:
                messagebox.showerror("Error", f"Failed to open folder:\n{str(e)}")
        else:
            messagebox.showwarning("Warning", "Original folder does not exist or is not set.")

    def open_source_line1_folder(self):
        """Open source line1 folder in Windows Explorer"""
        folder_path = self.source_line1_entry.get()
        if folder_path and os.path.exists(folder_path):
            try:
                subprocess.Popen(['explorer', os.path.normpath(folder_path)])
            except Exception as e:
                messagebox.showerror("Error", f"Failed to open folder:\n{str(e)}")
        else:
            messagebox.showwarning("Warning", "Source Line1 folder does not exist or is not set.")

    def open_source_line2_folder(self):
        """Open source line2 folder in Windows Explorer"""
        folder_path = self.source_line2_entry.get()
        if folder_path and os.path.exists(folder_path):
            try:
                subprocess.Popen(['explorer', os.path.normpath(folder_path)])
            except Exception as e:
                messagebox.showerror("Error", f"Failed to open folder:\n{str(e)}")
        else:
            messagebox.showwarning("Warning", "Source Line2 folder does not exist or is not set.")

    def open_target_folder(self):
        """Open target folder in Windows Explorer"""
        folder_path = self.target_entry.get()
        if folder_path and os.path.exists(folder_path):
            try:
                # Windows: explorer 명령어로 폴더 열기
                subprocess.Popen(['explorer', os.path.normpath(folder_path)])
            except Exception as e:
                messagebox.showerror("Error", f"Failed to open folder:\n{str(e)}")
        else:
            messagebox.showwarning("Warning", "Target folder does not exist or is not set.")

    def open_move_folder(self):
        """Open move folder in Windows Explorer"""
        folder_path = self.move_entry.get()
        if folder_path and os.path.exists(folder_path):
            try:
                # Windows: explorer 명령어로 폴더 열기
                subprocess.Popen(['explorer', os.path.normpath(folder_path)])
            except Exception as e:
                messagebox.showerror("Error", f"Failed to open folder:\n{str(e)}")
        else:
            messagebox.showwarning("Warning", "Move folder does not exist or is not set.")

    def open_trash_folder(self):
        """Open trash folder in Windows Explorer"""
        folder_path = self.trash_entry.get()
        if folder_path and os.path.exists(folder_path):
            try:
                # Windows: explorer 명령어로 폴더 열기
                subprocess.Popen(['explorer', os.path.normpath(folder_path)])
            except Exception as e:
                messagebox.showerror("Error", f"Failed to open folder:\n{str(e)}")
        else:
            messagebox.showwarning("Warning", "Trash folder does not exist or is not set.")

    def log(self, message):
        """Add message to log (thread-safe)"""
        def append():
            self.log_text.insert(tk.END, message + "\n")
            self.log_text.see(tk.END)
        self.root.after(0, append)

    def update_progress(self, value):
        """Update progress bar (thread-safe)"""
        def update():
            self.progress['value'] = value
        self.root.after(0, update)

    def start_line1_simulation(self):
        # Update paths from entry fields
        self.simulator.original_base = self.original_entry.get()
        self.simulator.source_line1 = self.source_line1_entry.get()
        self.simulator.target_base = self.target_entry.get()
        self.simulator.move_folder = self.move_entry.get()
        self.simulator.trash_folder = self.trash_entry.get()
        self.simulator.generate_outliers = bool(self.outlier_var.get())
        self.simulator.delay_normal_creation = bool(self.delay_var.get())
        self.simulator.split_normal_folders = bool(self.split_normal_var.get())
        self.simulator.use_dummy_data = bool(self.dummy_var.get())

        # Save configuration
        self.simulator.save_config()

        if not self.simulator.target_base:
            messagebox.showerror("Error", "Please set target folder!")
            return

        # In dummy mode, source folder is not required
        if not self.simulator.use_dummy_data and not self.simulator.source_line1:
            messagebox.showerror("Error", "Please set Source Line1 folder!")
            return

        # Clear log
        self.log_text.delete(1.0, tk.END)
        self.progress['value'] = 0

        # Update buttons
        self.start_line1_button.configure(state=tk.DISABLED)
        self.start_line2_button.configure(state=tk.DISABLED)
        self.stop_button.configure(state=tk.NORMAL)
        self.reset_button.configure(state=tk.DISABLED)

        # Start Line1 simulation
        self.simulator.start_line1(
            log_callback=self.log,
            progress_callback=self.update_progress,
            complete_callback=self.simulation_complete
        )

    def start_line2_simulation(self):
        # Update paths from entry fields
        self.simulator.original_base = self.original_entry.get()
        self.simulator.source_line2 = self.source_line2_entry.get()
        self.simulator.target_base = self.target_entry.get()
        self.simulator.move_folder = self.move_entry.get()
        self.simulator.trash_folder = self.trash_entry.get()
        self.simulator.generate_outliers = bool(self.outlier_var.get())
        self.simulator.delay_normal_creation = bool(self.delay_var.get())
        self.simulator.split_normal_folders = bool(self.split_normal_var.get())
        self.simulator.use_dummy_data = bool(self.dummy_var.get())

        # Save configuration
        self.simulator.save_config()
        if not self.simulator.target_base:
            messagebox.showerror("Error", "Please set target folder!")
            return

        # In dummy mode, source folder is not required
        if not self.simulator.use_dummy_data and not self.simulator.source_line2:
            messagebox.showerror("Error", "Please set Source Line2 folder!")
            return

        # Clear log
        self.log_text.delete(1.0, tk.END)
        self.progress['value'] = 0

        # Update buttons
        self.start_line1_button.configure(state=tk.DISABLED)
        self.start_line2_button.configure(state=tk.DISABLED)
        self.stop_button.configure(state=tk.NORMAL)
        self.reset_button.configure(state=tk.DISABLED)

        # Start Line2 simulation
        self.simulator.start_line2(
            log_callback=self.log,
            progress_callback=self.update_progress,
            complete_callback=self.simulation_complete
        )

    def stop_simulation(self):
        self.log("Stopping simulation...")
        self.simulator.stop()

    def simulation_complete(self):
        """Called when simulation completes or stops"""
        def update_ui():
            self.start_line1_button.configure(state=tk.NORMAL)
            self.start_line2_button.configure(state=tk.NORMAL)
            self.stop_button.configure(state=tk.DISABLED)
            self.reset_button.configure(state=tk.NORMAL)
        self.root.after(0, update_ui)

    def scan_target_folder(self):
        """Scan target folder and create reset mapping to source folder"""
        if not self.simulator.target_base:
            self.log("Error: Target folder is not set")
            return []

        if not os.path.exists(self.simulator.target_base):
            self.log(f"Error: Target folder does not exist: {self.simulator.target_base}")
            return []

        if not self.simulator.source_base:
            self.log("Error: Source folder is not set")
            return []

        if not os.path.exists(self.simulator.source_base):
            self.log(f"Error: Source folder does not exist: {self.simulator.source_base}")
            return []

        self.log(f"Scanning target: {self.simulator.target_base}")
        self.log(f"Will restore to source: {self.simulator.source_base}")

        items = []

        # Scan NIR files in target (nir, nir1, nir2)
        for nir_dir in ['nir', 'nir1', 'nir2']:
            target_nir = os.path.join(self.simulator.target_base, nir_dir)
            source_nir = os.path.join(self.simulator.source_base, 'nir')
            if os.path.exists(target_nir):
                for file_name in os.listdir(target_nir):
                    if file_name.endswith('.spc') or file_name.endswith('.txt'):
                        target_path = os.path.join(target_nir, file_name)
                        source_path = os.path.join(source_nir, file_name)
                        items.append({
                            'target': target_path,
                            'source': source_path
                        })
                        self.log(f"Found {nir_dir} file: {file_name}")

        # Scan normal folders in target (normal, normal1, normal2)
        for norm_dir in ['normal', 'normal1', 'normal2']:
            target_normal = os.path.join(self.simulator.target_base, norm_dir)
            source_normal = os.path.join(self.simulator.source_base, 'normal')
            if os.path.exists(target_normal):
                for folder_name in os.listdir(target_normal):
                    folder_path = os.path.join(target_normal, folder_name)
                    if os.path.isdir(folder_path):
                        source_path = os.path.join(source_normal, folder_name)
                        items.append({
                            'target': folder_path,
                            'source': source_path
                        })
                        self.log(f"Found {norm_dir} folder: {folder_name}")

        # Scan camera files in target
        for cam_idx in range(1, 7):
            target_cam = os.path.join(self.simulator.target_base, f'cam{cam_idx}')
            source_cam = os.path.join(self.simulator.source_base, f'cam{cam_idx}')
            if os.path.exists(target_cam):
                for file_name in os.listdir(target_cam):
                    if file_name.endswith('.bmp'):
                        target_path = os.path.join(target_cam, file_name)
                        source_path = os.path.join(source_cam, file_name)
                        items.append({
                            'target': target_path,
                            'source': source_path
                        })
                        self.log(f"Found cam{cam_idx} file: {file_name}")

        self.log(f"Total items found in target: {len(items)}")
        return items


    def copy_folder_recursive(self, src, dst):
        """Recursively copy folder contents, skipping duplicates"""
        copied = 0
        skipped = 0

        try:
            # Ensure destination exists
            os.makedirs(dst, exist_ok=True)

            for root, dirs, files in os.walk(src):
                # Calculate relative path from source
                rel_path = os.path.relpath(root, src)
                if rel_path == '.':
                    dest_root = dst
                else:
                    dest_root = os.path.join(dst, rel_path)

                # Create directories
                for dir_name in dirs:
                    dest_dir = os.path.join(dest_root, dir_name)
                    os.makedirs(dest_dir, exist_ok=True)

                # Copy files
                for file_name in files:
                    src_file = os.path.join(root, file_name)
                    dest_file = os.path.join(dest_root, file_name)

                    # Skip if destination file already exists
                    if os.path.exists(dest_file):
                        self.log(f"Skipped (exists): {os.path.relpath(dest_file, dst)}")
                        skipped += 1
                        continue

                    try:
                        # Use copy2 to preserve metadata
                        import shutil
                        shutil.copy2(src_file, dest_file)
                        self.log(f"✓ Copied: {os.path.relpath(dest_file, dst)}")
                        copied += 1
                    except Exception as e:
                        self.log(f"✗ Error copying {file_name}: {str(e)}")

        except Exception as e:
            self.log(f"Error in copy_folder_recursive: {str(e)}")

        return copied, skipped

    def delete_folder_contents(self, folder_path):
        """Delete all contents of a folder"""
        deleted = 0
        failed = 0

        if not os.path.exists(folder_path):
            return deleted, failed

        try:
            for root, dirs, files in os.walk(folder_path, topdown=False):
                # Delete files first
                for file_name in files:
                    file_path = os.path.join(root, file_name)
                    try:
                        os.remove(file_path)
                        deleted += 1
                        self.log(f"✓ Deleted: {os.path.relpath(file_path, folder_path)}")
                    except Exception as e:
                        failed += 1
                        self.log(f"✗ Error deleting file {file_name}: {str(e)}")

                # Delete directories
                for dir_name in dirs:
                    dir_path = os.path.join(root, dir_name)
                    try:
                        os.rmdir(dir_path)
                        deleted += 1
                        self.log(f"✓ Deleted dir: {os.path.relpath(dir_path, folder_path)}")
                    except Exception as e:
                        failed += 1
                        self.log(f"✗ Error deleting dir {dir_name}: {str(e)}")

        except Exception as e:
            self.log(f"Error deleting folder contents: {str(e)}")

        return deleted, failed

    def reset_target(self):
        # Update paths from entry fields
        self.simulator.original_base = self.original_entry.get()
        self.simulator.source_line1 = self.source_line1_entry.get()
        self.simulator.source_line2 = self.source_line2_entry.get()
        self.simulator.target_base = self.target_entry.get()
        self.simulator.move_folder = self.move_entry.get()
        self.simulator.trash_folder = self.trash_entry.get()
        self.simulator.split_normal_folders = bool(self.split_normal_var.get())
        self.simulator.delay_normal_creation = bool(self.delay_var.get())
        self.simulator.use_dummy_data = bool(self.dummy_var.get())
        # Invalidate any pending delayed dummy workers and stop running simulation
        self.simulator.is_running = False
        self.simulator.current_simulation_id = str(uuid.uuid4())

        # Save configuration
        self.simulator.save_config()

        # Validate required paths (skip original/source in dummy mode)
        if not self.simulator.use_dummy_data:
            if not self.simulator.original_base:
                messagebox.showerror("Error", "Please set original folder!")
                return

            if not self.simulator.source_line1:
                messagebox.showerror("Error", "Please set Source Line1 folder!")
                return

            if not self.simulator.source_line2:
                messagebox.showerror("Error", "Please set Source Line2 folder!")
                return

            if not os.path.exists(self.simulator.original_base):
                messagebox.showerror("Error", f"Original folder does not exist:\n{self.simulator.original_base}")
                return

        # Log current paths
        self.log("="*60)
        if not self.simulator.use_dummy_data:
            self.log(f"Original folder: {self.simulator.original_base}")
            self.log(f"Source Line1 folder: {self.simulator.source_line1}")
            self.log(f"Source Line2 folder: {self.simulator.source_line2}")
        else:
            self.log("Dummy mode: original/source folders are ignored")
        self.log(f"Target folder: {self.simulator.target_base}")
        self.log(f"Move folder: {self.simulator.move_folder}")
        self.log(f"Trash folder: {self.simulator.trash_folder}")
        self.log("="*60)

        # Confirm reset operation
        cleanup_scope = "Target, Move, and Trash folders"
        if self.simulator.split_normal_folders:
            cleanup_scope = "Target normal/normal1/normal2/cam1-6, Move, and Trash folders"
        if self.simulator.use_dummy_data:
            result = messagebox.askyesno(
                "Confirm Reset",
                "Dummy mode reset will:\n"
                f"- Delete contents of {cleanup_scope}\n\n"
                "Continue?"
            )
        else:
            result = messagebox.askyesno(
                "Confirm Reset",
                "This will:\n"
                "1. Copy contents from Original folder to Source Line1 folder (skip duplicates)\n"
                "2. Copy contents from Original folder to Source Line2 folder (skip duplicates)\n"
                f"3. Delete contents of {cleanup_scope}\n\n"
                "Continue?"
            )

        if not result:
            return

        # Clear log and reset progress
        self.log_text.delete(1.0, tk.END)
        self.progress['value'] = 0

        try:
            total_steps = 5  # Copy original->line1, copy original->line2, delete target, delete move, delete trash
            current_step = 0
            if self.simulator.use_dummy_data:
                total_steps = 3  # delete target, delete move, delete trash
            else:
                # Step 1: Copy original folder to source line1 folder
                self.log("Step 1: Copying original folder to Source Line1...")
                copied, skipped = self.copy_folder_recursive(self.simulator.original_base, self.simulator.source_line1)
                self.log(f"Line1 copy complete: {copied} copied, {skipped} skipped")
                current_step += 1
                self.update_progress((current_step / total_steps) * 100)

                # Step 2: Copy original folder to source line2 folder
                self.log("Step 2: Copying original folder to Source Line2...")
                copied, skipped = self.copy_folder_recursive(self.simulator.original_base, self.simulator.source_line2)
                self.log(f"Line2 copy complete: {copied} copied, {skipped} skipped")
                current_step += 1
                self.update_progress((current_step / total_steps) * 100)

            # Step 3: Delete target folder contents
            if self.simulator.target_base:
                self.log("Step 3: Deleting target normal/normal1/normal2, nir/nir1/nir2, and cam1-6 contents...")
                
                # List of subfolders to clean up
                subfolders = [
                    'normal', 'normal1', 'normal2',
                    'nir', 'nir1', 'nir2',
                    '_delay_normal'
                ]
                for i in range(1, 7):
                    subfolders.append(f'cam{i}')
                
                for sub in subfolders:
                    sub_path = os.path.join(self.simulator.target_base, sub)
                    if os.path.exists(sub_path):
                        if sub == '_delay_normal':
                            try:
                                import shutil
                                shutil.rmtree(sub_path)
                                self.log(f"Target {sub} cleanup: Removed tree")
                            except Exception as e:
                                self.log(f"Target {sub} cleanup failed: {e}")
                        else:
                            deleted, failed = self.delete_folder_contents(sub_path)
                            if deleted > 0 or failed > 0:
                                self.log(f"Target {sub} cleanup: {deleted} deleted, {failed} failed")
            else:
                self.log("Step 3: Target folder not set, skipping")
            current_step += 1
            self.update_progress((current_step / total_steps) * 100)

            # Step 4: Delete move folder contents
            if self.simulator.move_folder:
                self.log("Step 4: Deleting move folder contents...")
                deleted, failed = self.delete_folder_contents(self.simulator.move_folder)
                self.log(f"Move cleanup: {deleted} deleted, {failed} failed")
            else:
                self.log("Step 4: Move folder not set, skipping")
            current_step += 1
            self.update_progress((current_step / total_steps) * 100)

            # Step 5: Delete trash folder contents
            if self.simulator.trash_folder:
                self.log("Step 5: Deleting trash folder contents...")
                deleted, failed = self.delete_folder_contents(self.simulator.trash_folder)
                self.log(f"Trash cleanup: {deleted} deleted, {failed} failed")
            else:
                self.log("Step 5: Trash folder not set, skipping")
            current_step += 1
            self.update_progress((current_step / total_steps) * 100)

            self.log("Reset operation completed!")
            messagebox.showinfo("Reset Complete", "Reset operation completed successfully!")

        except Exception as e:
            self.log(f"Error during reset: {str(e)}")
            messagebox.showerror("Error", f"Failed to reset:\n{str(e)}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description='Data Simulator - Timestamp-based File Move Tool',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # GUI mode (default)
  python data_simulator.py

  # CLI mode - Show ChronoView config paths
  python data_simulator.py --cli --show-config

  # CLI mode - Auto-detect target from ChronoView config
  python data_simulator.py --cli --read-config --mode dummy --line line1

  # CLI mode - Manual target path
  python data_simulator.py --cli --mode dummy --target /path/to/target --line line1

  # CLI mode - Real data move with auto-detect
  python data_simulator.py --cli --read-config --mode real --source /path/to/source --line line1

  # CLI mode - With cleanup
  python data_simulator.py --cli --read-config --mode dummy --line line1 --cleanup
        """
    )

    parser.add_argument('--cli', action='store_true',
                        help='Run in CLI mode (no GUI)')
    parser.add_argument('--mode', choices=['dummy', 'real'], default='dummy',
                        help='Simulation mode: dummy (generate black images) or real (move existing files)')
    parser.add_argument('--line', choices=['line1', 'line2'], default='line1',
                        help='Line to simulate: line1 or line2')
    parser.add_argument('--source',
                        help='Source folder path (required for real mode)')
    parser.add_argument('--target',
                        help='Target folder path (optional with --read-config)')
    parser.add_argument('--read-config', action='store_true',
                        help='Auto-detect target path from ChronoView config')
    parser.add_argument('--show-config', action='store_true',
                        help='Show ChronoView config paths and exit')
    parser.add_argument('--cleanup', action='store_true',
                        help='Clean up test data after simulation')
    parser.add_argument('--delayed', action='store_true',
                        help='Enable delayed file creation (10-40s random delay)')

    args = parser.parse_args()

    if args.cli:
        # CLI mode
        if not GUI_AVAILABLE:
            print("[CLI] Note: GUI not available, running in CLI mode only")
        simulator = DataSimulator()
        exit_code = simulator.run_cli(args)

        # Cleanup if requested (use the actual target used during simulation)
        if args.cleanup and exit_code == 0:
            target = simulator.target_base  # Use the actual target path (may be auto-detected)
            print(f"[CLI] Cleaning up test data: {target}")
            simulator.cleanup_test_data(target)

        sys.exit(exit_code)
    else:
        # GUI mode (default)
        if not GUI_AVAILABLE:
            print("Error: tkinter is not available. Please install tkinter or use --cli mode.")
            sys.exit(1)
        root = tk.Tk()
        app = SimulatorGUI(root)
        root.mainloop()
