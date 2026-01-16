
import json
import math
import os

WINDOW_SIZE = 10
THRESHOLD = 0.05 # 5% deviation in ratio

class RatioDetector:
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

    def add_and_check(self, w, h):
        ratio = w / h if h != 0 else 0
        
        if len(self.history) < 5:
            self.history.append(ratio)
            return {"res": "Init", "med": ratio, "dev": 0, "ratio": ratio}

        median = self.get_median(self.history)
        
        diff = abs(ratio - median)
        limit = median * THRESHOLD
        
        is_abnormal = diff > limit
        dev_pct = (diff / median) * 100 if median > 0 else 0

        # Update history
        self.history.append(ratio)
        if len(self.history) > WINDOW_SIZE:
            self.history.pop(0)

        return {
            "res": "ABNORMAL" if is_abnormal else "Normal",
            "med": median,
            "dev": dev_pct,
            "ratio": ratio
        }

def run_ratio_simulation():
    # Path fix
    json_path = os.path.join(os.getcwd(), "..", "examples", "image_sizes.json")
    if not os.path.exists(json_path):
         json_path = os.path.join(os.getcwd(), "examples", "image_sizes.json")

    with open(json_path, 'r', encoding='utf-8') as f:
        data = json.load(f)

    sorted_keys = sorted(data.keys())
    
    det = RatioDetector()

    print(f"{'Key':<20} | {'W':<5} {'H':<5} {'Ratio':<6} {'Med':<6} {'Dev%':<6} {'Res':<10}")
    print("-" * 80)

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
        
        res = det.add_and_check(w, h)

        if True:
            print(f"{key:<20} | {w:<5} {h:<5} {res['ratio']:<6.3f} {res['med']:<6.3f} {res['dev']:<6.1f} {res['res']:<10}")

if __name__ == "__main__":
    run_ratio_simulation()
