import os
import re
import shutil
import time
import threading
from datetime import datetime, timedelta
from pathlib import Path
import tkinter as tk
from tkinter import ttk, filedialog, scrolledtext, messagebox


class DataSimulator:
    def __init__(self):
        self.source_base = r"Z:\윤태경\seaweed\program\data\2025_A046"
        self.target_base = ""
        self.is_running = False
        self.simulation_thread = None

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
        """Copy file or folder to target location using hardlinks for speed"""
        target_path = os.path.join(self.target_base, item['relative_path'])

        if item['type'] == 'normal_folder':
            # Copy folder structure with hardlinks for files (much faster)
            if os.path.exists(target_path):
                shutil.rmtree(target_path)

            # Create directory structure and hardlink all files
            os.makedirs(target_path, exist_ok=True)
            for root, dirs, files in os.walk(item['source']):
                # Calculate relative path from source
                rel_path = os.path.relpath(root, item['source'])
                target_dir = os.path.join(target_path, rel_path) if rel_path != '.' else target_path

                # Create subdirectories
                for dir_name in dirs:
                    os.makedirs(os.path.join(target_dir, dir_name), exist_ok=True)

                # Hardlink files
                for file_name in files:
                    src_file = os.path.join(root, file_name)
                    dst_file = os.path.join(target_dir, file_name)
                    try:
                        # Use hardlink (instant, no disk space)
                        os.link(src_file, dst_file)
                    except:
                        # Fallback to copy if hardlink fails
                        shutil.copy2(src_file, dst_file)
        else:
            # Hardlink single file (instant)
            os.makedirs(os.path.dirname(target_path), exist_ok=True)
            try:
                if os.path.exists(target_path):
                    os.remove(target_path)
                os.link(item['source'], target_path)
            except:
                # Fallback to copy if hardlink fails
                shutil.copy2(item['source'], target_path)

    def run_simulation(self, log_callback, progress_callback, complete_callback):
        """Run the simulation in a separate thread"""
        copy_threads = []

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

            log_callback(f"Found {len(items)} items to copy")

            # Get the first timestamp as reference (t0)
            t0 = items[0]['timestamp']
            log_callback(f"Reference time (t0): {t0}")

            # Wait 5 seconds before starting
            log_callback("Waiting 5 seconds before starting...")
            for i in range(5, 0, -1):
                if not self.is_running:
                    log_callback("Simulation cancelled")
                    complete_callback()
                    return
                log_callback(f"Starting in {i}...")
                time.sleep(1)

            start_real_time = time.time()
            log_callback("Starting simulation...")

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

                # Start copy in background thread (non-blocking)
                actual_start = time.time()
                elapsed = actual_start - start_real_time
                log_callback(f"[T+{elapsed:.1f}s] Starting copy: {item['name']}")

                def copy_worker(item_data, idx_val):
                    try:
                        self.copy_item(item_data)
                        copy_time = time.time() - actual_start
                        log_callback(f"✓ Copied in {copy_time:.2f}s: {item_data['relative_path']}")
                    except Exception as e:
                        log_callback(f"✗ Error copying {item_data['name']}: {str(e)}")

                    # Update progress
                    progress = (idx_val + 1) / len(items) * 100
                    progress_callback(progress)

                thread = threading.Thread(target=copy_worker, args=(item, idx), daemon=True)
                thread.start()
                copy_threads.append(thread)

            # Wait for all copy operations to complete
            log_callback("Waiting for all copy operations to complete...")
            for thread in copy_threads:
                thread.join()

            if self.is_running:
                log_callback("Simulation completed successfully!")

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

    def browse_target(self):
        folder = filedialog.askdirectory(initialdir=self.target_entry.get() or os.getcwd())
        if folder:
            self.target_entry.delete(0, tk.END)
            self.target_entry.insert(0, folder)
            self.simulator.target_base = folder

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
        if not self.target_entry.get():
            messagebox.showinfo("Info", "No target folder set")
            return

        result = messagebox.askyesno(
            "Confirm Reset",
            f"Delete all files (keeping folder structure) in:\n{self.target_entry.get()}\n\nAre you sure?"
        )

        if result:
            try:
                target = self.target_entry.get()
                if os.path.exists(target):
                    file_count = 0
                    folder_count = 0

                    # Walk through all directories and delete only files
                    for root, dirs, files in os.walk(target, topdown=False):
                        # Delete all files
                        for file_name in files:
                            file_path = os.path.join(root, file_name)
                            try:
                                os.remove(file_path)
                                file_count += 1
                            except Exception as e:
                                self.log(f"Failed to delete {file_path}: {str(e)}")

                        # Count folders (but don't delete them)
                        folder_count += len(dirs)

                    self.log(f"Deleted {file_count} files, kept {folder_count} folders")
                    messagebox.showinfo("Success", f"Deleted {file_count} files\nKept folder structure ({folder_count} folders)")
            except Exception as e:
                self.log(f"Error clearing target folder: {str(e)}")
                messagebox.showerror("Error", f"Failed to clear folder:\n{str(e)}")


if __name__ == "__main__":
    root = tk.Tk()
    app = SimulatorGUI(root)
    root.mainloop()
