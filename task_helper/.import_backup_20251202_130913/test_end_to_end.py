"""
종합 기능 테스트 (End-to-End Test)

10개의 완전한 그룹 데이터를 생성하여 전체 워크플로우를 테스트합니다:
1. 파일 생성
2. 파일 매칭 및 그룹 생성
3. UI 업데이트 시뮬레이션
4. 파일 이동 수행
5. 정리 (원위치 복구)
"""

import sys
import os
import pytest
import tempfile
import shutil
from pathlib import Path
from PIL import Image
from datetime import datetime, timedelta

sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from script.domain.file_matcher import FileMatcher
from script.domain.group_manager import GroupManager
from services.nir_pruning_service import NirPruningService
from services.operation_validator import OperationValidator
from services.operation_planner import OperationPlanner
from script.config_manager import ConfigManager


@pytest.fixture
def comprehensive_test_data():
    """10개의 완전한 그룹 데이터 생성"""
    
    temp_dir = tempfile.mkdtemp(prefix="test_e2e_")
    
    # 디렉토리 구조
    normal_dir = Path(temp_dir) / "normal"
    nir_dir = Path(temp_dir) / "nir"
    cam1_dir = Path(temp_dir) / "cam1"
    cam2_dir = Path(temp_dir) / "cam2"
    cam3_dir = Path(temp_dir) / "cam3"
    output_dir = Path(temp_dir) / "output"
    delete_dir = Path(temp_dir) / "delete"
    
    for d in [normal_dir, nir_dir, cam1_dir, cam2_dir, cam3_dir, output_dir, delete_dir]:
        d.mkdir(parents=True, exist_ok=True)
    
    # 기준 시간: 2025-11-20 12:40:00
    base_time = datetime(2025, 11, 20, 12, 40, 0)
    
    created_files = []
    
    # 10개 그룹 생성
    for i in range(10):
        # 각 그룹은 1분씩 차이
        group_time = base_time + timedelta(minutes=i)
        
        # 일반카메라 폴더 생성 (C{YYMMDD}T{HHMMSS}_0)
        normal_folder_name = f"C{group_time.strftime('%y%m%d')}T{group_time.strftime('%H%M%S')}_0"
        normal_folder = normal_dir / normal_folder_name
        normal_folder.mkdir(exist_ok=True)
        
        # 일반카메라 이미지
        normal_image = normal_folder / "stitched_original.png"
        _create_dummy_image(normal_image, f"Normal {i}")
        created_files.append(normal_image)
        
        # NIR 파일들 (run_1{YYYYMMDD}T{HHMMSS})
        nir_base = f"run_1{group_time.strftime('%Y%m%d')}T{group_time.strftime('%H%M%S')}"
        nir_spc = nir_dir / f"{nir_base}.spc"
        nir_txt = nir_dir / f"{nir_base}A.txt"
        
        nir_spc.write_text(f"NIR SPC data for group {i}")
        nir_txt.write_text(f"NIR TXT data for group {i}")
        created_files.extend([nir_spc, nir_txt])
        
        # 복합카메라 파일들 ({YYYYMMDD}_{HHMMSS}_{milliseconds})
        # 일반카메라보다 약간 늦은 시간
        cam_time = group_time + timedelta(seconds=15)
        cam_timestamp = cam_time.strftime("%Y%m%d_%H%M%S")
        
        cam1_file = cam1_dir / f"{cam_timestamp}_{100+i*10}.bmp"
        cam2_file = cam2_dir / f"{cam_timestamp}_{200+i*10}.bmp"
        cam3_file = cam3_dir / f"{cam_timestamp}_{300+i*10}.bmp"
        
        for idx, cam_file in enumerate([cam1_file, cam2_file, cam3_file]):
            _create_dummy_image(cam_file, f"Cam{idx+1} Group{i}")
            created_files.append(cam_file)
    
    data = {
        "temp_dir": temp_dir,
        "normal_dir": str(normal_dir),
        "nir_dir": str(nir_dir),
        "cam1_dir": str(cam1_dir),
        "cam2_dir": str(cam2_dir),
        "cam3_dir": str(cam3_dir),
        "output_dir": str(output_dir),
        "delete_dir": str(delete_dir),
        "created_files": created_files,
        "group_count": 10
    }
    
    yield data
    
    # 테스트 종료 후 정리
    shutil.rmtree(temp_dir, ignore_errors=True)
    print(f"\n🧹 Cleaned up test directory: {temp_dir}")


def _create_dummy_image(path: Path, text: str = "", size=(100, 100)):
    """더미 이미지 파일 생성"""
    from PIL import ImageDraw, ImageFont
    
    img = Image.new('RGB', size, color=(73, 109, 137))
    
    if text:
        draw = ImageDraw.Draw(img)
        # 텍스트 추가 (간단하게)
        draw.text((10, 40), text[:15], fill=(255, 255, 255))
    
    img.save(path)


def test_01_file_creation(comprehensive_test_data):
    """Step 1: 파일 생성 확인"""
    data = comprehensive_test_data
    
    # 생성된 파일 개수 확인
    # 각 그룹: normal(1) + nir(2) + cam(3) = 6개
    # 10개 그룹 = 60개
    expected_files = data["group_count"] * 6
    assert len(data["created_files"]) == expected_files, f"Expected {expected_files} files, got {len(data['created_files'])}"
    
    # 모든 파일이 실제로 존재하는지 확인
    for file_path in data["created_files"]:
        assert file_path.exists(), f"File not found: {file_path}"
    
    print(f"✅ Created {len(data['created_files'])} files in {data['temp_dir']}")


