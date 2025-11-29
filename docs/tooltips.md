# tooltips.py 간략 문서

## 개요
UI 요소별 도움말 툴팁 텍스트를 관리하는 모듈입니다.

**크기**: 6KB (118 라인)

## 상수

### TOOLTIPS (딕셔너리)
모든 UI 요소의 툴팁 텍스트를 저장
- 상단 툴바 버튼
- 입력 필드
- 통계 정보
- 설정 항목

## 함수

### get_tooltip(key: str) -> str
키에 해당하는 툴팁 텍스트 반환

### set_tooltip_enabled(widget, key: str, enabled: bool)
위젯에 툴팁 설정/제거

## 사용 예시
```python
from tooltips import set_tooltip_enabled

btn_run.setObjectName("btn_run")
set_tooltip_enabled(btn_run, "btn_run", True)
```
