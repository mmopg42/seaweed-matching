# ui_components.py 문서

## 개요
사용자 인터페이스 컴포넌트 모음입니다. 설정 다이얼로그, 레이아웃, 경로 입력 등 재사용 가능한 UI 요소들을 제공합니다.

**파일 경로**: `script/ui/components/ui_components.py`  
**파일 크기**: 738 라인  
**총 클래스**: 5개 (`FlowLayout_`, `PathLineEdit`, `SettingDialog`, `ImageWidget`, `MonitorRow`)  
**업데이트**: 2025-12-04

---

## 🔥 최신 변경사항 (2025-12-03)

### 1. Qt 프레임워크 변경
- **PyQt6 → PySide6**: 모든 import 변경

---

## 주요 클래스

### FlowLayout_
가로로 위젯을 배치하다가 공간이 부족하면 다음 줄로 넘어가는 플로우 레이아웃

#### 생성자
```python
__init__(parent=None, margin=0, spacing=6, max_spacing=None)
```
- `max_spacing`: 최대 간격 제한 (None이면 무제한)

#### 주요 메서드
- `addItem(item)`: 레이아웃 아이템 추가
- `addWidget(w)`: 위젯 추가
- `count()`: 아이템 개수 반환
- `itemAt(index)`: 특정 인덱스의 아이템 반환
- `takeAt(index)`: 아이템 제거 및 반환
- `expandingDirections()`: 확장 방향 반환
- `hasHeightForWidth()`: 너비에 따른 높이 계산 지원 여부
- `heightForWidth(width)`: 주어진 너비에 대한 높이 계산
- `setGeometry(rect)`: 레이아웃 지오메트리 설정
- `sizeHint()`: 권장 크기 반환
- `minimumSize()`: 최소 크기 반환
- `_do_layout(rect, test_only)`: 실제 레이아웃 계산 및 배치

**용도**: 통계 칩, 버튼 그룹 등을 유연하게 배치할 때 사용

---

### PathLineEdit
드래그 앤 드롭으로 경로를 입력할 수 있는 QLineEdit

#### 메서드
- `__init__(parent=None)`: 위젯 초기화
- `dragEnterEvent(event)`: 드래그 진입 이벤트 처리
- `dropEvent(event)`: 드롭 이벤트 처리 (파일/폴더 경로 추출)

**기능**:
- 파일 또는 폴더를 드래그하여 경로 자동 입력
- Windows 탐색기, 파일 다이얼로그에서 드래그 지원
- URL 형식 자동 변환 (`file:///` → 일반 경로)

---

### SettingDialog
프로그램 설정을 관리하는 다이얼로그

#### 설정 섹션

##### 1. 경로 설정
- **일반 카메라 (라인1)**: `normal`
- **일반2 카메라 (라인2)**: `normal2`
- **NIR 파일 (라인1)**: `nir`
- **NIR2 파일 (라인2)**: `nir2`
- **복합 카메라 1~3 (라인1)**: `cam1`, `cam2`, `cam3`
- **복합 카메라 4~6 (라인2)**: `cam4`, `cam5`, `cam6`
- **이동 대상 폴더**: `output`
- **삭제 폴더**: `delete`

각 경로 입력란에는 다음 버튼이 제공됩니다:
- **폴더 선택**: 다이얼로그로 폴더 선택
- **열기**: 탐색기에서 해당 폴더 열기

##### 2. UI 설정
- **일반/복합 카메라 썸네일 크기**: `img_width`, `img_height` (픽셀)
- **NIR 정보 표시 영역 크기**: `nir_width`, `nir_height` (픽셀)

##### 3. 작업 설정
- **스캔 간격**: `scan_interval` (초) - 파일 감시 업데이트 주기
- **라인 모드**: `line_mode`
  - `통합`: 라인1과 라인2를 하나의 시료로 처리
  - `분리`: 각 라인을 독립적으로 처리
- **camera 하위폴더 사용**: `use_camera_subfolder`
  - True: `/path/camera` 하위 검색
  - False: `/path` 직접 검색
- **복합카메라 시간 기반 매칭**: `use_cam_time_matching`
  - True: 일반카메라와 설정된 시간 차이 범위 내 매칭
  - False: 순차적으로 1:1 매칭
- **복합카메라 최소 시간 차이**: `cam_match_min_diff` (초, 기본: 4.0)
  - 일반카메라와 복합카메라의 최소 타임스탬프 차이
  - QDoubleValidator로 0.0~60.0 범위 검증
- **복합카메라 최대 시간 차이**: `cam_match_max_diff` (초, 기본: 6.0)
  - 일반카메라와 복합카메라의 최대 타임스탬프 차이
  - QDoubleValidator로 0.0~60.0 범위 검증
- **NIR 매칭 시간 차이**: `nir_match_time_diff` (초)
  - 일반카메라와 NIR의 타임스탬프 차이 허용 범위

