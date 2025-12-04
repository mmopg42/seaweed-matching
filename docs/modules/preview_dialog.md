# preview_dialog.py 문서

## 개요
이미지 미리보기 다이얼로그를 제공하는 모듈입니다. QPixmap을 전체 화면으로 표시하며, 클릭 시 닫히는 간단한 이미지 뷰어입니다.

**파일 경로**: `script/ui/dialogs/preview_dialog.py`  
**파일 크기**: 56 라인  
**총 클래스**: 1개 (`PreviewDialog`)  
**의존성**: PySide6  
**업데이트**: 2025-12-04

---

## 클래스: PreviewDialog

이미지 미리보기 다이얼로그 (QDialog 상속)

### 초기화

#### `__init__(pixmap: QPixmap, title: str = "미리보기", parent=None)`
- **설명**: 미리보기 다이얼로그 초기화
- **매개변수**:
  - `pixmap`: 표시할 QPixmap 이미지
  - `title`: 윈도우 제목 (기본: "미리보기")
  - `parent`: 부모 위젯 (선택적)
- **동작**:
  1. QLabel을 중앙 정렬로 생성
  2. QVBoxLayout에 라벨 배치
  3. 이벤트 필터 설치 (라벨 클릭 감지)
  4. 기본 크기 설정 (900x700)
  5. 이미지를 창 크기에 맞게 스케일링

---

### 메서드

#### `eventFilter(obj, event)`
- **설명**: 이벤트 필터 (라벨 클릭 감지)
- **동작**: 라벨을 왼쪽 버튼으로 클릭하면 다이얼로그 닫기 (`accept()`)
- **반환값**: True (이벤트 처리됨), False (기본 처리)

#### `mousePressEvent(e)`
- **설명**: 마우스 클릭 이벤트 처리
- **동작**: 왼쪽 버튼 클릭 시 다이얼로그 닫기 (`accept()`)

#### `resizeEvent(e)`
- **설명**: 창 크기 변경 이벤트 처리
- **동작**: 창 크기가 변경되면 이미지 재스케일링

#### `_update_scaled()`
- **설명**: 이미지를 창 크기에 맞게 스케일링
- **동작**:
  1. 현재 창 크기의 95% 계산
  2. 비율 유지하며 스케일링 (`KeepAspectRatio`)
  3. 부드러운 변환 적용 (`SmoothTransformation`)
  4. 라벨에 스케일링된 이미지 설정

---

## 주요 특징

### 1. 반응형 크기 조정
- 창 크기 변경 시 자동으로 이미지 재스케일링
- 비율 유지하며 최대한 크게 표시
- 창 크기의 95% 사용 (여백 확보)

### 2. 간편한 닫기
- 이미지 클릭 → 다이얼로그 닫기
- 배경 클릭 → 다이얼로그 닫기
- ESC 키 → 주석 처리됨 (필요시 활성화 가능)

### 3. 고품질 렌더링
- `SmoothTransformation` 사용으로 부드러운 스케일링
- 비율 유지 (`KeepAspectRatio`)
- 중앙 정렬

---

## 사용 예시

### 기본 사용
```python
from preview_dialog import PreviewDialog
from PySide6.QtGui import QPixmap

# QPixmap 로드
pixmap = QPixmap("/path/to/image.jpg")

# 미리보기 다이얼로그 표시
dialog = PreviewDialog(pixmap, title="이미지 미리보기")
result = dialog.exec()

if result == QDialog.DialogCode.Accepted:
    print("사용자가 이미지를 클릭하여 닫음")
```

### MainWindow에서 사용
```python
def show_image_preview(self, thumb_pixmap, image_path):
    # 원본 이미지 로드
    pixmap = QPixmap(image_path)
    
    if pixmap.isNull():
        QMessageBox.warning(self, "오류", "이미지를 로드할 수 없습니다")
        return
    
    # 미리보기 표시
    dialog = PreviewDialog(pixmap, title=f"미리보기: {os.path.basename(image_path)}")
    dialog.exec()
```

### 썸네일 클릭 시 호출
```python
class MonitorRow(QWidget):
    def __init__(self, main_window):
        self.main_window = main_window
        
        # 썸네일 라벨
        self.thumbnail = QPushButton()
        self.thumbnail.clicked.connect(self.on_thumbnail_clicked)
    
    def on_thumbnail_clicked(self):
        if self.image_path:
            self.main_window.show_image_preview(
                self.thumbnail.icon().pixmap(200, 200),
                self.image_path
            )
```

---

## 레이아웃 구조

```
PreviewDialog (QDialog)
  └─ QVBoxLayout (margin: 6px)
      └─ QLabel (중앙 정렬)
          └─ QPixmap (스케일링됨)
```

---

## 이벤트 흐름

### 생성 시
```
__init__()
  → setWindowTitle()
  → QLabel 생성 및 정렬
  → 레이아웃 설정
  → installEventFilter()
  → resize(900, 700)
  → _update_scaled()
      → QPixmap.scaled(0.95 * size)
      → setPixmap()
```

### 창 크기 변경 시
```
사용자가 창 크기 조정
  → resizeEvent()
      → _update_scaled()
          → QPixmap.scaled(0.95 * new_size)
          → setPixmap()
```

### 클릭 시
```
사용자가 이미지/배경 클릭
  → mousePressEvent() 또는 eventFilter()
      → LeftButton 확인
      → accept()
      → 다이얼로그 닫힘
```

---

## 설정 옵션

### 기본 크기
- 너비: 900px
- 높이: 700px

### 여백
- 컨텐츠 마진: 6px (상하좌우)

### 이미지 스케일 비율
- 창 크기의 95% (0.95)
- 변경 가능: `sz = self.size() * 0.95`

---

## 의존성
- `PySide6.QtWidgets`: QDialog, QVBoxLayout, QLabel
- `PySide6.QtCore`: Qt, QEvent
- `PySide6.QtGui`: QPixmap

---

## 주의사항

1. **메모리**: 큰 이미지는 메모리를 많이 사용할 수 있습니다
2. **파일 핸들**: 원본 QPixmap은 `_orig`에 저장되어 리사이징 시 재사용됩니다
3. **Null 체크**: QPixmap이 null인지 확인 후 사용하세요

---

## 향후 개선 사항

1. **확대/축소**: 마우스 휠로 확대/축소 지원
2. **드래그**: 큰 이미지일 때 드래그로 이동
3. **키보드 단축키**: 화살표 키로 다음/이전 이미지 탐색
4. **정보 표시**: 이미지 크기, 파일명 등 정보 오버레이
5. **ESC 키**: 주석 처리된 ESC 키 닫기 기능 활성화 고려

---

## ESC 키 닫기 (선택적)

현재 주석 처리된 기능:

```python
# def keyPressEvent(self, e):
#     # ESC로 닫기(선택)
#     if e.key() == Qt.Key.Key_Escape:
#         self.reject()
#     else:
#         super().keyPressEvent(e)
```

필요 시 주석을 제거하여 ESC 키로 다이얼로그를 닫을 수 있습니다.
