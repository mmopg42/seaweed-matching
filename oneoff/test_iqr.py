
import json
import math
import os

WINDOW_SIZE = 10
# IQM/IQR approach
MULTIPLIER = 1.5
MIN_IQR = 100 # Minimum Interquartile Range allowed (pixels). Prevents 0-width boxes.

class IqrDetector:
    def __init__(self):
        self.history = []

    def add_and_check(self, value):
        if len(self.history) < 5:
            self.history.append(value)
            return {"res": "Init", "q1": 0, "q3": 0, "iqr": 0, "lb": 0, "ub": 0}

        sorted_h = sorted(self.history)
        n = len(sorted_h)
        
        # Calculate Q1 (25%) and Q3 (75%)
        q1_idx = int(n * 0.25)
        q3_idx = int(n * 0.75)
        
        q1 = sorted_h[q1_idx]
        q3 = sorted_h[q3_idx]
        
        iqr = q3 - q1
        
        # Enforce minimum variation (Stability guard)
        # If the history is super stable (IQR=0), we pretend it has at least MIN_IQR width
        effective_iqr = max(iqr, MIN_IQR)
        
        lower_bound = q1 - (effective_iqr * MULTIPLIER)
        upper_bound = q3 + (effective_iqr * MULTIPLIER)
        
        is_abnormal = value < lower_bound or value > upper_bound

        # Logic: Do we add outliers to history?
        # If we do, the box expands. If we don't, we might lock out valid shifts.
        # Let's try: Add everything, trusting the Rolling Window to clear old data.
        self.history.append(value)
        if len(self.history) > WINDOW_SIZE:
             self.history.pop(0)

        return {
            "res": "ABNORMAL" if is_abnormal else "Normal",
            "q1": q1,
            "q3": q3,
            "iqr": effective_iqr,
            "lb": lower_bound,
            "ub": upper_bound,
            "val": value
        }

def run_iqr_simulation():
    json_path = os.path.join(os.getcwd(), "..", "examples", "image_sizes.json")
    if not os.path.exists(json_path):
         json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    
    det_w = IqrDetector()
    det_h = IqrDetector()

    print(f"{'Key':<20} | {'W':<5} {'Range':<15} {'Res(W)':<10} | {'H':<5} {'Range':<15} {'Res(H)':<10}")
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
            w_range = f"[{rw['lb']:.0f}-{rw['ub']:.0f}]"
            h_range = f"[{rh['lb']:.0f}-{rh['ub']:.0f}]"
            print(f"{key:<20} | {w:<5} {w_range:<15} {rw['res']:<10} | {h:<5} {h_range:<15} {rh['res']:<10}")

if __name__ == "__main__":
    run_iqr_simulation()
