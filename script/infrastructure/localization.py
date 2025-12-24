# localization.py
"""
중앙화된 로컬라이징 관리 모듈
모든 UI 문자열을 한 곳에서 관리합니다.
"""

from typing import Dict

# 기본 언어는 한국어
_DEFAULT_LANGUAGE = "ko"

# 로컬라이즈된 문자열 딕셔너리
_STRINGS: Dict[str, Dict[str, str]] = {
    "ko": {
        # 툴팁 (기존 tooltips.py 통합)
        "tooltip_btn_settings": "프로그램 설정을 변경합니다.\n- 폴더 경로 설정\n- 이미지 크기 조정\n- 라인 모드 선택",
        "tooltip_btn_run": "폴더 감시를 시작합니다.\n감시 중에는 새 파일이 자동으로 감지됩니다.",
        "tooltip_btn_stop": "폴더 감시를 중지합니다.\n감시를 멈추고 수동으로 작업할 수 있습니다.",
        "tooltip_btn_refresh_rows": "현재 매칭된 그룹을 새로고침합니다.\n화면에 표시된 데이터를 최신 상태로 업데이트합니다.",
        "tooltip_btn_move": "매칭된 데이터를 이동 대상 폴더로 이동/복사합니다.\n- 복사: 원본 파일 유지\n- 이동: 원본 파일 삭제",
        "tooltip_btn_delete_rows": "선택한 행의 파일들을 삭제 폴더로 이동합니다.\n체크박스로 삭제할 항목을 선택할 수 있습니다.",
        "tooltip_btn_toggle_select": "모든 행의 선택 상태를 토글합니다.\n전체 선택 ↔ 전체 해제",
        "tooltip_combo_mode": "파일 작업 모드를 선택합니다.\n- 복사: 원본 파일 유지\n- 이동: 원본 파일 삭제 후 이동",
        "tooltip_today_edit": "작업 날짜를 입력합니다.\n예: 250129 (YY/MM/DD)",
        "tooltip_subject_folder_edit": "시료명(폴더명)을 입력합니다.\n이동 시 이 이름으로 폴더가 생성됩니다.",
        "tooltip_subject_folder_edit2": "라인2 시료명을 입력합니다.\n분리 모드에서만 사용됩니다.",
        "tooltip_nir_count_edit": "이동할 NIR 파일 개수를 제한합니다.\n0 = 전체 이동, N = 최신 N개만 이동",
        "tooltip_data_count_edit": "이동할 데이터 그룹 개수를 제한합니다.\n0 = 전체 이동, N = 최신 N개만 이동",
        
        # 통계 정보
        "label_nir_count": "현재 감지된 NIR 파일 개수",
        "label_nir2_count": "현재 감지된 NIR2 파일 개수 (라인2)",
        "label_normal_count": "현재 감지된 일반 카메라 폴더 개수",
        "label_normal2_count": "현재 감지된 일반2 카메라 폴더 개수 (라인2)",
        "label_cam1_count": "현재 감지된 CAM1 파일 개수 (라인1)",
        "label_cam2_count": "현재 감지된 CAM2 파일 개수 (라인1)",
        "label_cam3_count": "현재 감지된 CAM3 파일 개수 (라인1)",
        "label_cam4_count": "현재 감지된 CAM4 파일 개수 (라인2)",
        "label_cam5_count": "현재 감지된 CAM5 파일 개수 (라인2)",
        "label_cam6_count": "현재 감지된 CAM6 파일 개수 (라인2)",
        
        # 매칭 통계
        "label_total": "일반 카메라가 있는 총 그룹 수",
        "label_with": "NIR이 매칭된 그룹 수",
        "label_without": "NIR이 없는 그룹 수",
        "label_fail": "누락 발생 그룹 수 (일반 카메라 없음)",
        "label_total_line1": "라인1 총 그룹 수",
        "label_with_line1": "라인1 NIR 매칭 그룹 수",
        "label_without_line1": "라인1 NIR 미매칭 그룹 수",
        "label_fail_line1": "라인1 누락 발생 그룹 수",
        "label_total_line2": "라인2 총 그룹 수",
        "label_with_line2": "라인2 NIR 매칭 그룹 수",
        "label_without_line2": "라인2 NIR 미매칭 그룹 수",
        "label_fail_line2": "라인2 누락 발생 그룹 수",
        
        # 탭
        "tab_line1": "라인1 데이터만 표시합니다.\n일반 카메라(_0), NIR, CAM1~3",
        "tab_line2": "라인2 데이터만 표시합니다.\n일반2 카메라(_1), NIR2, CAM4~6",
        "tab_combined": "두 라인을 통합하여 표시합니다.\n왼쪽: 라인1, 오른쪽: 라인2",
        
        # 설정
        "setting_normal": "일반 카메라 폴더 경로 (라인1)\n_0으로 끝나는 폴더가 생성되는 위치",
        "setting_normal2": "일반2 카메라 폴더 경로 (라인2)\n_1으로 끝나는 폴더가 생성되는 위치",
        "setting_nir": "NIR 파일 폴더 경로 (라인1)\n.spc 파일이 저장되는 위치",
        "setting_nir2": "NIR2 파일 폴더 경로 (라인2)\n라인2의 .spc 파일이 저장되는 위치",
        "setting_cam1": "CAM1 파일 폴더 경로 (라인1)",
        "setting_cam2": "CAM2 파일 폴더 경로 (라인1)",
        "setting_cam3": "CAM3 파일 폴더 경로 (라인1)",
        "setting_cam4": "CAM4 파일 폴더 경로 (라인2)",
        "setting_cam5": "CAM5 파일 폴더 경로 (라인2)",
        "setting_cam6": "CAM6 파일 폴더 경로 (라인2)",
        "setting_output": "이동 대상 폴더 경로\n매칭된 데이터가 이동/복사될 위치",
        "setting_delete": "삭제 폴더 경로\n삭제된 파일이 이동될 위치",
        "setting_interval": "파일 감시 업데이트 간격 (초)\n너무 짧으면 CPU 부하 증가",
        "setting_img_width": "일반/복합 카메라 썸네일 너비 (픽셀)",
        "setting_img_height": "일반/복합 카메라 썸네일 높이 (픽셀)",
        "setting_nir_width": "NIR 정보 표시 영역 너비 (픽셀)",
        "setting_nir_height": "NIR 정보 표시 영역 높이 (픽셀)",
        "setting_line_mode": "라인 모드 선택\n- 통합: 두 라인을 하나의 시료로 처리\n- 분리: 각 라인을 독립적으로 처리",
        "setting_legacy_ui": "레거시 UI 모드\n이전 버전 스타일로 표시",
        "setting_show_tooltips": "도움말 표시\n마우스를 올렸을 때 설명을 표시합니다.",
        
        # 버튼 및 UI 요소
        "button_settings_save": "설정 저장",
        "button_settings_open_folder": "설정 폴더 열기",
        "button_monitoring_start": "▶ 모니터링 시작",
        "button_monitoring_stop": "■ 모니터링 중지",
        "button_log_clear": "로그 지우기",
        "button_main_run": "▶ 실행",
        "button_main_stop": "■ 종료",
        "button_nir_run": "▶ 실행",
        "button_nir_stop": "■ 종료",
        
        # 상태 메시지
        "status_stopped": "상태: 중지됨",
        "status_running": "상태: 실행 중",
        
        # 그룹 박스
        "group_monitoring_control": "모니터링 제어",
        "group_monitoring_log": "모니터링 로그",
        "group_monitoring_run": "모니터링 실행",
        "group_settings": "설정 패널",
        
        # 라벨
        "label_main_monitoring": "메인 모니터링:",
        "label_nir_monitoring": "NIR 모니터링:",
        
        # 정보 메시지
        "info_independent_windows": "각 모니터링을 독립된 창으로 실행합니다.\n창을 닫으면 해당 모니터링이 중지됩니다.",
        
        # 에러 메시지
        "error_loading_failed": "로딩 실패",
    },
    # 향후 영어 지원을 위한 공간
    "en": {
        # TODO: 영어 번역 추가
    }
}

