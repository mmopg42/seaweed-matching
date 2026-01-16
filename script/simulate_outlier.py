
import os
import glob
from PIL import Image
import numpy as np

def calculate_z_score(val, buffer):
    if len(buffer) < 2:
        return None
    mean = np.mean(buffer)
    variance = np.var(buffer) # population variance? Code uses sample variance?
    # C# code uses: valuesList.Sum(x => Math.Pow(x - mean, 2)) / valuesList.Count; -> Population Variance
    
    if variance == 0:
        return None
        
    std = np.sqrt(variance)
    return (val - mean) / std

def simulate():
    folder = r"C:\workspace\seaweed\data\테스트 데이터\시뮬\20260108\normal"
    # Assuming png based on earlier context, user said "image size"
    # Actually user said "normal folder", usually has "stitched_original.png" inside subfolders?
    # Or is it a flat folder of images?
    # User said: "这里 보면 10번째 부터... 50% 확률로 이상치"
    # Using glob to find all images
    
    # Try finding subfolders first (structure: normal/C..._0/stitched_original.png)
    subfolders = sorted(glob.glob(os.path.join(folder, "*")))
    images = []
    
    print(f"Found {len(subfolders)} items in {folder}")
    
    for item in subfolders:
        if os.path.isdir(item):
            img_path = os.path.join(item, "stitched_original.png")
            if os.path.exists(img_path):
                images.append(img_path)
        elif item.lower().endswith(('.png', '.bmp', '.jpg')):
             images.append(item)
             
    print(f"Found {len(images)} images to process")
    
    width_buffer = []
    height_buffer = []
    
    window_size = 100
    min_samples = 10
    threshold = 3.0
    
    print(f"| Index | File | Width | Height | Z-W | Z-H | Abnormal? | BufferSize | BufferMeanW | BufferStdW |")
    print(f"|---|---|---|---|---|---|---|---|---|---|")
    
    for i, img_path in enumerate(images):
        try:
            with Image.open(img_path) as img:
                w, h = img.size
                
            fname = os.path.basename(os.path.dirname(img_path)) + "/" + os.path.basename(img_path)
            
            is_abnormal = False
            zw = None
            zh = None
            
            # Logic from C# code
            if len(width_buffer) < min_samples:
                width_buffer.append(w)
                height_buffer.append(h)
                print(f"| {i+1} | {fname} | {w} | {h} | - | - | SKIP (Init) | {len(width_buffer)} | - | - |")
                continue
                
            # Calculate Z
            zw = calculate_z_score(w, width_buffer)
            zh = calculate_z_score(h, height_buffer)
            
            mean_w = np.mean(width_buffer)
            var_w = np.var(width_buffer)
            std_w = np.sqrt(var_w)
            
            if zw is None or zh is None:
                # Variance 0 -> Consider Normal
                is_abnormal = False
                note = "Var=0"
            else:
                 if abs(zw) > threshold or abs(zh) > threshold:
                     is_abnormal = True
                 note = ""
                 
            print(f"| {i+1} | {fname} | {w} | {h} | {zw if zw is not None else 'NaN'} | {zh if zh is not None else 'NaN'} | {is_abnormal} {note} | {len(width_buffer)} | {mean_w:.2f} | {std_w:.2f} |")
            
            width_buffer.append(w)
            height_buffer.append(h)
            
            # Maintain Window
            while len(width_buffer) > window_size:
                width_buffer.pop(0)
                height_buffer.pop(0)
                
        except Exception as e:
            print(f"Error processing {img_path}: {e}")

if __name__ == "__main__":
    simulate()
