import os
from pathlib import Path
from datetime import datetime, timedelta
from PIL import Image, ImageDraw
import random

# 기준 시간: 2025-11-30 09:00:00
base_time = datetime(2025, 11, 30, 9, 0, 0)

# 데이터 폴더 생성
data_dir = Path("data")
normal_dir = data_dir / "normal"
nir_dir = data_dir / "nir"
cam1_dir = data_dir / "cam1"
cam2_dir = data_dir / "cam2"
cam3_dir = data_dir / "cam3"

for d in [normal_dir, nir_dir, cam1_dir, cam2_dir, cam3_dir]:
    d.mkdir(parents=True, exist_ok=True)

print(f"📁 데이터 폴더 생성: {data_dir.absolute()}")

def create_dummy_image(path: Path, text: str = "", size=(100, 100)):
    """더미 이미지 파일 생성"""
    img = Image.new('RGB', size, color=(73, 109, 137))
    
    if text:
        draw = ImageDraw.Draw(img)
        draw.text((10, 40), text[:15], fill=(255, 255, 255))
    
    img.save(path)

# 10개 그룹 생성
created_files = []

for i in range(10):
    # 각 그룹은 2분씩 차이
    group_time = base_time + timedelta(minutes=i*2)
    
    # 1. 일반카메라 폴더 생성 (C{YYMMDD}T{HHMMSS}_0)
    normal_folder_name = f"C{group_time.strftime('%y%m%d')}T{group_time.strftime('%H%M%S')}_0"
    normal_folder = normal_dir / normal_folder_name
    normal_folder.mkdir(exist_ok=True)
    
    # 일반카메라 이미지
    normal_image = normal_folder / "stitched_original.png"
    create_dummy_image(normal_image, f"Normal {i}")
    created_files.append(normal_image)
    
    # 2. NIR 파일들 (run_1{YYYYMMDD}T{HHMMSS})
    nir_base = f"run_1{group_time.strftime('%Y%m%d')}T{group_time.strftime('%H%M%S')}"
    nir_spc = nir_dir / f"{nir_base}.spc"
    nir_txt = nir_dir / f"{nir_base}A.txt"
    
    nir_spc.write_text(f"NIR SPC data for group {i}\n")
    nir_txt.write_text(f"NIR TXT data for group {i}\n")
    created_files.extend([nir_spc, nir_txt])
    
    # 3. 복합카메라 파일들 ({YYYYMMDD}_{HHMMSS}_{milliseconds})
    # 일반카메라보다 5-8초 사이로 랜덤하게 늦은 시간
    cam_delay = random.uniform(5.0, 8.0)
    cam_time = group_time + timedelta(seconds=cam_delay)
    cam_timestamp = cam_time.strftime("%Y%m%d_%H%M%S")
    
    cam1_file = cam1_dir / f"{cam_timestamp}_{100+i*10:03d}.bmp"
    cam2_file = cam2_dir / f"{cam_timestamp}_{200+i*10:03d}.bmp"
    cam3_file = cam3_dir / f"{cam_timestamp}_{300+i*10:03d}.bmp"
    
    for idx, cam_file in enumerate([cam1_file, cam2_file, cam3_file]):
        create_dummy_image(cam_file, f"Cam{idx+1}G{i}")
        created_files.append(cam_file)
    
    print(f"✅ 그룹 {i+1}/10 생성 완료: {group_time.strftime('%Y-%m-%d %H:%M:%S')} (복합카메라 +{cam_delay:.1f}초)")


print(f"\n🎉 총 {len(created_files)}개 파일 생성 완료!")
print(f"📂 경로: {data_dir.absolute()}")
print("\n📋 생성된 구조:")
print(f"  - normal: {len(list(normal_dir.iterdir()))} 폴더")
print(f"  - nir: {len(list(nir_dir.glob('*')))} 파일")
print(f"  - cam1: {len(list(cam1_dir.glob('*.bmp')))} 파일")
print(f"  - cam2: {len(list(cam2_dir.glob('*.bmp')))} 파일")
print(f"  - cam3: {len(list(cam3_dir.glob('*.bmp')))} 파일")

print("\n💡 monitoring_app.py의 설정 다이얼로그에서 다음 경로를 입력하세요:")
print(f"  - Normal: {normal_dir.absolute()}")
print(f"  - NIR: {nir_dir.absolute()}")
print(f"  - Cam1: {cam1_dir.absolute()}")
print(f"  - Cam2: {cam2_dir.absolute()}")
print(f"  - Cam3: {cam3_dir.absolute()}")
