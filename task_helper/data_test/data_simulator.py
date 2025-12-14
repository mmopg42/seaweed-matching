import os
import re
import shutil
import time
import threading
import json
from datetime import datetime, timedelta
from pathlib import Path
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

    def extract_timestamp_nir(self, filename):
        """Extract timestamp from NIR files: run_120251201T140542.spc -> 140542"""
        match = re.search(r'T(\d{6})', filename)
        if match:
            time_str = match.group(1)
            hours = int(time_str[0:2])
            minutes = int(time_str[2:4])
            seconds = int(time_str[4:6])
            return timedelta(hours=hours, minutes=minutes, seconds=seconds)
        return None

    def extract_timestamp_normal(self, foldername):
        """Extract timestamp from normal folders: C251201T140543_0 -> 140543"""
        match = re.search(r'T(\d{6})', foldername)
        if match:
            time_str = match.group(1)
            hours = int(time_str[0:2])
            minutes = int(time_str[2:4])
            seconds = int(time_str[4:6])
            return timedelta(hours=hours, minutes=minutes, seconds=seconds)
        return None

    def extract_timestamp_camera(self, filename):
        """Extract timestamp from camera files: 20251201_140548_897.bmp -> 140548"""
        match = re.search(r'^\d+_(\d{6})_', filename)
        if match:
            time_str = match.group(1)
            hours = int(time_str[0:2])
            minutes = int(time_str[2:4])
            seconds = int(time_str[4:6])
            return timedelta(hours=hours, minutes=minutes, seconds=seconds)
        return None

    def scan_all_data(self):
        """Scan all source folders and collect file/folder information with timestamps"""
        items = []

        # Scan NIR folder
        nir_path = os.path.join(self.source_base, "nir")
        if os.path.exists(nir_path):
            for filename in os.listdir(nir_path):
                filepath = os.path.join(nir_path, filename)
                if os.path.isfile(filepath):
                    timestamp = self.extract_timestamp_nir(filename)
                    if timestamp:
                        items.append({
                            'type': 'nir_file',
                            'source': filepath,
                            'relative_path': os.path.join('nir', filename),
                            'timestamp': timestamp,
                            'name': filename
                        })

        # Scan Normal folder
        normal_path = os.path.join(self.source_base, "normal")
        if os.path.exists(normal_path):
            for foldername in os.listdir(normal_path):
                folderpath = os.path.join(normal_path, foldername)
                if os.path.isdir(folderpath):
                    timestamp = self.extract_timestamp_normal(foldername)
                    if timestamp:
                        items.append({
                            'type': 'normal_folder',
                            'source': folderpath,
                            'relative_path': os.path.join('normal', foldername),
                            'timestamp': timestamp,
                            'name': foldername
                        })

        # Scan Camera folders (cam1, cam2, cam3)
        for cam_num in [1, 2, 3]:
            cam_path = os.path.join(self.source_base, f"cam{cam_num}")
            if os.path.exists(cam_path):
                for filename in os.listdir(cam_path):
                    filepath = os.path.join(cam_path, filename)
                    if os.path.isfile(filepath):
                        timestamp = self.extract_timestamp_camera(filename)
                        if timestamp:
                            items.append({
                                'type': f'cam{cam_num}_file',
                                'source': filepath,
                                'relative_path': os.path.join(f'cam{cam_num}', filename),
                                'timestamp': timestamp,
                                'name': filename
                            })

        # Sort by timestamp
        items.sort(key=lambda x: x['timestamp'])

        return items

    def copy_item(self, item):
        """Copy file or folder to target location.

        For network drive compatibility, uses actual file copy instead of links.
        """
        # Normalize paths to use Windows backslashes
        target_path = os.path.normpath(os.path.join(self.target_base, item['relative_path']))
        source_path = os.path.normpath(item['source'])

        # Remove existing item if present
        if os.path.exists(target_path):
            if os.path.isdir(target_path):
                shutil.rmtree(target_path)
            else:
                os.remove(target_path)

        # Create parent directory if needed
        parent_dir = os.path.dirname(target_path)
        if parent_dir:
            os.makedirs(parent_dir, exist_ok=True)

        if item['type'] == 'normal_folder':
            # For folders, copy entire directory tree
            shutil.copytree(source_path, target_path)
        else:
            # For files, copy with metadata preservation
            shutil.copy2(source_path, target_path)

    def run_simulation(self, log_callback, progress_callback, complete_callback):
        """Run the simulation by moving files at exact timestamps.

        Strategy: Move files from source to target at scheduled times (same drive = instant rename).
        Files can be moved back to original location for reset.
        """
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
            log_callback(f"Reference time (t0): {t0}")

            # Clear previous moved items tracking
            self.moved_items = []

            # Start simulation immediately
            start_real_time = time.time()
            log_callback("Starting scheduled file appearance simulation...")

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


