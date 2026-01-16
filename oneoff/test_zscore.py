
import json
import math

# Partial data from the file or manual recreation of the flow
# Widths sequence from the file around the failure point
# Let's say we have a stable history of 1896/1888s
# Then 1528 -> 1616 -> 1520 -> 1248

data_widths = [
    1896, 1896, 1896, 1888, 1888, 1888, 1888, 1896, 1912, 1896, # 10 samples
    1896, 1880, 1896, 1896, 1912, 1888, 1904, 1904, 1880, 1904, # More samples...
    2056, # Outlier?
    1904, 1880, 1880, 1896, 1896, 1880, 1896, 1888, 1896, 1880,
    1992, # Outlier?
    1880, 1888, 1864, 1896, 1872, 1912, 1888, 1888, 1896, 1872,
    1528, # First drop (Line 171)
    1616, # (Line 175)
    1520, # (Line 179)
    1248  # (Line 183)
]

# Config
WINDOW_SIZE = 10
THRESHOLD = 2.0

history = []

print(f"{'Value':<10} {'Mean':<10} {'Std':<10} {'Z-Score':<10} {'Result':<10}")

def calculate_stats(hist):
    if len(hist) < 2: return 0, 0
    mean = sum(hist) / len(hist)
    variance = sum((x - mean) ** 2 for x in hist) / len(hist)
    return mean, math.sqrt(variance)

for val in data_widths:
    if len(history) < WINDOW_SIZE:
        history.append(val)
        print(f"{val:<10} {'N/A':<10} {'N/A':<10} {'Initializing'}")
        continue
    
    # Calculate stats on CURRENT history (before adding)
    mean, std = calculate_stats(history)
    
    z_score = None
    is_abnormal = False
    
    if std == 0:
        z_score = None # Code returns null
        # Logic: if null, return Normal
        is_abnormal = False
    else:
        z_score = (val - mean) / std
        is_abnormal = abs(z_score) > THRESHOLD
        
    res_str = "ABNORMAL" if is_abnormal else "Normal"
    z_str = f"{z_score:.2f}" if z_score is not None else "Std=0"
    
    print(f"{val:<10} {mean:<10.2f} {std:<10.2f} {z_str:<10} {res_str}")
    
    # Add to history, remove oldest
    history.append(val)
    if len(history) > WINDOW_SIZE:
        history.pop(0)