# 현재 언어 설정
_current_language = _DEFAULT_LANGUAGE


def set_language(lang: str) -> None:
    """
    현재 언어를 설정합니다.
    
    Args:
        lang: 언어 코드 ("ko", "en" 등)
    """
    global _current_language
    if lang in _STRINGS:
        _current_language = lang
    else:
        raise ValueError(f"지원하지 않는 언어: {lang}")


def get_language() -> str:
    """
    현재 언어를 반환합니다.
    
    Returns:
        현재 언어 코드
    """
    return _current_language


def get_string(key: str, lang: str = None) -> str:
    """
    키에 해당하는 로컬라이즈된 문자열을 반환합니다.
    
    Args:
        key: 문자열 키
        lang: 언어 코드 (None이면 현재 언어 사용)
    
    Returns:
        로컬라이즈된 문자열. 키가 없으면 키 자체를 반환
    """
    target_lang = lang or _current_language
    
    # 현재 언어에서 찾기
    if target_lang in _STRINGS and key in _STRINGS[target_lang]:
        return _STRINGS[target_lang][key]
    
    # 기본 언어에서 찾기
    if _DEFAULT_LANGUAGE in _STRINGS and key in _STRINGS[_DEFAULT_LANGUAGE]:
        return _STRINGS[_DEFAULT_LANGUAGE][key]
    
    # 없으면 키 반환
    return key


def get_tooltip(key: str) -> str:
    """
    툴팁 키에 해당하는 문자열을 반환합니다.
    기존 tooltips.py와의 호환성을 위한 편의 메서드입니다.
    
    Args:
        key: 툴팁 키 (예: "btn_run", "btn_stop")
    
    Returns:
        툴팁 텍스트. 키가 없으면 빈 문자열 반환
    """
    # 기존 tooltips.py 키 형식 지원
    tooltip_key = f"tooltip_{key}"
    result = get_string(tooltip_key)
    
    # tooltip_ 접두사가 없으면 원래 키로도 시도
    if result == tooltip_key:
        result = get_string(key, "")
    
    return result if result and result != key else ""


# 기존 tooltips.py와의 호환성을 위한 딕셔너리
# 기존 코드가 tooltips.get_tooltip()을 사용할 수 있도록
TOOLTIPS = {
    key.replace("tooltip_", ""): value 
    for key, value in _STRINGS[_DEFAULT_LANGUAGE].items() 
    if key.startswith("tooltip_")
}



