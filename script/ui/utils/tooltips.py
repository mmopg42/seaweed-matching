# tooltips.py
"""
UI 요소별 도움말 툴팁 텍스트 관리
이 모듈은 이제 infrastructure.localization을 사용합니다.
기존 코드와의 호환성을 위해 유지됩니다.
"""

import sys
from pathlib import Path

# infrastructure 모듈 경로 추가
script_root = Path(__file__).parent.parent.parent
if str(script_root) not in sys.path:
    sys.path.insert(0, str(script_root))

from infrastructure.localization import get_tooltip as _get_tooltip, TOOLTIPS

# 기존 코드와의 호환성을 위해 TOOLTIPS 딕셔너리와 get_tooltip 함수를 재내보냄
# TOOLTIPS는 localization 모듈에서 가져온 것을 사용

def get_tooltip(key: str) -> str:
    """
    키에 해당하는 툴팁 텍스트를 반환
    infrastructure.localization.get_tooltip()을 사용합니다.

    Args:
        key: 툴팁 키 (예: "btn_run", "btn_stop")

    Returns:
        툴팁 텍스트. 키가 없으면 빈 문자열 반환
    """
    return _get_tooltip(key)


def set_tooltip_enabled(widget, key: str, enabled: bool = True):
    """
    위젯에 툴팁을 설정하거나 제거

    Args:
        widget: PySide6 위젯
        key: 툴팁 키
        enabled: True면 툴팁 설정, False면 제거
    """
    if enabled:
        tooltip_text = get_tooltip(key)
        if tooltip_text:
            widget.setToolTip(tooltip_text)
    else:
        widget.setToolTip("")
