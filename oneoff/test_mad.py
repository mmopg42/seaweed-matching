
import json
import math
import os

WINDOW_SIZE = 10
THRESHOLD = 2.5 # Standard equivalent for Modified Z-score is often 3.5, but let's test with 2.5 first as per user context
STRICT_MODE = True # If MAD=0, any diff is abnormal

class MadDetector:
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
        # 1. Calc stats on EXISTING history
        if len(self.history) < 5:
            self.history.append(value)
            return {"z": None, "res": "Init", "med": None, "mad": None}

        median = self.get_median(self.history)
        deviations = [abs(x - median) for x in self.history]
        mad = self.get_median(deviations)

        z_score = 0
        is_abnormal = False
        
        # Modified Z-score constant: 0.6745
        k = 0.6745

        if mad == 0:
            if value == median:
                z_score = 0
                is_abnormal = False
            else:
                z_score = 999.0 # Infinity
                is_abnormal = True 
        else:
            z_score = k * (value - median) / mad
            is_abnormal = abs(z_score) > THRESHOLD

        self.history.append(value)
        if len(self.history) > WINDOW_SIZE:
            self.history.pop(0)

        return {
            "z": z_score, 
            "res": "ABNORMAL" if is_abnormal else "Normal",
            "med": median,
            "mad": mad
        }

def run_mad_simulation():
    json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    
    det_w = MadDetector()
    det_h = MadDetector()

    # Targets to check
    targets = [
        "C260109T184603_0", # User says Normal, Logic says Abnormal
        "C260109T184715_0", # User says Abnormal, Logic says Normal
        "C260109T184716_0",
        "C260109T184718_0",
        "C260109T184720_0",
        "C260109T184707_0",
        "C260109T184709_0"
    ]

    print(f"{'Key':<20} | {'W':<5} {'Med':<6} {'MAD':<6} {'Z(W)':<8} {'Res(W)':<10} | {'H':<5} {'Med':<6} {'MAD':<6} {'Z(H)':<8} {'Res(H)':<10}")
    print("-" * 110)

    for key in sorted_keys:
        val = data[key]
        w = val['width']
        h = val['height']
        
        rw = det_w.add_and_check(w)
        rh = det_h.add_and_check(h)

        if key in targets:
            w_z = f"{rw['z']:.2f}" if rw['z'] is not None else "-"
            h_z = f"{rh['z']:.2f}" if rh['z'] is not None else "-"
            
            print(f"{key:<20} | {w:<5} {rw['med']:<6} {rw['mad']:<6} {w_z:<8} {rw['res']:<10} | {h:<5} {rh['med']:<6} {rh['mad']:<6} {h_z:<8} {rh['res']:<10}")

if __name__ == "__main__":
    run_mad_simulation()
