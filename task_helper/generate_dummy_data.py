"""
더미 데이터 생성 스크립트

실제 데이터 구조를 기반으로 최소한의 더미 데이터를 생성합니다.
- 이미지 파일은 1x1 픽셀의 유효한 이미지로 생성 (용량 최소화)
- 파일명과 개수는 실제 데이터와 동일
- 테스트용 최소 데이터셋
"""

import os
import shutil
from pathlib import Path
from PIL import Image


def create_minimal_image(file_path, format='PNG'):
    """1x1 픽셀의 최소 이미지 생성"""
    file_path.parent.mkdir(parents=True, exist_ok=True)

    # 1x1 검은색 이미지 생성
    img = Image.new('RGB', (1, 1), color='black')
    img.save(file_path, format=format)


def create_empty_file(file_path):
    """빈 파일 생성"""
    file_path.parent.mkdir(parents=True, exist_ok=True)
    file_path.touch()


def create_result_yml(file_path, timestamp):
    """최소한의 result.yml 생성"""
    file_path.parent.mkdir(parents=True, exist_ok=True)

    content = f"""stitching:
  total_stitching_ms: 1000.0
  overlap_computation_ms: 800.0
  image_blending_ms: 200.0
  image_count: 10.0
angle: 2.0
width: 100.0
height: 150.0
hole_ratio: 0.0
timestamp: '{timestamp}'
paths:
  original: dummy_path/stitched_original.png
  visualization: dummy_path/stitched_vision.png
  mosaic: dummy_path/stitched_mosaic.png
timing:
  focus_process_ms: 500.0
  size_calculation_ms: 1.0
  visualization_process_ms: 1.0
  mosaic_process_ms: 1.0
  metadata_generation_ms: 1.0
  save_original_ms: 100.0
  save_mosaic_ms: 1.0
  save_visualization_ms: 1.0
  total_processing_ms: 510.0
  total_saving_ms: 102.0
  all_process_time: 1000.0
  image_input_time: 300.0
"""

    file_path.write_text(content, encoding='utf-8')


def read_file_list(list_path):
    """파일 리스트 읽기"""
    if not list_path.exists():
        return []
    return [line.strip() for line in list_path.read_text(encoding='utf-8').splitlines() if line.strip()]


def generate_dummy_data():
    """더미 데이터 생성"""

    # 기준 경로
    script_path = Path(__file__).parent
    base_path = script_path.parent / "data" / "dummy"
    example_path = script_path.parent / "data" / "example"

    # 기존 더미 데이터 삭제 (있으면)
    if base_path.exists():
        import shutil
        shutil.rmtree(base_path)
        print(f"Removed existing dummy data: {base_path}")

    print(f"\nGenerating dummy data in: {base_path}\n")

    # 파일 리스트 읽기
    normal_list = read_file_list(script_path / "normal_list.txt")
    cam1_list = read_file_list(script_path / "cam1_list.txt")
    cam2_list = read_file_list(script_path / "cam2_list.txt")
    cam3_list = read_file_list(script_path / "cam3_list.txt")
    nir_list = read_file_list(script_path / "nir_list.txt")

    print(f"File counts from example data:")
    print(f"  - Normal folders: {len(normal_list)}")
    print(f"  - cam1 files: {len(cam1_list)}")
    print(f"  - cam2 files: {len(cam2_list)}")
    print(f"  - cam3 files: {len(cam3_list)}")
    print(f"  - nir files: {len(nir_list)}")
    print()

    total_files = 0

    # ===== 1. Normal 카메라 폴더 생성 =====
    print("=" * 50)
    print(f"Creating {len(normal_list)} Normal Camera Folders")
    print("=" * 50)

    for i, folder_name in enumerate(normal_list, 1):
        folder_path = base_path / "normal" / folder_name
        folder_path.mkdir(parents=True, exist_ok=True)

        # 원본 폴더 경로
        example_folder = example_path / "normal" / folder_name

        # images 폴더 생성 (빈 폴더)
        (folder_path / "images").mkdir(exist_ok=True)

        # result.yml 복사 (실제 파일에서)
        if (example_folder / "result.yml").exists():
            shutil.copy2(example_folder / "result.yml", folder_path / "result.yml")
            total_files += 1

        # 최소 이미지 파일들 생성
        create_minimal_image(folder_path / "ratio.png", format='PNG')
        create_minimal_image(folder_path / "stitched_original.png", format='PNG')
        total_files += 2

        # stats CSV 파일명 추출 후 생성
        stats_files = list(example_folder.glob("stats_*.csv"))
        if stats_files:
            stats_name = stats_files[0].name
            create_empty_file(folder_path / stats_name)
            total_files += 1

        if i % 10 == 0:
            print(f"  Created {i}/{len(normal_list)} folders...")

    print(f"✓ Created {len(normal_list)} normal camera folders")
    print()

    # ===== 2. cam1 폴더 생성 =====
    print("=" * 50)
    print(f"Creating {len(cam1_list)} cam1 Files")
    print("=" * 50)

    for i, filename in enumerate(cam1_list, 1):
        create_minimal_image(base_path / "cam1" / filename, format='BMP')
        total_files += 1
        if i % 10 == 0:
            print(f"  Created {i}/{len(cam1_list)} files...")

    print(f"✓ Created {len(cam1_list)} cam1 files")
    print()

    # ===== 3. cam2 폴더 생성 =====
    print("=" * 50)
    print(f"Creating {len(cam2_list)} cam2 Files")
    print("=" * 50)

    for i, filename in enumerate(cam2_list, 1):
        create_minimal_image(base_path / "cam2" / filename, format='BMP')
        total_files += 1
        if i % 10 == 0:
            print(f"  Created {i}/{len(cam2_list)} files...")

    print(f"✓ Created {len(cam2_list)} cam2 files")
    print()

    # ===== 4. cam3 폴더 생성 =====
    print("=" * 50)
    print(f"Creating {len(cam3_list)} cam3 Files")
    print("=" * 50)

    for i, filename in enumerate(cam3_list, 1):
        create_minimal_image(base_path / "cam3" / filename, format='BMP')
        total_files += 1
        if i % 10 == 0:
            print(f"  Created {i}/{len(cam3_list)} files...")

    print(f"✓ Created {len(cam3_list)} cam3 files")
    print()

    # ===== 5. NIR 폴더 생성 =====
    print("=" * 50)
    print(f"Creating {len(nir_list)} NIR Files")
    print("=" * 50)

    for i, filename in enumerate(nir_list, 1):
        create_empty_file(base_path / "nir" / filename)
        total_files += 1
        if i % 10 == 0:
            print(f"  Created {i}/{len(nir_list)} files...")

    print(f"✓ Created {len(nir_list)} nir files")
    print()

    print("=" * 50)
    print("Dummy Data Generation Complete!")
    print("=" * 50)
    print(f"\nLocation: {base_path.absolute()}")
    print(f"\nTotal files created: {total_files}")

    # 실제 크기 확인
    total_size = sum(f.stat().st_size for f in base_path.rglob('*') if f.is_file())
    print(f"Total size: {total_size / 1024:.1f} KB")


if __name__ == "__main__":
    generate_dummy_data()
