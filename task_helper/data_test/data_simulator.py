"""
Data Simulator - Timestamp-based File Move Tool

Simulates real-time data generation by moving files from source to target
at precise timestamp intervals for ChronoView testing.
"""

import os
import re
import time
import json
import threading
import subprocess
from datetime import datetime
import tkinter as tk
from tkinter import ttk, filedialog, scrolledtext, messagebox


class DataSimulator:
    def __init__(self):
        self.config_dir = r"C:\workspace\seaweed\gui_kiro\task_helper\data_test"
        self.config_file = os.path.join(self.config_dir, "simulator_config.json")

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

        # Scan NIR folder - goes to target/nir/
        nir_path = os.path.join(source_folder, 'nir')
        if os.path.exists(nir_path):
            for file_name in os.listdir(nir_path):
                if file_name.endswith('.spc') or file_name.endswith('.txt'):
                    timestamp = self.extract_timestamp(file_name, 'nir')
                    if timestamp:
                        items.append({
                            'name': file_name,
                            'source': os.path.join(nir_path, file_name),
                            'relative_path': os.path.join('nir', file_name),
                            'timestamp': timestamp,
                            'type': 'nir_file'
                        })

        # Scan normal folder - only _0 folders for Line1
        normal_path = os.path.join(source_folder, 'normal')
        if os.path.exists(normal_path):
            for folder_name in os.listdir(normal_path):
                if folder_name.endswith('_0'):  # Line1 scans only _0 folders
                    folder_path = os.path.join(normal_path, folder_name)
                    if os.path.isdir(folder_path):
                        timestamp = self.extract_timestamp(folder_name, 'normal')
                        if timestamp:
                            items.append({
                                'name': folder_name,
                                'source': folder_path,
                                'relative_path': os.path.join('normal', folder_name),
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
                                'relative_path': os.path.join('normal', target_folder_name),  # _1 folder in target
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

    def run_simulation(self, log_callback, progress_callback, complete_callback):
        """Run the simulation by moving files at exact timestamps (deprecated - use run_line1_simulation)"""
        # For backward compatibility, use Line1 simulation
        self.run_line1_simulation(log_callback, progress_callback, complete_callback)

    def run_line1_simulation(self, log_callback, progress_callback, complete_callback):
        """Run Line1 simulation by moving files at exact timestamps"""
        try:
            if not self.target_base:
                log_callback("ERROR: Target folder not set!")
                complete_callback()
                return

            if not self.source_line1:
                log_callback("ERROR: Source Line1 folder not set!")
                complete_callback()
                return

            log_callback(f"[LINE1] Scanning source folders: {self.source_line1}")
            items = self.scan_line1_data(self.source_line1)

            if not items:
                log_callback("ERROR: No Line1 data found in source folders!")
                complete_callback()
                return

            log_callback(f"[LINE1] Found {len(items)} items to move")

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

                # Move file/folder to target (instant rename on same drive)
                try:
                    source_path = os.path.normpath(item['source'])
                    target_path = os.path.normpath(os.path.join(self.target_base, item['relative_path']))

                    # Create parent directory if needed
                    parent_dir = os.path.dirname(target_path)
                    if parent_dir:
                        os.makedirs(parent_dir, exist_ok=True)

                    # Move using os.rename (fastest on same drive)
                    actual_time = time.time()
                    os.rename(source_path, target_path)
                    move_time = time.time() - actual_time

                    # Track for reset
                    self.moved_items.append({
                        'target': target_path,
                        'source': source_path
                    })

                    elapsed = actual_time - start_real_time
                    current_time_str = datetime.now().strftime('%H:%M:%S')
                    log_callback(f"✓ [LINE1] [{current_time_str}] [T+{elapsed:.1f}s] Moved in {move_time:.4f}s: {item['name']}")

                except Exception as e:
                    log_callback(f"✗ [LINE1] Error moving {item['name']}: {str(e)}")

                # Update progress
                if self.is_running:
                    progress = (idx + 1) / len(items) * 100
                    progress_callback(progress)

            if self.is_running:
                log_callback(f"[LINE1] Simulation completed! {len(self.moved_items)} items moved.")

        except Exception as e:
            log_callback(f"[LINE1] ERROR: {str(e)}")
        finally:
            self.is_running = False
            complete_callback()

    def run_line2_simulation(self, log_callback, progress_callback, complete_callback):
        """Run Line2 simulation by moving files at exact timestamps"""
        try:
            if not self.target_base:
                log_callback("ERROR: Target folder not set!")
                complete_callback()
                return

            if not self.source_line2:
                log_callback("ERROR: Source Line2 folder not set!")
                complete_callback()
                return

            log_callback(f"[LINE2] Scanning source folders: {self.source_line2}")
            items = self.scan_line2_data(self.source_line2)

            if not items:
                log_callback("ERROR: No Line2 data found in source folders!")
                complete_callback()
                return

            log_callback(f"[LINE2] Found {len(items)} items to move")

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

                # Move file/folder to target (instant rename on same drive)
                try:
                    source_path = os.path.normpath(item['source'])
                    target_path = os.path.normpath(os.path.join(self.target_base, item['relative_path']))

                    # Create parent directory if needed
                    parent_dir = os.path.dirname(target_path)
                    if parent_dir:
                        os.makedirs(parent_dir, exist_ok=True)

                    # Move using os.rename (fastest on same drive)
                    actual_time = time.time()
                    os.rename(source_path, target_path)
                    move_time = time.time() - actual_time

                    # Track for reset
                    self.moved_items.append({
                        'target': target_path,
                        'source': source_path
                    })

                    elapsed = actual_time - start_real_time
                    current_time_str = datetime.now().strftime('%H:%M:%S')
                    log_callback(f"✓ [LINE2] [{current_time_str}] [T+{elapsed:.1f}s] Moved in {move_time:.4f}s: {item['name']}")

                except Exception as e:
                    log_callback(f"✗ [LINE2] Error moving {item['name']}: {str(e)}")

                # Update progress
                if self.is_running:
                    progress = (idx + 1) / len(items) * 100
                    progress_callback(progress)

            if self.is_running:
                log_callback(f"[LINE2] Simulation completed! {len(self.moved_items)} items moved.")

        except Exception as e:
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

        self.is_running = True
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

        self.is_running = True
        self.simulation_thread = threading.Thread(
            target=self.run_line2_simulation,
            args=(log_callback, progress_callback, complete_callback),
            daemon=True
        )
        self.simulation_thread.start()

    def stop(self):
        """Stop the running simulation"""
        self.is_running = False


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
        main_frame.rowconfigure(6, weight=1)  # Log area should expand

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

        # Log area
        ttk.Label(main_frame, text="Log:").grid(row=6, column=0, sticky=(tk.W, tk.N), pady=5)
        self.log_text = scrolledtext.ScrolledText(main_frame, width=80, height=25, wrap=tk.WORD)
        self.log_text.grid(row=6, column=1, columnspan=2, sticky=(tk.W, tk.E, tk.N, tk.S), pady=5)

        # Progress bar
        ttk.Label(main_frame, text="Progress:").grid(row=7, column=0, sticky=tk.W, pady=5)
        self.progress = ttk.Progressbar(main_frame, length=400, mode='determinate')
        self.progress.grid(row=7, column=1, columnspan=2, sticky=(tk.W, tk.E), pady=5)

        # Control buttons
        button_frame = ttk.Frame(main_frame)
        button_frame.grid(row=8, column=0, columnspan=3, pady=10)

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

        # Save configuration
        self.simulator.save_config()

        if not self.simulator.target_base:
            messagebox.showerror("Error", "Please set target folder!")
            return

        if not self.simulator.source_line1:
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

        # Save configuration
        self.simulator.save_config()

        if not self.simulator.target_base:
            messagebox.showerror("Error", "Please set target folder!")
            return

        if not self.simulator.source_line2:
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

        # Scan NIR files in target
        target_nir = os.path.join(self.simulator.target_base, 'nir')
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
                    self.log(f"Found NIR file: {file_name}")

        # Scan normal folders in target
        target_normal = os.path.join(self.simulator.target_base, 'normal')
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
                    self.log(f"Found normal folder: {folder_name}")

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

        # Save configuration
        self.simulator.save_config()

        # Validate required paths
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
        self.log(f"Original folder: {self.simulator.original_base}")
        self.log(f"Source Line1 folder: {self.simulator.source_line1}")
        self.log(f"Source Line2 folder: {self.simulator.source_line2}")
        self.log(f"Target folder: {self.simulator.target_base}")
        self.log(f"Move folder: {self.simulator.move_folder}")
        self.log(f"Trash folder: {self.simulator.trash_folder}")
        self.log("="*60)

        # Confirm reset operation
        result = messagebox.askyesno(
            "Confirm Reset",
            "This will:\n"
            "1. Copy contents from Original folder to Source Line1 folder (skip duplicates)\n"
            "2. Copy contents from Original folder to Source Line2 folder (skip duplicates)\n"
            "3. Delete contents of Target, Move, and Trash folders\n\n"
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
                self.log("Step 3: Deleting target folder contents...")
                deleted, failed = self.delete_folder_contents(self.simulator.target_base)
                self.log(f"Target cleanup: {deleted} deleted, {failed} failed")
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
    root = tk.Tk()
    app = SimulatorGUI(root)
    root.mainloop()
