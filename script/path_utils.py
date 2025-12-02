# path_utils.py

import os
import re
from dataclasses import dataclass
from typing import Optional


# ========== 상수 정의 ==========
PATH_KEYS = ["normal", "normal2", "nir", "nir2", "cam1", "cam2", 
             "cam3", "cam4", "cam5", "cam6", "output", "delete"]

KEY_LABELS = {
    "normal": "일반 폴더",
    "normal2": "일반2 폴더",
    "nir": "NIR 폴더",
    "nir2": "NIR2 폴더",
    "cam1": "Cam1 폴더",
    "cam2": "Cam2 폴더",
    "cam3": "Cam3 폴더",
    "cam4": "Cam4 폴더",
    "cam5": "Cam5 폴더",
    "cam6": "Cam6 폴더",
    "output": "이동 대상 폴더",
    "delete": "삭제 폴더"
}


# ========== 데이터 클래스 ==========
@dataclass
class PathChange:
    """경로 변경 정보"""
    key: str        # 설정 키 (예: "normal", "nir")
    label: str      # UI 표시용 라벨 (예: "일반 폴더", "NIR 폴더")
    old_path: str   # 이전 경로
    new_path: str   # 새로운 경로


@dataclass
class FolderCreationResult:
    """폴더 생성 결과"""
    label: str          # 폴더 라벨
    path: str           # 폴더 경로
    error: Optional[str] = None  # 에러 메시지 (성공 시 None)


# ========== 경로 유틸리티 ==========
def get_effective_path(base_path: str, use_camera_subfolder: bool) -> str:
    """
    camera 하위폴더 옵션을 반영한 실제 검색 경로 계산
    
    Args:
        base_path: 기본 경로
        use_camera_subfolder: camera 하위폴더 사용 여부
    
    Returns:
        실제 검색할 경로 (camera 하위폴더 옵션 반영)
    """
    if not base_path:
        return ""
    
    if use_camera_subfolder:
        camera_path = os.path.join(base_path, "camera")
        return camera_path if os.path.isdir(camera_path) else base_path
    else:
        return base_path


# ========== 날짜 검증 ==========
def validate_date_format(date_str: str) -> tuple[bool, str]:
    """
    날짜 형식 검증 (YYYYMMDD)
    
    Args:
        date_str: 검증할 날짜 문자열
    
    Returns:
        (is_valid, error_message)
        - is_valid: 유효하면 True, 아니면 False
        - error_message: 에러 메시지 (유효하면 빈 문자열)
    """
    if not date_str:
        return False, "날짜가 입력되지 않았습니다."
    
    if len(date_str) != 8:
        return False, f"날짜는 8자리여야 합니다. (입력값: '{date_str}', 길이: {len(date_str)})"
    
    if not date_str.isdigit():
        return False, f"날짜는 숫자만 포함해야 합니다. (입력값: '{date_str}')"
    
    return True, ""


# ========== 경로 변경 계획 ==========
def plan_date_based_path_changes(settings: dict, new_date: str) -> list[PathChange]:
    """
    설정 경로들에서 날짜 패턴(YYYYMMDD)을 찾아 변경 계획을 생성합니다.
    
    Args:
        settings: 설정 딕셔너리
        new_date: 새로운 날짜 (YYYYMMDD 형식)
    
    Returns:
        PathChange 리스트 (변경할 경로가 없으면 빈 리스트)
    """
    # 8자리 연속 숫자를 찾는 정규식 패턴
    date_pattern = re.compile(r'\d{8}')
    changes = []
    
    for key in PATH_KEYS:
        old_path = settings.get(key, "")
        if not old_path:
            continue
        
        # 경로에 8자리 날짜 패턴이 있는지 확인
        if not date_pattern.search(old_path):
            continue
        
        # 경로에서 8자리 날짜 패턴을 찾아서 교체
        new_path = date_pattern.sub(new_date, old_path)
        
        if new_path != old_path:
            label = KEY_LABELS.get(key, key)
            changes.append(PathChange(
                key=key,
                label=label,
                old_path=old_path,
                new_path=new_path
            ))
    
    return changes


