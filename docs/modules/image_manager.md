# image_manager.py 문서

## 개요
이미지 로딩, 캐싱, UI 업데이트 관리 클래스입니다. MainWindow의 이미지 관련 책임을 분리한 매니저 클래스입니다.

**파일 경로**: `script/image/image_manager.py`  
**파일 크기**: 234 라인  
**총 클래스**: 1개 (`ImageManager`)  
**총 메서드**: 5개  
**업데이트**: 2025-12-04

---

## 설계 원칙

### 책임 분리
- **ImageManager**: 이미지 로딩 및 캐싱
- **ImageRegistry**: 이미지-위젯 매핑
- **ImageLoaderWorker**: 백그라운드 로딩
- **MainWindow**: UI 오케스트레이션

### 비동기 로딩
- Placeholder 즉시 표시
- 백그라운드 로딩 완료 시 UI 업데이트
- 우선순위 기반 큐

### Registry Pattern
- O(1) 이미지-위젯 빠른 매핑
- 단일 이미지 업데이트 시 해당 위젯만 갱신

---

## 클래스

### `ImageManager`

이미지 로딩, 캐싱, UI 업데이트 관리

#### 초기화

```python
manager = ImageManager(settings, image_loader, max_cache_items=500)
```

##### 매개변수
- `settings`: 애플리케이션 설정 딕셔너리
- `image_loader`: ImageLoaderWorker 인스턴스 (Optional)
- `max_cache_items`: 최대 캐시 항목 수 (기본 500)

##### 인스턴스 변수
- `settings`: 설정 참조
- `image_loader`: 백그라운드 로더
- `image_registry`: ImageRegistry 인스턴스

#### 예시
```python
# MainWindow.__init__()
self.image_manager = ImageManager(self.settings, None, max_cache_items=500)

# 이미지 로더 생성 후 연결
self.image_loader = ImageLoaderWorker(cache_dir=..., use_disk_cache=...)
self.image_loader.image_ready.connect(self.on_image_loaded)
self.image_loader.start()

self.image_manager.image_loader = self.image_loader
```

---

## 메서드

### `get_cached_pixmap(path: str, priority=5) → QPixmap`

비동기 이미지 로딩

#### 매개변수
- `path`: 이미지 파일 경로
- `priority`: 로딩 우선순위 (0=최고, 5=중간, 10=최저)

#### 반환값
- QPixmap 객체
  - 캐시에 있으면: 캐시된 이미지
  - 없으면: Placeholder (회색 "로딩 중...")

#### 동작
1. Registry 캐시 확인
2. 캐시 히트 → 즉시 반환
3. 캐시 미스:
   - Placeholder 반환
   - 백그라운드 로더에 요청
   - 로딩 완료 시 `on_image_loaded()` 콜백

#### 우선순위 가이드
- `0-2`: 긴급 (현재 보이는 행)
- `3-5`: 중간 (스크롤로 곧 보일 행)
- `6-10`: 낮음 (백그라운드 프리페치)

#### 예시
```python
# MonitorRow 생성 시
pixmap = self.get_cached_pixmap(image_path, priority=5)
row_widget.set_thumbnail(pixmap)
```

---

### `on_image_loaded(image_path: str, pixmap: QPixmap, request_id: str = "")`

이미지 로딩 완료 콜백

#### 매개변수
- `image_path`: 이미지 파일 경로
- `pixmap`: 로드된 QPixmap
- `request_id`: 요청 ID (사용 안 함)

#### 동작
1. Registry 캐시에 저장 (`register_image()`)
2. 해당 이미지를 사용하는 위젯 찾기
3. 위젯 즉시 업데이트 (`refresh_single_image()`)

#### 특징
- **디바운싱 제거**: 즉시 UI 갱신
- **Registry Pattern**: O(1) 위젯 탐색
- **부분 업데이트**: 변경된 위젯만 갱신

#### 예시
```python
# ImageLoaderWorker 시그널 연결
self.image_loader.image_ready.connect(self.image_manager.on_image_loaded)
```

