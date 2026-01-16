
import os
import glob
from PIL import Image
import numpy as np

def calculate_z_score(val, buffer):
    if len(buffer) < 2:
        return None
    mean = np.mean(buffer)
    variance = np.var(buffer)
    
    if variance == 0:
        return None
        
    std = np.sqrt(variance)
    return (val - mean) / std

def simulate():
    folder = r"C:\workspace\seaweed\data\테스트 데이터\시뮬\20260108\normal"
    subfolders = sorted(glob.glob(os.path.join(folder, "*")))
    images = []
    
    for item in subfolders:
        if os.path.isdir(item):
            img_path = os.path.join(item, "stitched_original.png")
            if os.path.exists(img_path):
                images.append(img_path)
        elif item.lower().endswith(('.png', '.bmp', '.jpg')):
             images.append(item)
    
    width_buffer = []
    height_buffer = []
    
    window_size = 100
    min_samples = 10
    threshold = 3.0
    
    print(f"| Index | Width | Height | Z-W | Z-H | Abnormal? | BufferSize | BufferMeanW | BufferStdW |")
    print(f"|---|---|---|---|---|---|---|---|---|")
    
    for i, img_path in enumerate(images):
        if i >= 30: break 
        
        try:
            with Image.open(img_path) as img:
                w, h = img.size
                
            is_abnormal = False
            zw = None
            zh = None
            
            if len(width_buffer) < min_samples:
                width_buffer.append(w)
                height_buffer.append(h)
                print(f"| {i+1} | {w} | {h} | - | - | SKIP (Init) | {len(width_buffer)} | - | - |")
                continue
                
            zw = calculate_z_score(w, width_buffer)
            zh = calculate_z_score(h, height_buffer)
            
            mean_w = np.mean(width_buffer)
            var_w = np.var(width_buffer)
            std_w = np.sqrt(var_w)
            
            if zw is None or zh is None:
                is_abnormal = False
                note = "Var=0"
            else:
                 if abs(zw) > threshold or abs(zh) > threshold:
                     is_abnormal = True
                 note = ""
                 
            # Print only integer parts for W/H to save space, simplify format
            print(f"| {i+1} | {w} | {h} | {zw if zw is not None else 'NaN'} | {zh if zh is not None else 'NaN'} | {is_abnormal} {note} | {len(width_buffer)} | {mean_w:.1f} | {std_w:.1f} |")
            
            width_buffer.append(w)
            height_buffer.append(h)
            
            while len(width_buffer) > window_size:
                width_buffer.pop(0)
                height_buffer.pop(0)
                
        except Exception as e:
            print(f"Error: {e}")

if __name__ == "__main__":
    simulate()
