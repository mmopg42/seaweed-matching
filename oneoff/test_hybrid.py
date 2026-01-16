
import json
import math
import os

WINDOW_SIZE = 10
THRESHOLD = 2.5
MIN_ABS_DIFF = 150 # Minimum absolute difference (pixels) to consider abnormal (User considers 104px diff Normal, 208px Abnormal)
CONSECUTIVE_LIMIT = 5

class HybridDetector:
    def __init__(self):
        self.history = []
        self.consecutive_abnormal = 0

    def get_median(self, values):
        sorted_v = sorted(values)
        n = len(sorted_v)
        if n == 0: return 0
        if n % 2 == 1:
            return sorted_v[n//2]
        else:
            return (sorted_v[n//2 - 1] + sorted_v[n//2]) / 2.0

    def add_and_check(self, value):
        if len(self.history) < 5:
            self.history.append(value)
            return {"z": 0, "res": "Init", "med": value, "mad": 0, "diff": 0, "action": "Added(Init)"}

        median = self.get_median(self.history)
        deviations = [abs(x - median) for x in self.history]
        mad = self.get_median(deviations)

        # 1. Calculate Score
        z_score = 0
        k = 0.6745
        
        if mad < 1.0: mad = 1.0 # Floor to prevent division by zero (Noise floor)

        z_score = k * (value - median) / mad
        
        stats_abnormal = abs(z_score) > THRESHOLD
        
        # 2. Magnitude Check
        abs_diff = abs(value - median)
        magnitude_abnormal = abs_diff > MIN_ABS_DIFF
        
        # 3. Final Decision
        is_abnormal = stats_abnormal and magnitude_abnormal

        action = ""
        
        if is_abnormal:
            self.consecutive_abnormal += 1
            action = "Ignored"
            if self.consecutive_abnormal > CONSECUTIVE_LIMIT:
                 # If we see many consecutive abnormals, maybe the baseline actually changed?
                 # But for now, just logging.
                 action = "Ignored(HighCount)"
        else:
            self.consecutive_abnormal = 0
            self.history.append(value)
            action = "Added"
            if len(self.history) > WINDOW_SIZE:
                self.history.pop(0)

        return {
            "z": z_score, 
            "res": "ABNORMAL" if is_abnormal else "Normal",
            "med": median,
            "mad": mad,
            "diff": abs_diff,
            "action": action
        }

def run_hybrid_simulation():
    json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    
    det_w = HybridDetector()
    det_h = HybridDetector()

    targets = [
        "C260109T184603_0", # Height 2008 vs ~2112 (Diff 104) -> Expect Normal
        "C260109T184707_0", # W 1616 vs ~1896 (Diff 280) -> Expect Abnormal
        "C260109T184715_0", # W 1688 vs ~1896 (Diff 208) -> Expect Abnormal
    ]
    
    # Also print the ones user specifically mentioned
    print(f"{'Key':<20} | {'W':<5} {'Med':<6} {'Diff':<6} {'Z(W)':<8} {'Res(W)':<10} | {'H':<5} {'Med':<6} {'Diff':<6} {'Z(H)':<8} {'Res(H)':<10}")
    print("-" * 120)

    for key in sorted_keys:
        val = data[key]
        w = val['width']
        h = val['height']
        
        rw = det_w.add_and_check(w)
        rh = det_h.add_and_check(h)

        if key in targets or "1847" in key or "184603" in key:
            w_z = f"{rw['z']:.2f}"
            h_z = f"{rh['z']:.2f}"
            
            print(f"{key:<20} | {w:<5} {rw['med']:<6} {rw['diff']:<6} {w_z:<8} {rw['res']:<10} | {h:<5} {rh['med']:<6} {rh['diff']:<6} {h_z:<8} {rh['res']:<10}")

if __name__ == "__main__":
    run_hybrid_simulation()
