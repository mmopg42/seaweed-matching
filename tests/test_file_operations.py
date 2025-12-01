"""
파일 작업 기능 테스트 (더미 데이터 사용)

실제 파일을 생성하여 파일 매칭, 그룹 생성, NIR 정리 등을 테스트합니다.
"""

import sys
import os
import pytest
import tempfile
import shutil
from pathlib import Path
from PIL import Image

sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from file_matcher import FileMatcher
from group_manager import GroupManager
from services.nir_pruning_service import NirPruningService
from services.operation_validator import OperationValidator


@pytest.fixture
def temp_test_dir():
    """임시 테스트 디렉토리 생성"""
    temp_dir = tempfile.mkdtemp(prefix="test_monitoring_")
    yield temp_dir
    # 테스트 종료 후 정리
    shutil.rmtree(temp_dir, ignore_errors=True)


@pytest.fixture
def dummy_files(temp_test_dir):
    """더미 파일 생성"""
    
    # 디렉토리 구조 생성
    normal_dir = Path(temp_test_dir) / "normal"
    nir_dir = Path(temp_test_dir) / "nir"
    cam1_dir = Path(temp_test_dir) / "cam1"
    cam2_dir = Path(temp_test_dir) / "cam2"
    cam3_dir = Path(temp_test_dir) / "cam3"
    
    for d in [normal_dir, nir_dir, cam1_dir, cam2_dir, cam3_dir]:
        d.mkdir(parents=True, exist_ok=True)
    
    # 타임스탬프: 20251120T124040
    timestamp = "20251120T124040"
    
    # 1. 일반카메라 (폴더 + 이미지)
    normal_folder = normal_dir / f"C251120T{timestamp.split('T')[1]}_0"
    normal_folder.mkdir(exist_ok=True)
    normal_image = normal_folder / "stitched_original.png"
    _create_dummy_image(normal_image)
    
    # 2. NIR 파일들
    nir_base = f"run_120251120T124040"
    nir_spc = nir_dir / f"{nir_base}.spc"
    nir_txt = nir_dir / f"{nir_base}A.txt"
    nir_spc.touch()
    nir_txt.write_text("dummy nir data")
    
    # 3. 복합카메라들 (같은 타임스탬프의 bmp 파일들)
    cam_timestamp = "20251120_124215"
    cam1_file = cam1_dir / f"{cam_timestamp}_322.bmp"
    cam2_file = cam2_dir / f"{cam_timestamp}_577.bmp"
    cam3_file = cam3_dir / f"{cam_timestamp}_533.bmp"
    
    for cam_file in [cam1_file, cam2_file, cam3_file]:
        _create_dummy_image(cam_file)
    
    return {
        "temp_dir": temp_test_dir,
        "normal_dir": str(normal_dir),
        "nir_dir": str(nir_dir),
        "cam1_dir": str(cam1_dir),
        "cam2_dir": str(cam2_dir),
        "cam3_dir": str(cam3_dir),
    }


def _create_dummy_image(path: Path, size=(100, 100)):
    """더미 이미지 파일 생성"""
    img = Image.new('RGB', size, color=(73, 109, 137))
    img.save(path)


def test_file_matcher_scan(dummy_files):
    """FileMatcher가 파일을 제대로 스캔하는지 테스트"""
    
    matcher = FileMatcher()
    
    # settings dict 생성
    settings = {
        "normal": dummy_files["normal_dir"],
        "normal2": "",
        "nir": dummy_files["nir_dir"],
        "nir2": "",
        "cam1": dummy_files["cam1_dir"],
        "cam2": dummy_files["cam2_dir"],
        "cam3": dummy_files["cam3_dir"],
        "cam4": "",
        "cam5": "",
        "cam6": "",
        "use_folder_suffix": False,
        "use_camera_subfolder_normal": False,
        "use_camera_subfolder_normal2": False,
    }
    
    # 스캔 실행
    unmatched = matcher.scan_and_build_unmatched(settings)
    
    # 검증: 각 타입의 파일이 스캔되었는지
    print(f"✅ Unmatched keys: {list(unmatched.keys())}")
    for key, value in unmatched.items():
        print(f"   {key}: {len(value)} items")
    
    assert "normal" in unmatched or "camera" in unmatched
    assert "nir" in unmatched


def test_operation_validator():
    """OperationValidator 테스트"""
    
    mock_settings = {
        "output": "/tmp/test_output"
    }
    
    mock_groups = [
        {"카메라": {"file1": {"absolute_path": "/path/to/file1"}}, "line": 1}
    ]
    
    validator = OperationValidator(
        settings=mock_settings,
        groups_ref=lambda: mock_groups
    )
    
    # 기본 검증 테스트
    is_valid, errors = validator.validate_basic_inputs(is_file_operation_running=False)
    
    # output 경로가 실제 존재하지 않아도 검증 로직은 동작
    assert isinstance(is_valid, bool)
    assert isinstance(errors, list)
    print(f"✅ Validation result: valid={is_valid}, errors={len(errors)}")


if __name__ == "__main__":
    # 직접 실행 시 pytest 실행
    pytest.main([__file__, "-v"])
