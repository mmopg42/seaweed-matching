# image_registry.py 문서

## 개요
이미지-위젯 매핑 관리 (Registry Pattern)를 담당하는 모듈입니다. 특정 이미지가 변경되었을 때 해당 이미지를 표시하는 모든 위젯을 O(1) 시간에 찾아 업데이트할 수 있습니다.

**파일 경로**: `script/image/image_registry.py`  
**파일 크기**: 165 라인  
**총 클래스**: 1개 (`ImageRegistry`)  
**총 메서드**: 10개  
**성능**: O(N²) → O(1) 최적화 적용  
**업데이트**: 2025-12-04

---

## 클래스: ImageRegistry

이미지-위젯 매핑 관리 클래스

### 핵심 개념

**Registry Pattern**: 이미지 경로를 키로 하여 해당 이미지를 표시하는 위젯들을 빠르게 조회할 수 있도록 양방향 매핑을 유지합니다.

**최적화 효과**:
- 이전: 전체 위젯 순회 → O(N²)
- 현재: 레지스트리 조회 → O(1)

---

### 초기화

#### `__init__(max_cache_items=300, pixmap_cache=None)`
- **설명**: 이미지 레지스트리 초기화
- **매개변수**:
  - `max_cache_items`: QPixmap 메모리 캐시 최대 항목 수 (기본: 300)
  - `pixmap_cache`: 기존 LruPixmapCache 인스턴스 (재사용 가능)
- **초기화 항목**:
  - `image_path_to_widgets`: 이미지 경로 → 위젯 리스트 (defaultdict)
  - `widget_to_image_path`: 위젯 → 이미지 경로 (dict)
  - `pixmap_cache`: LRU 캐시 (주입 또는 생성)
  - `_placeholder_pixmap`: 플레이스홀더 캐시

---

### 위젯 등록/해제

#### `register_widget(widget, image_path: str)`
- **설명**: 위젯을 특정 이미지 경로에 등록
- **매개변수**:
  - `widget`: 이미지를 표시하는 위젯
  - `image_path`: 이미지 파일 경로
- **동작**:
  1. 기존 경로에서 위젯 제거
  2. 새 경로에 위젯 등록
  3. 양방향 매핑 업데이트
- **용도**: 위젯의 이미지가 변경될 때마다 호출

#### `unregister_widget(widget)`
- **설명**: 위젯을 레지스트리에서 완전히 제거
- **매개변수**: `widget` - 제거할 위젯
- **용도**: 위젯 삭제 시 호출하여 메모리 누수 방지

---

### 조회 및 업데이트

#### `get_widgets_for_image(image_path: str) -> list`
- **설명**: 특정 이미지 경로의 모든 위젯 조회
- **매개변수**: `image_path` - 이미지 파일 경로
- **반환값**: 해당 이미지를 표시하는 위젯 리스트
- **시간 복잡도**: O(1)

#### `refresh_single_image(image_path: str, pixmap)`
- **설명**: 특정 이미지 경로만 찾아서 즉시 업데이트
- **매개변수**:
  - `image_path`: 이미지 파일 경로
  - `pixmap`: QPixmap 객체
- **동작**:
  1. 레지스트리에서 위젯 리스트 조회
  2. 각 위젯의 `set_image()` 메서드 호출
- **최적화**: O(N²) → O(1) 개선

---

### 캐시 관리

#### `get_cached_pixmap(path: str)`
- **설명**: 캐시에서 QPixmap 조회
- **매개변수**: `path` - 이미지 파일 경로
- **반환값**: QPixmap 또는 None

#### `set_cached_pixmap(path: str, pixmap)`
- **설명**: 캐시에 QPixmap 저장
- **매개변수**:
  - `path`: 이미지 파일 경로
  - `pixmap`: QPixmap 객체

#### `clear_cache()`
- **설명**: 모든 캐시 초기화
- **동작**: pixmap 캐시 및 플레이스홀더 모두 제거

---

### 플레이스홀더

#### `get_placeholder_pixmap(width=200, height=150)`
- **설명**: 로딩 중 플레이스홀더 이미지 반환
- **개선**: 크기별 캐싱 지원 (Dictionary 사용)
- **매개변수**:
  - `width`: 플레이스홀더 너비 (기본: 200)
  - `height`: 플레이스홀더 높이 (기본: 150)
- **반환값**: QPixmap (회색 배경 + "로딩 중..." 텍스트)

### 캐시 관리 (권장 메서드)

#### `get_pixmap(path: str) -> QPixmap or None`
- **설명**: 메모리 캐시에서 QPixmap 조회
- **권장**: `get_cached_pixmap()` 대신 사용

