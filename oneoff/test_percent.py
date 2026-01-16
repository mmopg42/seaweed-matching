
import json
import math
import os

WINDOW_SIZE = 10
PERCENT_THRESHOLD = 0.10 # 10% deviation allowed

class PercentDetector:
    def __init__(self):
        self.history = []

    def get_median(self, values):
        sorted_v = sorted(values)
        n = len(sorted_v)
        if n == 0: return 0
        if n % 2 == 1:
            return sorted_v[n//2]
        else:
            return (sorted_v[n//2 - 1] + sorted_v[n//2]) / 2.0

    def add_and_check(self, value):
        # 1. Init
        if len(self.history) < 5:
            self.history.append(value)
            return {"res": "Init", "med": value, "diff_pct": 0, "limit": 0}

        # 2. Baseline = Median
        median = self.get_median(self.history)
        
        # 3. Check Deviation
        diff = abs(value - median)
        limit = median * PERCENT_THRESHOLD
        
        is_abnormal = diff > limit
        diff_pct = (diff / median) * 100 if median > 0 else 0

        # 4. Update History
        # We add everything. Median filters outliers naturally.
        self.history.append(value)
        if len(self.history) > WINDOW_SIZE:
            self.history.pop(0)

        return {
            "res": "ABNORMAL" if is_abnormal else "Normal",
            "med": median,
            "diff_pct": diff_pct,
            "limit": limit
        }

def run_percent_simulation():
    # Path fix for oneoff directory
    json_path = os.path.join(os.getcwd(), "..", "examples", "image_sizes.json")
    if not os.path.exists(json_path):
         json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    
    det_w = PercentDetector()
    det_h = PercentDetector()

    print(f"{'Key':<20} | {'W':<5} {'Med':<6} {'Dev%':<6} {'Res(W)':<10} | {'H':<5} {'Med':<6} {'Dev%':<6} {'Res(H)':<10}")
    print("-" * 100)

    targets = [
        "C260109T184603_0", 
        "C260109T184707_0", 
        "C260109T184709_0", 
        "C260109T184711_0", 
        "C260109T184715_0", 
        "C260109T184716_0", 
        "C260109T184718_0", 
        "C260109T184720_0", 
        "C260109T184722_0", 
        "C260109T184724_0"
    ]

    for key in sorted_keys:
        val = data[key]
        w = val['width']
        h = val['height']
        
        rw = det_w.add_and_check(w)
        rh = det_h.add_and_check(h)

        if key in targets:
            print(f"{key:<20} | {w:<5} {rw['med']:<6.0f} {rw['diff_pct']:<6.1f} {rw['res']:<10} | {h:<5} {rh['med']:<6.0f} {rh['diff_pct']:<6.1f} {rh['res']:<10}")

if __name__ == "__main__":
    run_percent_simulation()
