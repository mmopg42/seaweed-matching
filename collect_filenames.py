"""
이동된 파일 이름 수집 스크립트 (시료명별)

작업날짜 폴더에서 실행하여 각 시료명별로 이동된 파일 이름을 JSON으로 수집합니다.
각 시료명마다 개별 JSON 파일이 생성됩니다.

구조:
작업날짜폴더/
  ├── 시료명1/
  │   ├── with NIR/
  │   │   ├── Nir/
  │   │   ├── 일반/
  │   │   └── 복합 카메라/
  │   └── without NIR/
  │       ├── 일반 카메라/
  │       └── 복합 카메라/
  ├── 시료명2/
  └── ...

출력 파일:
  - collected_filenames_시료명1.json
  - collected_filenames_시료명2.json
  - ...

사용법:
  python collect_filenames.py [작업날짜폴더경로]

  예시:
  python collect_filenames.py "D:/매칭결과/250114"

  현재 디렉토리가 작업날짜 폴더인 경우:
  python collect_filenames.py
"""

import os
import json
import sys
from pathlib import Path
from datetime import datetime


def collect_files_in_directory(dir_path):
    """
    디렉토리 내의 모든 파일 이름을 수집 (재귀적)

    Args:
        dir_path: 디렉토리 경로

    Returns:
        list: 파일 이름 목록 (상대 경로)
    """
    files = []
    if not os.path.exists(dir_path):
        return files

    try:
        for root, dirs, filenames in os.walk(dir_path):
            for filename in filenames:
                # 상대 경로로 저장
                rel_path = os.path.relpath(os.path.join(root, filename), dir_path)
                files.append(rel_path.replace("\\", "/"))
    except Exception as e:
        print(f"경고: {dir_path} 읽기 실패 - {e}")

    return sorted(files)


