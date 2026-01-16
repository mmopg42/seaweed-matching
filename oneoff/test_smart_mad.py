
import json
import math
import os

WINDOW_SIZE = 10
THRESHOLD = 2.5
CONSECUTIVE_LIMIT = 20 # If 20 bad images in a row, assume camera changed and reset

class SmartMadDetector:
    def __init__(self):
        self.history = []
        self.consecutive_abnormal = 0
        self.last_clean_median = None

    def get_median(self, values):
        sorted_v = sorted(values)
        n = len(sorted_v)
        if n == 0: return 0
        if n % 2 == 1:
            return sorted_v[n//2]
        else:
            return (sorted_v[n//2 - 1] + sorted_v[n//2]) / 2.0

    def add_and_check(self, value):
        # 1. Initialize
        if len(self.history) < 5:
            self.history.append(value)
            return {"z": 0, "res": "Init", "med": value, "mad": 0, "action": "Added(Init)"}

        # 2. Stats from CLEAN history
        median = self.get_median(self.history)
        deviations = [abs(x - median) for x in self.history]
        mad = self.get_median(deviations)

        z_score = 0
        is_abnormal = False
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

        action = ""

        # 3. Update Logic
        if is_abnormal:
            self.consecutive_abnormal += 1
            action = "Ignored(Pollution)"
            
            # Reset check
            if self.consecutive_abnormal > CONSECUTIVE_LIMIT:
                self.history = [value] # Hard Reset
                self.consecutive_abnormal = 0
                action = "RESET(Drift)"
                is_abnormal = False # Treat the reset point as new normal?
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
            "action": action
        }

def run_smart_simulation():
    json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")
    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    
    det_w = SmartMadDetector()
    det_h = SmartMadDetector()

    targets = [
        "C260109T184603_0", 
        "C260109T184715_0", 
        "C260109T184716_0",
        "C260109T184718_0",
        "C260109T184720_0",
        "C260109T184707_0",
        "C260109T184709_0",
        "C260109T184724_0"
    ]

    print(f"{'Key':<20} | {'W':<5} {'Med':<6} {'MAD':<6} {'Z(W)':<8} {'Res(W)':<10} {'Action':<15} | {'H':<5} {'Med':<6} {'MAD':<6} {'Z(H)':<8} {'Res(H)':<10} {'Action':<15}")
    print("-" * 140)

    for key in sorted_keys:
        val = data[key]
        w = val['width']
        h = val['height']
        
        rw = det_w.add_and_check(w)
        rh = det_h.add_and_check(h)

        if key in targets:
            w_z = f"{rw['z']:.2f}" if rw['z'] is not None else "-"
            h_z = f"{rh['z']:.2f}" if rh['z'] is not None else "-"
            
            print(f"{key:<20} | {w:<5} {rw['med']:<6} {rw['mad']:<6} {w_z:<8} {rw['res']:<10} {rw['action']:<15} | {h:<5} {rh['med']:<6} {rh['mad']:<6} {h_z:<8} {rh['res']:<10} {rh['action']:<15}")

if __name__ == "__main__":
    run_smart_simulation()
