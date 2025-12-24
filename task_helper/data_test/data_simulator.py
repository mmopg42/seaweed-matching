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
        self.source_base = r"Z:\윤태경\seaweed\program\data\2025_A046"
        self.target_base = ""
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
                    self.source_base = config.get('source_base', self.source_base)
                    self.target_base = config.get('target_base', self.target_base)
        except Exception as e:
            print(f"Failed to load config: {e}")

    def save_config(self):
        """Save configuration to JSON file"""
        try:
            os.makedirs(self.config_dir, exist_ok=True)
            config = {
                'source_base': self.source_base,
                'target_base': self.target_base
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
        """Scan all data folders and extract items with timestamps"""
        items = []

        # Scan NIR folder
        nir_path = os.path.join(self.source_base, 'nir')
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

        # Scan normal folder
        normal_path = os.path.join(self.source_base, 'normal')
        if os.path.exists(normal_path):
            for folder_name in os.listdir(normal_path):
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

        # Scan camera folders
        for cam_idx in range(1, 7):
            cam_path = os.path.join(self.source_base, f'cam{cam_idx}')
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

    def run_simulation(self, log_callback, progress_callback, complete_callback):
        """Run the simulation by moving files at exact timestamps"""
        try:
            if not self.target_base:
                log_callback("ERROR: Target folder not set!")
                complete_callback()
                return

            log_callback(f"Scanning source folders: {self.source_base}")
            items = self.scan_all_data()

            if not items:
                log_callback("ERROR: No data found in source folders!")
                complete_callback()
                return

            log_callback(f"Found {len(items)} items to move")

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
                    log_callback(f"✓ [T+{elapsed:.1f}s] Moved in {move_time:.4f}s: {item['name']}")

                except Exception as e:
                    log_callback(f"✗ Error moving {item['name']}: {str(e)}")

                # Update progress
                if self.is_running:
                    progress = (idx + 1) / len(items) * 100
                    progress_callback(progress)

            if self.is_running:
                log_callback(f"Simulation completed! {len(self.moved_items)} items moved.")
                log_callback("Use 'Reset' to move files back to original location.")

        except Exception as e:
            log_callback(f"ERROR: {str(e)}")
        finally:
            self.is_running = False
            complete_callback()

    def start(self, log_callback, progress_callback, complete_callback):
        """Start simulation in a separate thread"""
        if self.is_running:
            log_callback("Simulation already running!")
            return

        self.is_running = True
        self.simulation_thread = threading.Thread(
            target=self.run_simulation,
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
        main_frame.rowconfigure(2, weight=1)

        # Source folder
        ttk.Label(main_frame, text="Source Folder:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.source_entry = ttk.Entry(main_frame, width=60)
        self.source_entry.insert(0, self.simulator.source_base)
        self.source_entry.grid(row=0, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_source).grid(row=0, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_source_folder).grid(row=0, column=3, pady=5, padx=(5, 0))

        # Target folder
        ttk.Label(main_frame, text="Target Folder:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.target_entry = ttk.Entry(main_frame, width=60)
        self.target_entry.insert(0, self.simulator.target_base)
        self.target_entry.grid(row=1, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_target).grid(row=1, column=2, pady=5)
        ttk.Button(main_frame, text="Open", command=self.open_target_folder).grid(row=1, column=3, pady=5, padx=(5, 0))

        # Log area
        ttk.Label(main_frame, text="Log:").grid(row=2, column=0, sticky=(tk.W, tk.N), pady=5)
        self.log_text = scrolledtext.ScrolledText(main_frame, width=80, height=25, wrap=tk.WORD)
        self.log_text.grid(row=2, column=1, columnspan=2, sticky=(tk.W, tk.E, tk.N, tk.S), pady=5)

        # Progress bar
        ttk.Label(main_frame, text="Progress:").grid(row=3, column=0, sticky=tk.W, pady=5)
        self.progress = ttk.Progressbar(main_frame, length=400, mode='determinate')
        self.progress.grid(row=3, column=1, columnspan=2, sticky=(tk.W, tk.E), pady=5)

        # Control buttons
        button_frame = ttk.Frame(main_frame)
        button_frame.grid(row=4, column=0, columnspan=3, pady=10)

        self.start_button = ttk.Button(button_frame, text="Start Simulation", command=self.start_simulation)
        self.start_button.pack(side=tk.LEFT, padx=5)

        self.stop_button = ttk.Button(button_frame, text="Stop", command=self.stop_simulation, state=tk.DISABLED)
        self.stop_button.pack(side=tk.LEFT, padx=5)

        self.reset_button = ttk.Button(button_frame, text="Reset (Move Back)", command=self.reset_target)
        self.reset_button.pack(side=tk.LEFT, padx=5)

    def browse_source(self):
        folder = filedialog.askdirectory(initialdir=self.source_entry.get())
        if folder:
            self.source_entry.delete(0, tk.END)
            self.source_entry.insert(0, folder)
            self.simulator.source_base = folder
            self.simulator.save_config()

    def browse_target(self):
        folder = filedialog.askdirectory(initialdir=self.target_entry.get() or os.getcwd())
        if folder:
            self.target_entry.delete(0, tk.END)
            self.target_entry.insert(0, folder)
            self.simulator.target_base = folder
            self.simulator.save_config()

    def open_source_folder(self):
        """Open source folder in Windows Explorer"""
        folder_path = self.source_entry.get()
        if folder_path and os.path.exists(folder_path):
            try:
                # Windows: explorer 명령어로 폴더 열기
                subprocess.Popen(['explorer', os.path.normpath(folder_path)])
            except Exception as e:
                messagebox.showerror("Error", f"Failed to open folder:\n{str(e)}")
        else:
            messagebox.showwarning("Warning", "Source folder does not exist or is not set.")

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

    def start_simulation(self):
        # Update paths from entry fields
        self.simulator.source_base = self.source_entry.get()
        self.simulator.target_base = self.target_entry.get()

        if not self.simulator.target_base:
            messagebox.showerror("Error", "Please set target folder!")
            return

        # Clear log
        self.log_text.delete(1.0, tk.END)
        self.progress['value'] = 0

        # Update buttons
        self.start_button.configure(state=tk.DISABLED)
        self.stop_button.configure(state=tk.NORMAL)
        self.reset_button.configure(state=tk.DISABLED)

        # Start simulation
        self.simulator.start(
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
            self.start_button.configure(state=tk.NORMAL)
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

    def reset_target(self):
        # Update paths from entry fields
        self.simulator.source_base = self.source_entry.get()
        self.simulator.target_base = self.target_entry.get()

        # Log current paths
        self.log("="*60)
        self.log(f"Source folder: {self.simulator.source_base}")
        self.log(f"Target folder: {self.simulator.target_base}")
        self.log("="*60)

        # Always scan target folder directly (ignore tracked items)
        self.log("Scanning target folder...")
        items_to_reset = self.scan_target_folder()

        if not items_to_reset:
            messagebox.showinfo("Info", "No files found in target folder to reset.")
            self.simulator.moved_items = []
            return

        result = messagebox.askyesno(
            "Confirm Reset",
            f"Found {len(items_to_reset)} items at target location.\n"
            f"Move them back to original location?\n\n"
            "This will restore the source folder."
        )

        if result:
            # Reset progress bar
            self.progress['value'] = 0

            try:
                moved_back = 0
                failed = 0
                failed_items = []  # Track failed items for retry

                # Move files back to original location in reverse order
                for idx, item in enumerate(reversed(items_to_reset)):
                    try:
                        target_path = item['target']
                        source_path = item['source']

                        # Ensure source parent directory exists
                        source_parent = os.path.dirname(source_path)
                        if source_parent:
                            os.makedirs(source_parent, exist_ok=True)

                        # Move back using os.rename (instant)
                        os.rename(target_path, source_path)
                        moved_back += 1
                        self.log(f"✓ Restored: {os.path.basename(source_path)}")

                    except PermissionError as e:
                        failed += 1
                        failed_items.append(item)
                        self.log(f"✗ File in use: {os.path.basename(source_path)}")

                    except Exception as e:
                        failed += 1
                        failed_items.append(item)
                        self.log(f"✗ Error restoring {os.path.basename(source_path)}: {str(e)}")

                    # Update progress
                    progress = (idx + 1) / len(items_to_reset) * 100
                    self.update_progress(progress)

                # Update moved_items to only include failed items
                self.simulator.moved_items = failed_items

                # Show result
                if failed > 0:
                    self.log(f"Reset partial: {moved_back} items restored, {failed} failed")
                    messagebox.showwarning(
                        "Reset Partially Complete",
                        f"Moved {moved_back} items back to original location\n"
                        f"Failed: {failed} (possibly files in use)\n\n"
                        "Click 'Reset' again to retry the remaining files."
                    )
                else:
                    self.log(f"Reset complete: {moved_back} items restored")
                    messagebox.showinfo("Reset Complete",
                                      f"All {moved_back} items moved back to original location")

            except Exception as e:
                self.log(f"Error during reset: {str(e)}")
                messagebox.showerror("Error", f"Failed to reset:\n{str(e)}")


if __name__ == "__main__":
    root = tk.Tk()
    app = SimulatorGUI(root)
    root.mainloop()