def collect_subject_files(subject_path):
    """
    시료명 폴더에서 with NIR / without NIR 구분하여 파일 수집

    Args:
        subject_path: 시료명 폴더 경로

    Returns:
        dict: {
            "with_nir": {...},
            "without_nir": {...}
        }
    """
    subject_data = {
        "with_nir": {},
        "without_nir": {}
    }

    # with NIR 폴더 처리
    with_nir_path = os.path.join(subject_path, "with NIR")
    if os.path.exists(with_nir_path):
        # Nir 폴더
        nir_path = os.path.join(with_nir_path, "Nir")
        if os.path.exists(nir_path):
            subject_data["with_nir"]["nir"] = collect_files_in_directory(nir_path)

        # 일반 폴더
        normal_path = os.path.join(with_nir_path, "일반")
        if os.path.exists(normal_path):
            subject_data["with_nir"]["normal"] = collect_files_in_directory(normal_path)

        # 복합 카메라 폴더
        complex_path = os.path.join(with_nir_path, "복합 카메라")
        if os.path.exists(complex_path):
            subject_data["with_nir"]["complex_camera"] = {}
            for cam_folder in ["cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]:
                cam_path = os.path.join(complex_path, cam_folder)
                if os.path.exists(cam_path):
                    files = collect_files_in_directory(cam_path)
                    if files:
                        subject_data["with_nir"]["complex_camera"][cam_folder] = files

    # without NIR 폴더 처리
    without_nir_path = os.path.join(subject_path, "without NIR")
    if os.path.exists(without_nir_path):
        # 일반 카메라 폴더
        normal_camera_path = os.path.join(without_nir_path, "일반 카메라")
        if os.path.exists(normal_camera_path):
            subject_data["without_nir"]["normal_camera"] = collect_files_in_directory(normal_camera_path)

        # 복합 카메라 폴더
        complex_path = os.path.join(without_nir_path, "복합 카메라")
        if os.path.exists(complex_path):
            subject_data["without_nir"]["complex_camera"] = {}
            for cam_folder in ["cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]:
                cam_path = os.path.join(complex_path, cam_folder)
                if os.path.exists(cam_path):
                    files = collect_files_in_directory(cam_path)
                    if files:
                        subject_data["without_nir"]["complex_camera"][cam_folder] = files

    return subject_data


def collect_all_files(work_date_folder):
    """
    작업날짜 폴더에서 모든 시료명의 파일 수집하고 시료명별로 JSON 저장

    Args:
        work_date_folder: 작업날짜 폴더 경로

    Returns:
        dict: {
            "date": "작업날짜",
            "subject_count": 2,
            "total_files": 350,
            "saved_files": ["path1.json", "path2.json"]
        }
    """
    work_date_folder = os.path.abspath(work_date_folder)
    date_name = os.path.basename(work_date_folder)
    collected_at = datetime.now().isoformat()

    print(f"작업날짜 폴더: {work_date_folder}")
    print(f"작업날짜: {date_name}")
    print()

    # 작업날짜 폴더 내의 모든 시료명 폴더 탐색
    if not os.path.exists(work_date_folder):
        print(f"오류: 작업날짜 폴더가 존재하지 않습니다: {work_date_folder}")
        return {
            "date": date_name,
            "subject_count": 0,
            "total_files": 0,
            "saved_files": []
        }

    subject_count = 0
    total_files = 0
    saved_files = []

    for item in os.listdir(work_date_folder):
        item_path = os.path.join(work_date_folder, item)

        # 디렉토리만 처리 (시료명 폴더)
        if not os.path.isdir(item_path):
            continue

        # move_plan.json 같은 파일이 있는 폴더는 건너뛰기
        # (실제 시료명 폴더는 with NIR 또는 without NIR 폴더가 있어야 함)
        has_with_nir = os.path.exists(os.path.join(item_path, "with NIR"))
        has_without_nir = os.path.exists(os.path.join(item_path, "without NIR"))

        if not (has_with_nir or has_without_nir):
            continue

        subject_name = item
        print(f"처리 중: {subject_name}")

        subject_data = collect_subject_files(item_path)

        # 파일 개수 계산
        file_count = 0
        for category, category_data in subject_data.items():
            if isinstance(category_data, dict):
                for key, value in category_data.items():
                    if isinstance(value, list):
                        file_count += len(value)
                    elif isinstance(value, dict):
                        for subkey, subvalue in value.items():
                            if isinstance(subvalue, list):
                                file_count += len(subvalue)

        # 시료명별 JSON 저장
        subject_json = {
            "date": date_name,
            "subject": subject_name,
            "collected_at": collected_at,
            "file_count": file_count,
            "data": subject_data
        }

        # 파일명: collected_filenames_시료명.json
        output_filename = f"collected_filenames_{subject_name}.json"
        output_path = os.path.join(work_date_folder, output_filename)

        try:
            with open(output_path, "w", encoding="utf-8") as f:
                json.dump(subject_json, f, indent=2, ensure_ascii=False)
            saved_files.append(output_path)
            print(f"  - 파일 개수: {file_count}")
            print(f"  - 저장 완료: {output_filename}")
        except Exception as e:
            print(f"  - ❌ JSON 저장 실패: {e}")

        subject_count += 1
        total_files += file_count

    print()
    print(f"총 {subject_count}개 시료명 처리 완료")
    print(f"총 {total_files}개 파일 수집")

    return {
        "date": date_name,
        "subject_count": subject_count,
        "total_files": total_files,
        "saved_files": saved_files
    }


def main():
    """메인 함수"""
    # 명령줄 인자 처리
    if len(sys.argv) > 1:
        work_date_folder = sys.argv[1]
    else:
        # 현재 디렉토리 사용
        work_date_folder = os.getcwd()

    print("=" * 60)
    print("이동된 파일 이름 수집 스크립트 (시료명별)")
    print("=" * 60)
    print()

    # 파일 수집 및 시료명별 JSON 저장
    result = collect_all_files(work_date_folder)

    if result["subject_count"] == 0:
        print()
        print("경고: 수집된 시료명이 없습니다.")
        print("작업날짜 폴더 구조를 확인해주세요.")
        return

    # 결과 출력
    print()
    print("=" * 60)
    print(f"✅ 파일 이름 수집 완료!")
    print(f"시료명별 JSON 파일 {len(result['saved_files'])}개 생성:")
    for filepath in result['saved_files']:
        print(f"  - {os.path.basename(filepath)}")
    print("=" * 60)


if __name__ == "__main__":
    main()