#### 주요 메서드
- `__init__(parent=None)`: 다이얼로그 초기화
- `select_folder(edit_widget)`: 폴더 선택 다이얼로그 열기
- `open_folder(edit_widget)`: 탐색기에서 폴더 열기
  - **경로 정규화**: `os.path.normpath()`를 사용하여 OS에 맞게 경로 변환
  - 혼합된 슬래시(`/`, `\`)를 OS 표준 구분자로 통일
  - 중복된 슬래시 제거 (예: `C://folder\\path` → `C:\folder\path`)
- `get_settings()`: 현재 입력된 설정값 반환

**반환 형식**:
```python
{
    "normal": "path/to/folder",
    "nir": "path/to/folder",
    "img_width": 200,
    "img_height": 150,
    "scan_interval": 10.0,
    "line_mode": "통합",
    "use_camera_subfolder": False,
    "use_cam_time_mapping": True,
    "cam_match_min_diff": 4.0,
    "cam_match_max_diff": 6.0,
    "nir_match_time_diff": 1.0,
    ...
}
```

---

### ThumbnailWidget
이미지 썸네일을 표시하는 위젯

#### 주요 메서드
- `__init__(parent=None, width=200, height=150)`: 위젯 초기화
- `set_image(pixmap)`: QPixmap 이미지 설정
- `set_placeholder(text)`: 플레이스홀더 텍스트 설정 (이미지 없을 때)
- `clear()`: 이미지 제거
- `paintEvent(event)`: 이미지 그리기 (중앙 정렬, 비율 유지)

**특징**:
- 이미지 비율 유지하면서 위젯 크기에 맞게 스케일링
- 중앙 정렬
- 이미지 없을 시 플레이스홀더 텍스트 표시

---

### NirInfoWidget
NIR 파일 정보를 표시하는 위젯

#### 표시 정보
- NIR 파일명
- 타임스탬프
- 상태 (매칭됨/미매칭)

#### 주요 메서드
- `__init__(parent=None, width=200, height=150)`: 위젯 초기화
- `set_nir_info(nir_filename, timestamp)`: NIR 정보 설정
- `clear()`: 정보 제거
- `paintEvent(event)`: 정보 그리기

**스타일**:
- 배경색: 연한 노란색 (#ffffcc)
- 폰트: Consolas (고정폭)
- 레이아웃: 파일명과 타임스탬프를 세로로 배치

---

## 입력 검증

### QDoubleValidator
복합카메라 시간 범위 입력 필드에서 사용:
```python
validator = QDoubleValidator(0.0, 60.0, 2, self)
# 0.0 ~ 60.0 범위, 소수점 2자리
```

**적용 필드**:
- `cam_match_min_diff`: 최소 시간 차이 (초)
- `cam_match_max_diff`: 최대 시간 차이 (초)

**기능**:
- 숫자만 입력 가능
- 0.0 ~ 60.0 범위 제한
- 소수점 최대 2자리
- 플레이스홀더 텍스트: "초 단위 (예: 4.0)"

---

## 레이아웃 패턴

### 1. 경로 입력 레이아웃
```
[라벨] [PathLineEdit 입력란] [폴더 선택 버튼] [열기 버튼]
```

### 2. 플로우 레이아웃 (통계 칩)
```
[칩1] [칩2] [칩3] [칩4]
[칩5] [칩6] ...
```

### 3. 폼 레이아웃 (설정)
```
라벨1: [입력란1]
라벨2: [입력란2]
...
```

---

## 유틸리티 함수

### create_line_separator()
수평 구분선을 생성하여 반환
```python
line = QFrame()
line.setFrameShape(QFrame.Shape.HLine)
line.setFrameShadow(QFrame.Shadow.Sunken)
```

---

## 스타일 가이드

### 색상
- **배경**: `#f0f0f0` (밝은 회색)
- **NIR 정보 배경**: `#ffffcc` (연한 노란색)
- **구분선**: 시스템 기본 색상

### 폰트
- **일반 텍스트**: 시스템 기본
- **고정폭 텍스트**: Consolas

### 간격
- **기본 여백**: 8-10px
- **위젯 간 간격**: 6-8px
- **그룹 간 간격**: 16px

---

## 사용 예시

### SettingDialog 사용
```python
dialog = SettingDialog(parent=main_window)
if dialog.exec():
    settings = dialog.get_settings()
    # settings 적용
```

### PathLineEdit 사용
```python
path_edit = PathLineEdit()
path_edit.textChanged.connect(on_path_changed)
layout.addWidget(path_edit)
```

### ThumbnailWidget 사용
```python
thumbnail = ThumbnailWidget(width=200, height=150)
thumbnail.set_image(pixmap)
# 또는
thumbnail.set_placeholder("이미지 없음")
```

---

## 의존성
- `PySide6.QtWidgets`: Qt 위젯
- `PySide6.QtCore`: Qt 코어 기능
- `PySide6.QtGui`: Qt GUI 기능
- `subprocess`: 폴더 열기 (탐색기)
- `platform`: OS 플랫폼 감지
- `os`: 파일 시스템 작업