---

### `refresh_single_image(image_path: str, pixmap: QPixmap)`

특정 이미지 경로만 찾아서 즉시 업데이트 (Registry Pattern 적용)

#### 매개변수
- `image_path`: 이미지 파일 경로
- `pixmap`: QPixmap 객체

#### 동작
1. Registry에서 해당 경로를 사용하는 위젯 리스트 조회
2. 각 위젯의 setPixmap() 호출

#### 최적화
- O(N²) → O(1) 개선
- 전체 스캔 없이 직접 위젯 접근

#### 예시
```python
# 수동 리프레시
pixmap = QPixmap("/path/to/image.png")
self.image_manager.refresh_single_image("/path/to/image.png", pixmap)
```

---

### `refresh_visible_images(all_layouts: list)`

화면에 표시된 행들의 이미지를 캐시에서 다시 로드하여 갱신

#### 매개변수
- `all_layouts`: `[(scroll_area, scroll_layout), ...]` 리스트

#### 동작
1. 각 레이아웃의 모든 행 순회
2. 행이 화면에 보이는지 확인 (`_is_row_visible()`)
3. 보이는 행의 이미지만 캐시에서 재로드
4. setPixmap() 호출

#### 호출 시점
- 새로고침 버튼 클릭 시
- 이미지 로딩 완료 시 (타이머를 통해, 현재는 즉시 갱신으로 대체)

#### 최적화
- 보이지 않는 행은 건너뜀 (성능 향상)

#### 예시
```python
all_layouts = [
    (self.scroll_area_line1, self.scroll_layout_line1),
    (self.scroll_area_line2, self.scroll_layout_line2),
    (self.scroll_area_combined_line1, self.scroll_layout_combined_line1),
    (self.scroll_area_combined_line2, self.scroll_layout_combined_line2),
]

self.image_manager.refresh_visible_images(all_layouts)
```

---

### `_is_row_visible(scroll_area: QScrollArea, row_widget: MonitorRow) → bool`

행이 화면(viewport)에 보이는지 확인

#### 매개변수
- `scroll_area`: QScrollArea 위젯
- `row_widget`: MonitorRow 위젯

#### 반환값
- `True`: 화면에 보임
- `False`: 화면에 안 보임

#### 동작
1. scroll_area의 viewport 가져오기
2. row_widget의 global 좌표 계산
3. viewport의 global 좌표 계산
4. 교차 영역 확인

#### 버퍼 마진
- 상하 100px 버퍼 (곧 스크롤할 영역 포함)

#### 예시
```python
if self.image_manager._is_row_visible(scroll_area, row_widget):
    # 보이는 행만 업데이트
    row_widget.update_thumbnail(pixmap)
```

---

### `show_image_preview(thumb_pixmap: QPixmap, image_path: str, parent=None)`

미리보기 다이얼로그 표시

#### 매개변수
- `thumb_pixmap`: 썸네일 QPixmap
- `image_path`: 이미지 파일 경로
- `parent`: 부모 위젯 (Optional)

#### 동작
1. 파일 존재 확인
2. PIL로 이미지 열기
3. BytesIO로 메모리 로드 (파일 핸들 즉시 해제)
4. QPixmap으로 변환
5. PreviewDialog 표시

#### 파일 핸들 해제
- PIL Image → BytesIO → QPixmap
- 원본 파일 즉시 닫음 (이동/삭제 가능)

#### 예시
```python
# MonitorRow 클릭 시
def on_image_clicked(self, pixmap, image_path):
    self.main_window.image_manager.show_image_preview(
        pixmap, 
        image_path, 
        parent=self.main_window
    )
```

---

## MainWindow와의 통합

### 초기화