# ========== 폴더 생성 ==========
def create_folders_if_needed(path_changes: list[PathChange]) -> tuple[list[FolderCreationResult], list[FolderCreationResult]]:
    """
    경로 변경 리스트에서 존재하지 않는 폴더들을 생성합니다.
    
    Args:
        path_changes: PathChange 객체 리스트
    
    Returns:
        (created_list, failed_list)
        - created_list: 성공적으로 생성된 폴더 정보
        - failed_list: 생성 실패한 폴더 정보 (에러 메시지 포함)
    """
    created = []
    failed = []
    
    for change in path_changes:
        new_path = change.new_path
        
        # 이미 존재하는 폴더는 건너뜀
        if os.path.isdir(new_path):
            continue
        
        try:
            os.makedirs(new_path, exist_ok=True)
            created.append(FolderCreationResult(
                label=change.label,
                path=new_path
            ))
        except PermissionError as e:
            failed.append(FolderCreationResult(
                label=change.label,
                path=new_path,
                error=f"접근 권한 없음: {str(e)}"
            ))
        except OSError as e:
            failed.append(FolderCreationResult(
                label=change.label,
                path=new_path,
                error=f"OS 오류: {str(e)}"
            ))
        except Exception as e:
            failed.append(FolderCreationResult(
                label=change.label,
                path=new_path,
                error=f"예상치 못한 오류: {str(e)}"
            ))
    
    return created, failed


# ========== 기존 함수 (상수 사용하도록 업데이트) ==========
def get_normal_thumbnail_path(folder_key: str, data_folder_name: str, settings: dict) -> str:
    """
    일반카메라 데이터 폴더의 stitched_original.png 경로 반환

    Args:
        folder_key: "normal" 또는 "normal2"
        data_folder_name: 데이터 폴더명 (예: "C_20250101_120000")
        settings: 설정 딕셔너리

    Returns:
        str: stitched_original.png 절대 경로 또는 None
    """
    if not data_folder_name:
        return None

    # 기본 경로 가져오기
    base_path = settings.get(folder_key, "")
    if not base_path:
        return None
    
    # camera 하위폴더 옵션 체크
    use_subfolder_key = f"use_camera_subfolder_{folder_key}"
    use_camera_subfolder = settings.get(use_subfolder_key, False)
    
    # 실제 검색 경로 계산
    if use_camera_subfolder:
        search_path = os.path.join(base_path, "camera")
    else:
        search_path = base_path
    
    if not os.path.isdir(search_path):
        return None

    # 데이터 폴더 경로
    data_folder_path = os.path.join(search_path, data_folder_name)
    if not os.path.isdir(data_folder_path):
        return None

    # stitched_original.png 경로
    thumbnail_path = os.path.join(data_folder_path, "stitched_original.png")

    if os.path.exists(thumbnail_path) and os.path.isfile(thumbnail_path):
        return thumbnail_path

    return None


def extract_date_from_paths(settings: dict) -> str:
    """
    설정된 경로들에서 8자리 날짜 패턴(YYYYMMDD)을 추출합니다.
    여러 경로에서 발견되면 가장 많이 나타나는 날짜를 반환합니다.

    Args:
        settings: 설정 딕셔너리

    Returns:
        추출된 날짜 문자열 (YYYYMMDD) 또는 None
    """
    date_pattern = re.compile(r'\d{8}')
    date_counts = {}

    for key in PATH_KEYS:
        path = settings.get(key, "")
        if not path:
            continue

        # 경로에서 8자리 날짜 패턴 찾기
        matches = date_pattern.findall(path)
        for match in matches:
            date_counts[match] = date_counts.get(match, 0) + 1

    if not date_counts:
        return None

    # 가장 많이 나타나는 날짜 반환
    most_common_date = max(date_counts, key=date_counts.get)
    return most_common_date


def auto_update_paths_with_date(settings: dict, new_date: str) -> dict:
    """
    설정 경로들의 날짜 부분을 새로운 날짜로 자동 교체합니다.

    Args:
        settings: 설정 딕셔너리
        new_date: 새로운 날짜 (YYYYMMDD 형식)

    Returns:
        dict: 업데이트된 설정 딕셔너리
    """
    if not new_date or len(new_date) != 8:
        return settings
    
    # 기존 날짜 패턴 찾기
    old_date = extract_date_from_paths(settings)
    if not old_date:
        return settings
    
    # 모든 경로에서 날짜 교체
    updated_settings = settings.copy()
    
    for key in PATH_KEYS:
        path = settings.get(key, "")
        if path and old_date in path:
            updated_settings[key] = path.replace(old_date, new_date)
    
    return updated_settings