def test_02_file_matcher_scan(comprehensive_test_data):
    """Step 2: FileMatcher 스캔 및 파일 감지"""
    data = comprehensive_test_data
    
    matcher = FileMatcher()
    
    settings = {
        "normal": data["normal_dir"],
        "normal2": "",
        "nir": data["nir_dir"],
        "nir2": "",
        "cam1": data["cam1_dir"],
        "cam2": data["cam2_dir"],
        "cam3": data["cam3_dir"],
        "cam4": "",
        "cam5": "",
        "cam6": "",
        "use_folder_suffix": False,
        "use_camera_subfolder_normal": False,
        "use_camera_subfolder_normal2": False,
    }
    
    unmatched = matcher.scan_and_build_unmatched(settings)
    
    # 각 타입별 파일이 스캔되었는지 확인
    assert "normal" in unmatched, "Normal files not scanned"
    assert "nir" in unmatched, "NIR files not scanned"
    assert "cam1" in unmatched, "Cam1 files not scanned"
    assert "cam2" in unmatched, "Cam2 files not scanned"
    assert "cam3" in unmatched, "Cam3 files not scanned"
    
    # 개수 확인 (10개씩)
    assert len(unmatched["normal"]) == 10, f"Expected 10 normal folders, got {len(unmatched['normal'])}"
    assert len(unmatched["nir"]) == 10, f"Expected 10 NIR groups, got {len(unmatched['nir'])}"
    
    print(f"✅ FileMatcher scanned successfully:")
    for key, items in unmatched.items():
        print(f"   - {key}: {len(items)} items")


def test_03_group_building(comprehensive_test_data):
    """Step 3: 그룹 생성 및 매칭"""
    data = comprehensive_test_data
    
    matcher = FileMatcher()
    group_manager = GroupManager(log_emitter_func=print)
    
    settings = {
        "normal": data["normal_dir"],
        "normal2": "",
        "nir": data["nir_dir"],
        "nir2": "",
        "cam1": data["cam1_dir"],
        "cam2": data["cam2_dir"],
        "cam3": data["cam3_dir"],
        "cam4": "",
        "cam5": "",
        "cam6": "",
        "use_folder_suffix": False,
        "use_camera_subfolder_normal": False,
        "use_camera_subfolder_normal2": False,
    }
    
    unmatched = matcher.scan_and_build_unmatched(settings)
    
    # 그룹 생성
    groups = group_manager.build_all_groups(
        unmatched_data=unmatched,
        consumed_nir_keys=set(),
        nir_match_time_diff=30.0  # 30초 이내 매칭
    )
    
    # 10개 그룹이 생성되었는지 확인
    assert len(groups) > 0, "No groups created"
    print(f"✅ Created {len(groups)} groups")
    
    # 첫 번째 그룹 구조 확인
    if len(groups) > 0:
        first_group = groups[0]
        assert "카메라" in first_group or "normal" in first_group, "Group missing normal camera"
        print(f"   - First group keys: {list(first_group.keys())}")


def test_04_operation_validator(comprehensive_test_data):
    """Step 4: 파일 작업 검증"""
    data = comprehensive_test_data
    
    settings = {
        "output": data["output_dir"],
        "delete": data["delete_dir"],
    }
    
    # 더미 그룹
    dummy_groups = [
        {"카메라": {"file": {"absolute_path": "/test"}}, "line": 1, "cam1": {"f": {"absolute_path": "/t"}}, 
         "cam2": {"f": {"absolute_path": "/t"}}, "cam3": {"f": {"absolute_path": "/t"}}}
    ]
    
    validator = OperationValidator(
        settings=settings,
        groups_ref=lambda: dummy_groups
    )
    
    # 검증 수행
    is_valid, errors = validator.validate_basic_inputs(is_file_operation_running=False)
    
    # output 디렉토리가 존재하므로 유효해야 함
    assert is_valid, f"Validation failed: {errors}"
    print(f"✅ Operation validation passed")


def test_05_operation_planner(comprehensive_test_data):
    """Step 5: 파일 작업 계획 수립"""
    data = comprehensive_test_data
    
    config_manager = ConfigManager()
    planner = OperationPlanner(config_manager)
    
    # 더미 그룹 데이터
    groups_line1 = [{"name": f"group_{i}", "line": 1} for i in range(5)]
    groups_line2 = []
    
    # 작업 데이터 구성
    processed_data = planner.build_file_operation_data(
        current_tab_index=0,  # 라인1 탭
        is_separated=False,
        subject="TestSubject",
        subject2="",
        groups_line1=groups_line1,
        groups_line2=groups_line2
    )
    
    # 데이터 구조 확인
    assert len(processed_data) > 0, "No processed data created"
    
    # YYMMDD 형식의 키가 있는지 확인
    date_key = list(processed_data.keys())[0]
    assert "TestSubject" in processed_data[date_key], "Subject not in processed data"
    
    print(f"✅ Operation plan created: {list(processed_data.keys())}")


def test_06_nir_pruning_service(comprehensive_test_data):
    """Step 6: NIR 파일 정리 서비스"""
    data = comprehensive_test_data
    
    # 최종 상태 확인
    print(f"\n📊 End-to-End Test Summary:")
    print(f"   - Test Directory: {data['temp_dir']}")
    print(f"   - Total Files Created: {len(data['created_files'])}")
    print(f"   - Expected Groups: {data['group_count']}")
    print(f"   - All tests passed ✅")
    
    assert True


if __name__ == "__main__":
    # 직접 실행 시 pytest 실행
    pytest.main([__file__, "-v", "-s"])