class SimulatorGUI:
    def __init__(self, root):
        self.root = root
        self.root.title("Data Simulator - Timestamp-based Copy")
        self.root.geometry("800x600")

        self.simulator = DataSimulator()

        self.setup_ui()

    def setup_ui(self):
        # Main container
        main_frame = ttk.Frame(self.root, padding="10")
        main_frame.grid(row=0, column=0, sticky=(tk.W, tk.E, tk.N, tk.S))

        self.root.columnconfigure(0, weight=1)
        self.root.rowconfigure(0, weight=1)
        main_frame.columnconfigure(1, weight=1)
        main_frame.rowconfigure(4, weight=1)

        # Source folder
        ttk.Label(main_frame, text="Source Folder:").grid(row=0, column=0, sticky=tk.W, pady=5)
        self.source_entry = ttk.Entry(main_frame, width=60)
        self.source_entry.insert(0, self.simulator.source_base)
        self.source_entry.grid(row=0, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_source).grid(row=0, column=2, pady=5)

        # Target folder
        ttk.Label(main_frame, text="Target Folder:").grid(row=1, column=0, sticky=tk.W, pady=5)
        self.target_entry = ttk.Entry(main_frame, width=60)
        self.target_entry.insert(0, self.simulator.target_base)
        self.target_entry.grid(row=1, column=1, sticky=(tk.W, tk.E), pady=5, padx=5)
        ttk.Button(main_frame, text="Browse", command=self.browse_target).grid(row=1, column=2, pady=5)

        # Progress bar
        ttk.Label(main_frame, text="Progress:").grid(row=2, column=0, sticky=tk.W, pady=5)
        self.progress = ttk.Progressbar(main_frame, length=400, mode='determinate')
        self.progress.grid(row=2, column=1, columnspan=2, sticky=(tk.W, tk.E), pady=5, padx=5)

        # Control buttons
        button_frame = ttk.Frame(main_frame)
        button_frame.grid(row=3, column=0, columnspan=3, pady=10)

        self.start_button = ttk.Button(button_frame, text="Start Simulation", command=self.start_simulation, width=20)
        self.start_button.pack(side=tk.LEFT, padx=5)

        self.stop_button = ttk.Button(button_frame, text="Stop", command=self.stop_simulation, width=20, state=tk.DISABLED)
        self.stop_button.pack(side=tk.LEFT, padx=5)

        self.reset_button = ttk.Button(button_frame, text="Reset Target Folder", command=self.reset_target, width=20)
        self.reset_button.pack(side=tk.LEFT, padx=5)

        # Log area
        ttk.Label(main_frame, text="Log:").grid(row=4, column=0, sticky=(tk.W, tk.N), pady=5)
        self.log_text = scrolledtext.ScrolledText(main_frame, height=20, width=80, wrap=tk.WORD)
        self.log_text.grid(row=4, column=1, columnspan=2, sticky=(tk.W, tk.E, tk.N, tk.S), pady=5, padx=5)

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

    def log(self, message):
        """Thread-safe logging"""
        self.root.after(0, lambda: self._log_internal(message))

    def _log_internal(self, message):
        timestamp = datetime.now().strftime("%H:%M:%S")
        self.log_text.insert(tk.END, f"[{timestamp}] {message}\n")
        self.log_text.see(tk.END)

    def update_progress(self, value):
        """Thread-safe progress update"""
        self.root.after(0, lambda: self.progress.configure(value=value))

    def start_simulation(self):
        # Validate inputs
        if not self.target_entry.get():
            messagebox.showerror("Error", "Please select a target folder!")
            return

        self.simulator.source_base = self.source_entry.get()
        self.simulator.target_base = self.target_entry.get()

        # Update UI state
        self.start_button.configure(state=tk.DISABLED)
        self.stop_button.configure(state=tk.NORMAL)
        self.reset_button.configure(state=tk.DISABLED)
        self.progress['value'] = 0

        # Clear log
        self.log_text.delete(1.0, tk.END)

        # Start simulation in thread
        self.simulator.is_running = True
        self.simulator.simulation_thread = threading.Thread(
            target=self.simulator.run_simulation,
            args=(self.log, self.update_progress, self.on_simulation_complete),
            daemon=True
        )
        self.simulator.simulation_thread.start()

    def stop_simulation(self):
        self.log("Stopping simulation...")
        self.simulator.is_running = False

    def on_simulation_complete(self):
        """Called when simulation completes or is stopped"""
        self.root.after(0, self._update_ui_after_simulation)

    def _update_ui_after_simulation(self):
        self.start_button.configure(state=tk.NORMAL)
        self.stop_button.configure(state=tk.DISABLED)
        self.reset_button.configure(state=tk.NORMAL)

    def reset_target(self):
        if not self.simulator.moved_items:
            messagebox.showinfo("Info", "No files to reset. Run a simulation first.")
            return

        result = messagebox.askyesno(
            "Confirm Reset",
            f"Move {len(self.simulator.moved_items)} items back to original location?\n\nThis will restore the source folder."
        )

        if result:
            # Reset progress bar
            self.progress['value'] = 0
            try:
                moved_back = 0
                failed = 0

                # Move files back to original location in reverse order
                for item in reversed(self.simulator.moved_items):
                    try:
                        target_path = item['target']
                        source_path = item['source']

                        # Only move if file exists at target
                        if os.path.exists(target_path):
                            # Ensure source parent directory exists
                            source_parent = os.path.dirname(source_path)
                            if source_parent:
                                os.makedirs(source_parent, exist_ok=True)

                            # Move back using os.rename (instant)
                            os.rename(target_path, source_path)
                            moved_back += 1
                            self.log(f"✓ Restored: {os.path.basename(source_path)}")
                        else:
                            self.log(f"⊘ Skip (not found): {os.path.basename(target_path)}")

                    except Exception as e:
                        failed += 1
                        self.log(f"✗ Error restoring {os.path.basename(source_path)}: {str(e)}")

                # Clear moved items list
                self.simulator.moved_items = []

                self.log(f"Reset complete: {moved_back} items restored, {failed} failed")
                messagebox.showinfo("Reset Complete", f"Moved {moved_back} items back to original location\nFailed: {failed}")

            except Exception as e:
                self.log(f"Error during reset: {str(e)}")
                messagebox.showerror("Error", f"Failed to reset:\n{str(e)}")


if __name__ == "__main__":
    root = tk.Tk()
    app = SimulatorGUI(root)
    root.mainloop()
