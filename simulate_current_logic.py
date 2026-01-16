
import json
import math
import os

# Configuration matching C# defaults
WINDOW_SIZE = 10
THRESHOLD = 2.0
MIN_SAMPLES = 5

class AbnormalDetectorSimulator:
    def __init__(self):
        self.history = [] # List of values
        
    def add_and_check(self, value):
        # Logic from AbnormalDetectorService.cs
        
        # 1. Check if history is sufficient
        if len(self.history) < MIN_SAMPLES:
            self.history.append(value)
            return {
                "is_abnormal": False,
                "z_score": None,
                "mean": None,
                "std": None,
                "note": "Building history"
            }
            
        # 2. Calculate stats on EXISTING history (before adding current)
        mean, std = self._calculate_stats(self.history)
        
        z_score = None
        is_abnormal = False
        note = ""
        
        if std == 0:
            # Bug replication: If std is 0, return Normal
            z_score = None
            is_abnormal = False
            note = "Std=0 (Normal)"
        else:
            z_score = (value - mean) / std
            is_abnormal = abs(z_score) > THRESHOLD
            
        # 3. Add current value to history
        self.history.append(value)
        
        # 4. Maintain window size
        if len(self.history) > WINDOW_SIZE:
            self.history.pop(0)
            
        return {
            "is_abnormal": is_abnormal,
            "z_score": z_score,
            "mean": mean,
            "std": std,
            "note": note
        }

    def _calculate_stats(self, values):
        if len(values) < 2:
            return 0, 0
            
        mean = sum(values) / len(values)
        
        variance_sum = sum((v - mean) ** 2 for v in values)
        variance = variance_sum / len(values)
        std = math.sqrt(variance)
        
        return mean, std

def run_simulation(json_path, output_path):
    print(f"Reading {json_path}...")
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
        
    # Sort keys to ensure chronological order
    sorted_keys = sorted(data.keys())
    
    detector_w = AbnormalDetectorSimulator()
    detector_h = AbnormalDetectorSimulator()
    
    results = []
    
    print(f"Processing {len(sorted_keys)} items...")
    
    with open(output_path, 'w', encoding='utf-8') as f:
        # Header
        header = f"{'Key':<20} | {'W':<5} {'Mean':<8} {'Std':<8} {'Z(W)':<8} {'Res(W)':<10} | {'H':<5} {'Mean':<8} {'Std':<8} {'Z(H)':<8} {'Res(H)':<10}"
        f.write(header + "\n")
        f.write("-" * len(header) + "\n")
        print(header)
        
        for key in sorted_keys:
            dims = data[key]
            width = dims['width']
            height = dims['height']
            
            res_w = detector_w.add_and_check(width)
            res_h = detector_h.add_and_check(height)
            
            # Format output
            w_z = f"{res_w['z_score']:.2f}" if res_w['z_score'] is not None else "N/A"
            w_res = "ABNORMAL" if res_w['is_abnormal'] else "Normal"
            w_mean = f"{res_w['mean']:.1f}" if res_w['mean'] is not None else "-"
            w_std = f"{res_w['std']:.1f}" if res_w['std'] is not None else "-"
            
            h_z = f"{res_h['z_score']:.2f}" if res_h['z_score'] is not None else "N/A"
            h_res = "ABNORMAL" if res_h['is_abnormal'] else "Normal"
            h_mean = f"{res_h['mean']:.1f}" if res_h['mean'] is not None else "-"
            h_std = f"{res_h['std']:.1f}" if res_h['std'] is not None else "-"
            
            line = f"{key:<20} | {width:<5} {w_mean:<8} {w_std:<8} {w_z:<8} {w_res:<10} | {height:<5} {h_mean:<8} {h_std:<8} {h_z:<8} {h_res:<10}"
            
            f.write(line + "\n")
            print(line)

if __name__ == "__main__":
    json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")
    output_path = os.path.join(os.getcwd(), "simulation_result.txt")
    run_simulation(json_path, output_path)
