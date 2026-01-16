
import json
import math
import os

# Config
WINDOW_SIZE = 10
THRESHOLD = 0.12 # 12% deviation allowed (Adjustable)

class StableDetector:
    def __init__(self):
        self.history_w = []
        self.history_h = []

    def get_median(self, values):
        sorted_v = sorted(values)
        if not sorted_v: return 0
        n = len(sorted_v)
        if n % 2 == 1:
            return sorted_v[n//2]
        else:
            return (sorted_v[n//2 - 1] + sorted_v[n//2]) / 2.0

    def add_and_check(self, w, h):
        # 1. Initializing
        if len(self.history_w) < 5:
            self.history_w.append(w)
            self.history_h.append(h)
            return {"res": "Normal", "med_w": w, "med_h": h, "dev_w": 0, "dev_h": 0}

        # 2. Calculate stable medians
        med_w = self.get_median(self.history_w)
        med_h = self.get_median(self.history_h)
        
        # 3. Calculate deviations
        dev_w = (abs(w - med_w) / med_w) * 100 if med_w > 0 else 0
        dev_h = (abs(h - med_h) / med_h) * 100 if med_h > 0 else 0
        
        # 12% is a reasonable threshold for "Small vs Medium" drop
        is_abnormal = dev_w > (THRESHOLD * 100) or dev_h > (THRESHOLD * 100)

        # 4. CRITICAL: Only add to history if NORMAL (Clean Baseline)
        if not is_abnormal:
            self.history_w.append(w)
            self.history_h.append(h)
            if len(self.history_w) > WINDOW_SIZE:
                self.history_w.pop(0)
                self.history_h.pop(0)
        
        return {
            "res": "ABNORMAL" if is_abnormal else "Normal",
            "med_w": med_w,
            "med_h": med_h,
            "dev_w": dev_w,
            "dev_h": dev_h
        }

def run_stable_simulation():
    json_path = os.path.join(os.getcwd(), "..", "examples", "image_sizes.json")
    if not os.path.exists(json_path):
         json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    det = StableDetector()

    print(f"{'Key':<20} | {'W':<5} {'MedW':<5} {'DevW%':<6} | {'H':<5} {'MedH':<5} {'DevH%':<6} | {'Res':<10}")
    print("-" * 100)

    for key in sorted_keys:
        val = data[key]
        w = val['width']
        h = val['height']
        
        res = det.add_and_check(w, h)
        print(f"{key:<20} | {w:<5} {res['med_w']:<5.0f} {res['dev_w']:<6.1f} | {h:<5} {res['med_h']:<5.0f} {res['dev_h']:<6.1f} | {res['res']:<10}")

if __name__ == "__main__":
    run_stable_simulation()