```python
# MainWindow.__init__()
self.image_manager = ImageManager(self.settings, None, max_cache_items=500)

# 이미지 로더 생성 후 연결
thumbnail_cache_dir = os.path.join(self.config_manager.app_dir, "thumbnail_cache")
use_disk_cache = self.settings.get("use_disk_cache", True)
self.image_loader = ImageLoaderWorker(cache_dir=thumbnail_cache_dir, use_disk_cache=use_disk_cache)
self.image_loader.image_ready.connect(self.on_image_loaded)
self.image_loader.start()

self.image_manager.image_loader = self.image_loader
```

### 위임 메서드

```python
def get_cached_pixmap(self, path, priority=5):
    """비동기 이미지 로딩 - ImageManager에 위임"""
    return self.image_manager.get_cached_pixmap(path, priority)

def on_image_loaded(self, image_path: str, pixmap: QPixmap, request_id: str = ""):
    """이미지 로딩 완료 - ImageManager에 위임"""
    self.image_manager.on_image_loaded(image_path, pixmap, request_id)

def refresh_visible_images(self):
    """화면에 보이는 이미지 갱신 - ImageManager에 위임"""
    all_layouts = [
        (self.scroll_area_line1, self.scroll_layout_line1),
        (self.scroll_area_line2, self.scroll_layout_line2),
        (self.scroll_area_combined_line1, self.scroll_layout_combined_line1),
        (self.scroll_area_combined_line2, self.scroll_layout_combined_line2),
    ]
    self.image_manager.refresh_visible_images(all_layouts)

def show_image_preview(self, thumb_pixmap, image_path):
    """미리보기 다이얼로그 - ImageManager에 위임"""
    self.image_manager.show_image_preview(thumb_pixmap, image_path, parent=self)
```

### 사용 예시

```python
# MonitorRow 생성 시
def _update_row_widget(self, row_widget, group):
    # 썸네일 로드
    if thumbnail_path:
        pixmap = self.get_cached_pixmap(thumbnail_path, priority=5)
        row_widget.normal_thumbnail.setPixmap(pixmap)
    
    # NIR 스펙트럼
    if nir_path:
        pixmap = self.get_cached_pixmap(nir_path, priority=7)
        row_widget.nir_thumbnail.setPixmap(pixmap)
```

---

## 이미지 로딩 플로우

### 1. 초기 로드 (Placeholder)

```
MonitorRow 생성
    ↓
get_cached_pixmap(path)
    ↓
캐시 체크 (미스)
    ↓
Placeholder 반환 ("로딩 중...")
    ↓
백그라운드 요청 (ImageLoaderWorker)
```

### 2. 백그라운드 로딩

```
ImageLoaderWorker (별도 스레드)
    ↓
이미지 로드 (PIL)
    ↓
QPixmap 변환
    ↓
image_ready 시그널 발송
```

### 3. UI 갱신

```
on_image_loaded() 콜백
    ↓
Registry 캐시 저장
    ↓
위젯 찾기 (O(1))
    ↓
setPixmap() 즉시 업데이트
```

---

## 의존성

- `PySide6.QtGui.QPixmap`: 이미지 표시
- `PySide6.QtCore.QByteArray`: 바이트 변환
- `PySide6.QtWidgets.QApplication`: 이벤트 처리
- `image_registry.ImageRegistry`: 캐싱 및 매핑
- `image_loader.ImageLoaderWorker`: 백그라운드 로딩
- `preview_dialog.PreviewDialog`: 미리보기
- `PIL.Image`: 이미지 처리
- `io.BytesIO`: 메모리 버퍼

---

## 주의사항

1. **파일 핸들**: PIL → BytesIO로 즉시 해제
2. **우선순위**: 보이는 행은 높은 우선순위
3. **Registry**: 위젯 해제 시 unregister 필요

---

## 성능 특징

1. **Registry Pattern**: O(1) 위젯 탐색
2. **가시성 체크**: 보이는 행만 갱신
3. **비동기 로딩**: UI 블로킹 없음
4. **캐시**: 중복 로딩 방지

---

## 향후 개선 사항

1. **프리페칭**: 스크롤 예측 기반 선로딩
2. **압축**: WebP 등으로 메모리 절약
3. **스마트 캐시**: LRU + 빈도 기반 eviction
