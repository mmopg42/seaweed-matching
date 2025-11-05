# tooltips.py
"""
UI 요소별 도움말 툴팁 텍스트 관리
"""

TOOLTIPS = {
    # 상단 툴바 버튼
    "btn_settings": "프로그램 설정을 변경합니다.\n- 폴더 경로 설정\n- 이미지 크기 조정\n- 라인 모드 선택",
    "btn_run": "폴더 감시를 시작합니다.\n감시 중에는 새 파일이 자동으로 감지됩니다.",
    "btn_stop": "폴더 감시를 중지합니다.\n감시를 멈추고 수동으로 작업할 수 있습니다.",
    "btn_refresh_rows": "현재 매칭된 그룹을 새로고침합니다.\n화면에 표시된 데이터를 최신 상태로 업데이트합니다.",
    "btn_move": "매칭된 데이터를 이동 대상 폴더로 이동/복사합니다.\n- 복사: 원본 파일 유지\n- 이동: 원본 파일 삭제",
    "btn_delete_rows": "선택한 행의 파일들을 삭제 폴더로 이동합니다.\n체크박스로 삭제할 항목을 선택할 수 있습니다.",
    "btn_toggle_select": "모든 행의 선택 상태를 토글합니다.\n전체 선택 ↔ 전체 해제",

    # 모드 선택
    "combo_mode": "파일 작업 모드를 선택합니다.\n- 복사: 원본 파일 유지\n- 이동: 원본 파일 삭제 후 이동",

    # 입력 필드
    "today_edit": "작업 날짜를 입력합니다.\n예: 250129 (YY/MM/DD)",
    "subject_folder_edit": "시료명(폴더명)을 입력합니다.\n이동 시 이 이름으로 폴더가 생성됩니다.",
    "subject_folder_edit2": "라인2 시료명을 입력합니다.\n분리 모드에서만 사용됩니다.",
    "nir_count_edit": "이동할 NIR 파일 개수를 제한합니다.\n0 = 전체 이동, N = 최신 N개만 이동",
    "data_count_edit": "이동할 데이터 그룹 개수를 제한합니다.\n0 = 전체 이동, N = 최신 N개만 이동",

    # 통계 정보
    "lbl_nir_count": "현재 감지된 NIR 파일 개수",
    "lbl_nir2_count": "현재 감지된 NIR2 파일 개수 (라인2)",
    "lbl_normal_count": "현재 감지된 일반 카메라 폴더 개수",
    "lbl_normal2_count": "현재 감지된 일반2 카메라 폴더 개수 (라인2)",
    "lbl_cam1_count": "현재 감지된 CAM1 파일 개수 (라인1)",
    "lbl_cam2_count": "현재 감지된 CAM2 파일 개수 (라인1)",
    "lbl_cam3_count": "현재 감지된 CAM3 파일 개수 (라인1)",
    "lbl_cam4_count": "현재 감지된 CAM4 파일 개수 (라인2)",
    "lbl_cam5_count": "현재 감지된 CAM5 파일 개수 (라인2)",
    "lbl_cam6_count": "현재 감지된 CAM6 파일 개수 (라인2)",

    # 매칭 통계 (통합 모드)
    "lbl_total": "일반 카메라가 있는 총 그룹 수",
    "lbl_with": "NIR이 매칭된 그룹 수",
    "lbl_without": "NIR이 없는 그룹 수",
    "lbl_fail": "누락 발생 그룹 수 (일반 카메라 없음)",

    # 매칭 통계 (분리 모드 - 라인1)
    "lbl_total_line1": "라인1 총 그룹 수",
    "lbl_with_line1": "라인1 NIR 매칭 그룹 수",
    "lbl_without_line1": "라인1 NIR 미매칭 그룹 수",
    "lbl_fail_line1": "라인1 누락 발생 그룹 수",

    # 매칭 통계 (분리 모드 - 라인2)
    "lbl_total_line2": "라인2 총 그룹 수",
    "lbl_with_line2": "라인2 NIR 매칭 그룹 수",
    "lbl_without_line2": "라인2 NIR 미매칭 그룹 수",
    "lbl_fail_line2": "라인2 누락 발생 그룹 수",

    # 탭
    "tab_line1": "라인1 데이터만 표시합니다.\n일반 카메라(_0), NIR, CAM1~3",
    "tab_line2": "라인2 데이터만 표시합니다.\n일반2 카메라(_1), NIR2, CAM4~6",
    "tab_combined": "두 라인을 통합하여 표시합니다.\n왼쪽: 라인1, 오른쪽: 라인2",

    # MonitorRow
    "row_select": "이 행을 선택합니다.\n삭제 시 체크된 항목만 삭제됩니다.",
    "delete_btn": "이 그룹 전체를 삭제 폴더로 이동합니다.",
    "chk_norm": "일반 카메라 폴더 삭제 여부",
    "chk_nir": "NIR 파일 삭제 여부",
    "chk_cam": "복합 카메라 파일 삭제 여부",

    # 설정 다이얼로그
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
}

def get_tooltip(key: str) -> str:
    """
    키에 해당하는 툴팁 텍스트를 반환

    Args:
        key: 툴팁 키 (예: "btn_run", "btn_stop")

    Returns:
        툴팁 텍스트. 키가 없으면 빈 문자열 반환
    """
    return TOOLTIPS.get(key, "")

def set_tooltip_enabled(widget, key: str, enabled: bool = True):
    """
    위젯에 툴팁을 설정하거나 제거

    Args:
        widget: PyQt6 위젯
        key: 툴팁 키
        enabled: True면 툴팁 설정, False면 제거
    """
    if enabled:
        tooltip_text = get_tooltip(key)
        if tooltip_text:
            widget.setToolTip(tooltip_text)
    else:
        widget.setToolTip("")
