# drag_select_widget.py 문서

## 개요
드래그로 여러 행을 선택할 수 있는 컨테이너 위젯입니다. 마우스 드래그를 통해 연속된 행의 체크박스를 일괄 선택/해제할 수 있습니다.

**파일 경로**: `script/ui/drag_select_widget.py`  
**파일 크기**: 78 라인  
**총 클래스**: 1개 (`DragSelectWidget`)  
**총 메서드**: 7개 (public 3개 + private 2개 + override 3개)  
**작성일**: 2025-12-04

---

## 사용 목적

**문제**: 많은 그룹 행에서 여러 개를 선택하려면 하나씩 체크박스를 클릭해야 함  
**해결**: 마우스 드래그로 연속된 행을 한 번에 선택 가능

---

## 클래스: DragSelectWidget

QWidget을 상속한 드래그 선택 컨테이너

### 초기화

#### `__init__(parent=None)`
- **설명**: 드래그 선택 위젯 초기화
- **매개변수**:
  - `parent`: MainWindow 인스턴스 (필수)
- **초기화 속성**:
  - `self.main_window`: MainWindow 참조
  - `self.drag_start_pos`: 드래그 시작 위치 (QPoint)
  - `self.drag_start_row`: 드래그 시작 행 인덱스 (int)

---

### 마우스 이벤트 오버라이드

#### `mousePressEvent(event)`
- **설명**: 마우스 버튼 눌림 이벤트 처리
- **동작**:
  1. 좌클릭 버튼인지 확인
  2. 클릭한 위치의 자식 위젯 확인
  3. **체크박스, 버튼이면 드래그 시작 안 함** (기본 동작 유지)
  4. 이미지나 빈 공간 클릭 시 드래그 시작
  5. 시작 위치와 행 인덱스 저장
- **특징**: 체크박스와 버튼은 기존 기능 유지

---

#### `mouseMoveEvent(event)`
- **설명**: 마우스 이동 이벤트 처리 (드래그 중)
- **동작**:
  1. 드래그가 시작되었는지 확인
  2. 현재 마우스 위치의 행 인덱스 확인
  3. 시작 행부터 현재 행까지 범위 계산
  4. 범위 내 모든 행 선택 (`_select_rows_in_range()` 호출)
- **특징**: 실시간으로 선택 범위가 시각적으로 변경됨

---

#### `mouseReleaseEvent(event)`
- **설명**: 마우스 버튼 놓음 이벤트 처리
- **동작**:
  1. 좌클릭 버튼 놓임 확인
  2. 드래그 상태 초기화
  3. `drag_start_pos`와 `drag_start_row`를 None으로 리셋
- **특징**: 드래그 종료 시 선택 범위 확정

---

### Private 메서드

#### `_get_row_at_pos(pos) -> int | None`
- **설명**: 주어진 위치에 있는 행의 인덱스를 반환
- **매개변수**:
  - `pos` (QPoint): 마우스 위치
- **반환값**: 행 인덱스 (int) 또는 None (해당 위치에 행 없음)
- **동작**:
  1. `main_window.scroll_layout`에서 모든 위젯 순회
  2. 각 위젯의 Y 좌표 범위 확인
  3. 마우스 Y 좌표가 위젯 범위 내에 있으면 해당 인덱스 반환
- **특징**: 보이는 위젯만 검사 (`widget.isVisible()`)

---

#### `_select_rows_in_range(start_idx, end_idx)`
- **설명**: 지정된 범위의 행들을 선택
- **매개변수**:
  - `start_idx` (int): 시작 행 인덱스
  - `end_idx` (int): 끝 행 인덱스
- **동작**:
  1. `main_window.scroll_layout`에서 모든 위젯 순회
  2. 각 위젯에 `row_select` 체크박스가 있는지 확인
  3. 범위 내 행 (`start_idx <= i <= end_idx`)이면 체크
  4. 범위 외 행은 체크 해제
- **특징**: 
  - 범위 밖의 행도 자동으로 해제됨
  - `row_select.setChecked(should_select)` 사용

---

## 사용 예시 (MainWindow)

```python
from ui.drag_select_widget import DragSelectWidget

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # 드래그 선택 위젯 생성
        self.scroll_content = DragSelectWidget(self)
        
        # 스크롤 영역에 추가
        self.scroll_area = QScrollArea()
        self.scroll_area.setWidget(self.scroll_content)
        
        # 행 위젯 레이아웃 설정
        self.scroll_layout = QVBoxLayout(self.scroll_content)
        
        # 그룹 행 추가
        for group in groups:
            row_widget = self.create_row_widget(group)
            self.scroll_layout.addWidget(row_widget)
```

---

## 동작 방식

### 1. 드래그 시작
- 이미지나 빈 공간 클릭 시 시작
- 체크박스, 버튼 클릭 시 기본 동작 유지

### 2. 드래그 중
- 마우스 이동에 따라 실시간으로 선택 범위 변경
- 위에서 아래 또는 아래에서 위 드래그 모두 지원

### 3. 드래그 종료
- 마우스 버튼 놓으면 선택 범위 확정
- 드래그 상태 초기화

---

## 주의사항

1. **MainWindow 참조 필수**: `parent`로 MainWindow를 전달해야 함
2. **scroll_layout 필수**: MainWindow에 `scroll_layout` 속성 필요
3. **row_select 필수**: 각 행 위젯에 `row_select` 체크박스 속성 필요

---

## Phase 5 분리 (2025-11-30)

### 이전 (monitoring_app.py 내부)
- MainWindow 클래스 내부에 드래그 선택 로직 포함
- 2800+ 줄의 거대한 파일

### 이후 (drag_select_widget.py 분리)
- 독립적인 재사용 가능한 위젯
- 약 78줄의 간결한 모듈
- 다른 프로젝트에서도 사용 가능