#### `set_pixmap(path: str, pixmap: QPixmap)`
- **설명**: 메모리 캐시에 QPixmap 저장
- **권장**: `set_cached_pixmap()` 대신 사용

#### `has_pixmap(path: str) -> bool`
- **설명**: 캐시에 해당 경로의 pixmap이 있는지 확인



---

## 데이터 구조

### 이미지 경로 → 위젯 매핑
```python
image_path_to_widgets = {
    "/path/to/image1.jpg": [widget1, widget2, widget3],
    "/path/to/image2.png": [widget4],
    ...
}
```

### 위젯 → 이미지 경로 매핑 (역방향)
```python
widget_to_image_path = {
    widget1: "/path/to/image1.jpg",
    widget2: "/path/to/image1.jpg",
    widget3: "/path/to/image1.jpg",
    widget4: "/path/to/image2.png",
    ...
}
```

---

## 사용 예시

### 초기화
```python
from image_registry import ImageRegistry

# 새로 생성
registry = ImageRegistry(max_cache_items=500)

# 기존 캐시 재사용
from utils import LruPixmapCache
pixmap_cache = LruPixmapCache(max_items=300)
registry = ImageRegistry(pixmap_cache=pixmap_cache)
```

### 위젯 등록
```python
# 위젯 생성 시
image_label = QLabel()
registry.register_widget(image_label, "/path/to/image.jpg")

# 이미지 변경 시
registry.register_widget(image_label, "/path/to/new_image.jpg")
```

### 이미지 업데이트
```python
# 이미지 로드 완료 시
pixmap = QPixmap("/path/to/image.jpg")
registry.refresh_single_image("/path/to/image.jpg", pixmap)
# → 해당 경로를 사용하는 모든 위젯 즉시 업데이트
```

### 캐싱
```python
# 캐시에서 조회
pixmap = registry.get_pixmap("/path/to/image.jpg")
if pixmap is None:
    # 로드 필요
    pixmap = QPixmap("/path/to/image.jpg")
    registry.set_pixmap("/path/to/image.jpg", pixmap)

# 플레이스홀더 사용
placeholder = registry.get_placeholder_pixmap(width=300, height=200)
image_label.setPixmap(placeholder)
```

---

## MainWindow와의 통합

```python
class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # 기존 캐시 재사용
        self.pixmap_cache = LruPixmapCache(max_items=500)
        
        # ImageRegistry 생성
        self.image_registry = ImageRegistry(
            pixmap_cache=self.pixmap_cache
        )
        
        # 하위 호환성 (기존 코드가 직접 접근하는 경우)
        self.image_path_to_widgets = self.image_registry.image_path_to_widgets
        self.widget_to_image_path = self.image_registry.widget_to_image_path
    
    def on_image_loaded(self, image_path, pixmap):
        # 캐시에 저장
        self.image_registry.set_pixmap(image_path, pixmap)
        
        # 모든 관련 위젯 즉시 업데이트
        self.image_registry.refresh_single_image(image_path, pixmap)
```

---

## 성능 비교

### O(N²) 방식 (이전)
```python
# 전체 위젯 순회
for layout in all_layouts:
    for i in range(layout.count()):
        widget = layout.itemAt(i).widget()
        if widget.image_path == image_path:
            widget.set_image(pixmap)
```

### O(1) 방식 (현재)
```python
# 레지스트리 조회
widgets = registry.get_widgets_for_image(image_path)
for widget in widgets:
    widget.set_image(pixmap)
```

**개선 효과**:
- 100개 위젯, 10개 이미지 업데이트
- 이전: 1,000번 비교
- 현재: 10번 조회 + 해당 위젯만 업데이트

---

## 의존성
- `collections.defaultdict`: 기본값을 가진 딕셔너리
- `utils.LruPixmapCache`: LRU 캐시 구현
- `PySide6.QtGui`: QPixmap, QPainter, QColor, QFont
- `PySide6.QtCore`: Qt

---

## 주의사항

1. **위젯 삭제 시**: 반드시 `unregister_widget()` 호출하여 메모리 누수 방지
2. **캐시 공유**: 여러 컴포넌트가 같은 `pixmap_cache`를 공유할 수 있음
3. **플레이스홀더**: 싱글톤 패턴으로 한 번만 생성

---

## 향후 개선 사항

1. **자동 등록**: 위젯 생성 시 자동 등록 데코레이터
2. **약한 참조**: WeakSet 사용하여 자동 가비지 컬렉션
3. **이벤트 시스템**: 이미지 변경 시 자동 알림
