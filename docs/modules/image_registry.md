# image_registry.py 문서

## 개요
이미지-위젯 매핑 관리 (Registry Pattern)를 담당하는 모듈입니다. 특정 이미지가 변경되었을 때 해당 이미지를 표시하는 모든 위젯을 O(1) 시간에 찾아 업데이트할 수 있습니다.

**파일 경로**: `script/image/image_registry.py`  
**파일 크기**: ~180 라인  
**총 클래스**: 1개 (`ImageRegistry`)  
**총 메서드**: 11개 (+ `get_registry_stats`)  
**성능**: O(N²) → O(1) 최적화 적용  
**업데이트**: 2025-12-05

### 최근 변경사항 (2025-12-05)
- ✅ **get_registry_stats() 추가**: 레지스트리 상태 조회 메서드 추가 (디버깅용)
- ✅ **레지스트리 공유 지원**: `MainWindow`와 `ImageManager`가 동일한 인스턴스 공유 가능

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

#### `__init__(max_cache_items=300, pixmap_cache=None, memory_threshold_percent=80.0)`
- **설명**: 이미지 레지스트리 초기화
- **매개변수**:
  - `max_cache_items`: QPixmap 메모리 캐시 최대 항목 수 (기본: 300)
  - `pixmap_cache`: 기존 LruPixmapCache 인스턴스 (재사용 가능)
  - `memory_threshold_percent`: 메모리 임계값 (%, 기본: 80.0) (Task 12.2)
- **초기화 항목**:
  - `image_path_to_widgets`: 이미지 경로 → 위젯 리스트 (defaultdict)
  - `widget_to_image_path`: 위젯 → 이미지 경로 (dict)
  - `pixmap_cache`: LRU 캐시 (주입 또는 생성)
  - `_placeholder_pixmap`: 플레이스홀더 캐시
  - `memory_threshold_percent`: 메모리 임계값 (Task 12.2)
  - `psutil`: 메모리 모니터링 라이브러리 (Task 12.1)

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

#### `get_registry_stats() -> dict` (2025-12-05 추가)
- **설명**: 레지스트리 상태 반환 (디버깅용)
- **반환값**: 딕셔너리
  - `total_paths`: 등록된 이미지 경로 개수
  - `total_widgets`: 등록된 위젯 총 개수
  - `sample_paths`: 샘플 경로 리스트 (최대 5개)
- **용도**: 레지스트리가 올바르게 동작하는지 확인
- **예시**:
  ```python
  stats = self.image_registry.get_registry_stats()
  print(f"레지스트리: {stats['total_paths']}개 경로, {stats['total_widgets']}개 위젯")
  # 출력: 레지스트리: 380개 경로, 1900개 위젯
  ```

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

### 메모리 관리 (Task 12)

#### `check_and_adjust_cache_size(log_callback=None) -> tuple`
- **설명**: 메모리 사용량 확인 및 캐시 크기 자동 조절 (Task 12.2)
- **매개변수**:
  - `log_callback`: 로그 메시지를 전달할 콜백 함수 (선택)
- **반환값**: `(조절 발생 여부, 메모리 사용률, 경고 메시지)` 튜플
- **동작**:
  1. 시스템 메모리 사용률 확인
  2. 임계값(80%) 초과 시 캐시 크기를 50%로 축소
  3. LRU 정책으로 오래된 항목 제거
  4. 경고 메시지 생성 및 콜백 호출
- **용도**: 이미지 로딩 완료 시 자동 호출

#### `get_cache_size() -> int`
- **설명**: 현재 캐시 크기 반환 (Task 12.1)
- **반환값**: 캐시에 저장된 항목 수

#### `get_memory_usage_mb() -> float`
- **설명**: 캐시 메모리 사용량 추정 (Task 12.1)
- **반환값**: 추정 메모리 사용량 (MB)
- **계산**: 캐시 크기 × 평균 pixmap 크기 (50KB)

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
- `psutil`: 메모리 사용량 모니터링 (Task 12.1)

---

## 주의사항

1. **위젯 삭제 시**: 반드시 `unregister_widget()` 호출하여 메모리 누수 방지
2. **캐시 공유**: 여러 컴포넌트가 같은 `pixmap_cache`를 공유할 수 있음
3. **플레이스홀더**: 싱글톤 패턴으로 한 번만 생성

---

## 테스트

테스트 파일: `tests/test_memory_monitoring.py`

**테스트 커버리지:**
- 캐시 크기 축소 동작 (Task 12.2)
- 메모리 80% 초과 시 자동 조절 (Task 12.2)
- 메모리 정상 범위일 때 조절 안 함 (Task 12.2)
- LRU 정책으로 오래된 항목부터 제거 (Task 12.2)
- 경고 메시지 형식 확인 (Task 12.3)
- 로그 콜백 호출 확인 (Task 12.3)
- 대량 로딩 중 캐시 자동 조절 (Task 12.4)

## 향후 개선 사항

1. **자동 등록**: 위젯 생성 시 자동 등록 데코레이터
2. **약한 참조**: WeakSet 사용하여 자동 가비지 컬렉션
3. **이벤트 시스템**: 이미지 변경 시 자동 알림

## 버전 히스토리

- 2025-12-04: 초기 구현 (Registry Pattern)
- 2025-01-XX: 메모리 자동 조절 추가 (Task 12.2, 12.3 - 메모리 임계값 초과 시 캐시 축소)
