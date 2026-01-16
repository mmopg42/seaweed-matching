
import json
import math
import os

WINDOW_SIZE = 10
AREA_THRESHOLD = 0.15 # 15% change in total area is significant

class AreaDetector:
    def __init__(self):
        self.history = []

    def get_median(self, values):
        sorted_v = sorted(values)
        if not sorted_v: return 0
        n = len(sorted_v)
        if n % 2 == 1:
            return sorted_v[n//2]
        else:
            return (sorted_v[n//2 - 1] + sorted_v[n//2]) / 2.0

    def add_and_check(self, w, h):
        area = w * h
        
        if len(self.history) < 5:
            self.history.append(area)
            return {"res": "Init", "med": area, "dev": 0}

        median = self.get_median(self.history)
        diff = abs(area - median)
        dev_pct = (diff / median) * 100 if median > 0 else 0
        
        is_abnormal = dev_pct > (AREA_THRESHOLD * 100)

        # Update history
        self.history.append(area)
        if len(self.history) > WINDOW_SIZE:
            self.history.pop(0)

        return {
            "res": "ABNORMAL" if is_abnormal else "Normal",
            "med": median,
            "dev": dev_pct
        }

def run_area_simulation():
    # Path fix
    json_path = os.path.join(os.getcwd(), "..", "examples", "image_sizes.json")
    if not os.path.exists(json_path):
         json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    det = AreaDetector()

    print(f"{'Key':<20} | {'W':<5} {'H':<5} {'Area':<10} {'MedArea':<10} {'Dev%':<6} {'Res':<10}")
    print("-" * 80)

    for key in sorted_keys:
        val = data[key]
        w = val['width']
        h = val['height']
        area = w * h
        
        res = det.add_and_check(w, h)
        print(f"{key:<20} | {w:<5} {h:<5} {area:<10} {res['med']:<10.0f} {res['dev']:<6.1f} {res['res']:<10}")

if __name__ == "__main__":
    run_area_simulation()
