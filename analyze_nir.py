
import os
import glob
import math
import statistics

def calculate_std_dev(values):
    if len(values) < 2:
        return 0.0
    return statistics.stdev(values)

def analyze_file(filepath):
    wavelengths = []
    intensities = []
    
    try:
        # Try cp949 first (common for Korean Windows files), fallback to ignore errors
        with open(filepath, 'r', encoding='cp949', errors='replace') as f:
            lines = f.readlines()
            for line in lines:
                line = line.strip()
                if not line or line.startswith('#'):
                    continue
                    
                parts = line.split()
                # Handle 2 columns (Wavelength Intensity) or 3 columns (Pixel Wavelength Intensity)
                if len(parts) >= 2:
                    try:
                        # Assuming last column is Intensity, second to last is Wavelength
                        # If 2 cols: [0]=Wave, [1]=Int
                        # If 3 cols: [0]=Pixel, [1]=Wave, [2]=Int
                        
                        if len(parts) == 2:
                             w = float(parts[0])
                             i = float(parts[1])
                        else:
                             w = float(parts[1])
                             i = float(parts[2])
                             
                        wavelengths.append(w)
                        intensities.append(i)
                    except ValueError:
                        continue
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
        return

    # Filter 4500-6500
    filtered_data = []
    for w, i in zip(wavelengths, intensities):
        if 4500 <= w <= 6500:
            filtered_data.append((w, i))
            
    if not filtered_data:
        print(f"{os.path.basename(filepath)}: No data in range 4500-6500")
        return

    y_values = [d[1] for d in filtered_data]
    
    # 1. Global Metrics
    y_range = max(y_values) - min(y_values)
    y_std = calculate_std_dev(y_values)
    
    # 2. Window Analysis (Window=800, Stride=100)
    window_size = 800
    stride = 100
    
    x_min = min(d[0] for d in filtered_data)
    x_max = max(d[0] for d in filtered_data)
    
    window_ranges = []
    current_x = x_min
    
    while current_x + window_size <= x_max:
        window_y = [d[1] for d in filtered_data if current_x <= d[0] <= current_x + window_size]
        if window_y:
            window_ranges.append(max(window_y) - min(window_y))
        current_x += stride
        
    if not window_ranges:
         print(f"{os.path.basename(filepath)}: No windows generated")
         return

    window_800_mean = statistics.mean(window_ranges)
    window_800_std = calculate_std_dev(window_ranges)
    window_800_max = max(window_ranges)
    
    # Thresholds
    t_y_range = 0.035
    t_y_std = 0.010
    t_window_mean = 0.025
    t_window_std = 0.010
    t_window_max = 0.035
    
    # Check Pass/Fail
    p_y_range = y_range >= t_y_range
    p_y_std = y_std >= t_y_std
    p_window_mean = window_800_mean >= t_window_mean
    p_window_std = window_800_std >= t_window_std
    p_window_max = window_800_max >= t_window_max
    
    passed_count = sum([p_y_range, p_y_std, p_window_mean, p_window_std, p_window_max])
    is_valid = passed_count >= 4
    
    print(f"File: {os.path.basename(filepath)}")
    print(f"  y_range: {y_range:.6f} [{'PASS' if p_y_range else 'FAIL'}] (limit: {t_y_range})")
    print(f"  y_std: {y_std:.6f} [{'PASS' if p_y_std else 'FAIL'}] (limit: {t_y_std})")
    print(f"  win_mean: {window_800_mean:.6f} [{'PASS' if p_window_mean else 'FAIL'}] (limit: {t_window_mean})")
    print(f"  win_std: {window_800_std:.6f} [{'PASS' if p_window_std else 'FAIL'}] (limit: {t_window_std})")
    print(f"  win_max: {window_800_max:.6f} [{'PASS' if p_window_max else 'FAIL'}] (limit: {t_window_max})")
    print(f"  Result: {passed_count}/5 -> {'DETECTED (Pass)' if is_valid else 'IGNORED (Fail)'}")
    print("-" * 50)

# Run on all files in directory
target_dir = r"C:\workspace\seaweed\gui_kiro_v2\task_helper\test_data"
files = glob.glob(os.path.join(target_dir, "*.txt"))

print(f"Found {len(files)} txt files.")
for f in files:
    analyze_file(f)
