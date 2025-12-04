# image_loader.py 문서

## 개요
이미지 비동기 로딩 시스템입니다. QThread로 백그라운드 로딩을 수행하고, 디스크 캐싱 및 썸네일 최적화로 성능을 향상시킵니다.

**파일 경로**: `script/image/image_loader.py`  
**파일 크기**: 391 라인  
**총 클래스**: 2개 (`ThumbnailCache`, `ImageLoaderWorker`)  
**총 함수**: 1개 (`prefetch_images`)  
**업데이트**: 2025-12-04

---

## 🔥 최신 변경사항 (2025-12-03)

### 1. Qt 프레임워크 변경
- **PyQt6 → PySide6**: Signal import 변경

---

## ThumbnailCache 클래스
디스크 기반 썸네일 캐시 관리

### 속성
- `cache_dir`: 캐시 디렉토리 경로
- `max_cache_size`: 최대 캐시 크기 (바이트)

### 메서드

#### 초기화
```python
__init__(self, cache_dir: str, max_cache_size_mb: int = 500)
```
- 캐시 디렉토리 생성
- 최대 캐시 크기 설정 (MB → 바이트)

#### 캐시 경로 생성
```python
_get_cache_path(self, image_path: str, size: Tuple[int, int]) -> str
```
이미지 경로와 크기를 기반으로 캐시 파일 경로 생성
- 파일명: `<hash>_<width>x<height>.jpg`
- 해시: MD5(이미지 절대 경로)

#### 캐시 조회
```python
get(self, image_path: str, size: Tuple[int, int]) -> bytes or None
```
- 캐시 파일 존재 확인
- 원본 파일 수정 시간 비교 (캐시 무효화)
- **Returns**: JPEG 데이터 또는 None

#### 캐시 저장
```python
set(self, image_path: str, size: Tuple[int, int], jpeg_data: bytes)
```
썸네일을 JPEG로 캐시에 저장

#### 캐시 정리
```python
_cleanup_if_needed(self)
```
캐시 크기가 제한 초과 시 오래된 파일 삭제
- 파일 수정 시간 기준 정렬
- 가장 오래된 것부터 삭제

---

## ImageLoaderWorker 클래스
비동기 이미지 로더 워커 (QThread)

### 시그널
```python
image_loaded = Signal(str, object, str)  # (path, pixmap, request_id)
```

### 속성
- `cache`: ThumbnailCache 인스턴스
- `use_disk_cache`: 디스크 캐시 사용 여부
- `request_queue`: 우선순위 큐 (PriorityQueue)
- `executor`: ThreadPoolExecutor (병렬 로딩)
- `running`: 실행 상태

### 메서드

#### 초기화
```python
__init__(self, cache_dir: str, max_workers: int = None, use_disk_cache: bool = True)
```
- 캐시 초기화
- 스레드 풀 생성 (max_workers=4)
- 큐 생성

#### 이미지 요청
```python
request_image(
    self,
    image_path: str,
    size: Tuple[int, int],
    request_id: str = "",
    priority: int = 5
)
```
이미지 로딩 요청
- `priority`: 낮을수록 우선 (0=최고, 10=최저)
- 우선순위 큐에 추가

#### 워커 중지
```python
stop(self)
```
- 큐에 None 추가 (종료 신호)
- 스레드 종료 대기

#### 메인 루프
```python
run(self)
```
백그라운드 스레드 메인 루프
1. 큐에서 요청 꺼내기
2. ThreadPoolExecutor로 병렬 로딩
3. 완료된 작업 수집
4. `image_loaded` 시그널 발생

#### 이미지 로딩
```python
_load_image(self, image_path: str, size: Tuple[int, int], request_id: str) -> tuple
```
실제 이미지 로딩 로직
1. 디스크 캐시 확인
2. 캐시 없으면 디코딩 (Pillow 또는 Qt)
3. 캐시 저장
4. **Returns**: `(pixmap, request_id)`

#### Pillow를 사용한 로딩
```python
_load_with_pil(self, image_path: str, size: Tuple[int, int]) -> QPixmap
```
최적화된 썸네일 로딩
- `draft()` 모드: 디코딩 단계에서 축소
- 메모리 사용량 대폭 감소
- with 문으로 파일 핸들 즉시 해제

#### Qt를 사용한 로딩
```python
_load_with_qt(self, image_path: str, size: Tuple[int, int]) -> QPixmap
```
폴백 로딩 (Pillow 없을 때)
- 원본 크기로 로드 후 축소
- 메모리 사용량 높음

#### 캐시 저장
```python
_save_to_cache(self, pixmap: QPixmap, image_path: str, size: Tuple[int, int])
```
QPixmap을 JPEG로 변환하여 캐시에 저장

---

## 헬퍼 함수

### prefetch_images
```python
prefetch_images(loader: ImageLoaderWorker, image_paths: list, size: Tuple[int, int])
```
여러 이미지를 미리 로딩 요청
- 우선순위: 10 (낮음, 백그라운드 프리페치)

---

## 사용 예시

### 워커 생성 및 시작
```python
cache_dir = "C:/Users/.../thumbnail_cache"
loader = ImageLoaderWorker(cache_dir, max_workers=4, use_disk_cache=True)
loader.image_loaded.connect(on_image_loaded)
loader.start()
```

### 이미지 요청
```python
loader.request_image(
    "e:/data/image.jpg",
    (200, 150),
    request_id="row_5",
    priority=0  # 최고 우선순위
)
```

### 이미지 로드 완료 처리
```python
def on_image_loaded(path, pixmap, request_id):
    # pixmap을 UI에 표시
    thumbnail_widget.set_image(pixmap)
```

### 프리페치
```python
visible_images = ["path1.jpg", "path2.jpg", "path3.jpg"]
prefetch_images(loader, visible_images, (200, 150))
```

### 워커 중지
```python
loader.stop()
loader.wait()
```

---

## 캐시 구조

### 디렉토리
```
<cache_dir>/
├─ a1b2c3d4_200x150.jpg
├─ e5f6g7h8_200x150.jpg
├─ i9j0k1l2_300x200.jpg
└─ ...
```

### 파일명 생성
```python
hash = hashlib.md5(absolute_path.encode()).hexdigest()[:8]
filename = f"{hash}_{width}x{height}.jpg"
```

---

## 성능 최적화

1. **디스크 캐싱**: 썸네일을 디스크에 저장하여 재로딩 시간 단축
2. **Pillow draft 모드**: 디코딩 단계에서 축소하여 메모리 절약
3. **병렬 로딩**: ThreadPoolExecutor로 여러 이미지 동시 로딩
4. **우선순위 큐**: 중요한 이미지 먼저 로딩
5. **캐시 크기 제한**: 오래된 캐시 자동 삭제 (500MB 기본값)

---

## 메모리 관리

### Pillow 사용 시
```
원본 이미지 (4000x3000, 12MB)
  → draft() 모드로 200x150 디코딩
  → 메모리 사용: ~90KB
```

### Qt 사용 시 (폴백)
```
원본 이미지 (4000x3000, 12MB)
  → 전체 이미지 로드 (12MB)
  → 축소 (200x150)
  → 메모리 사용: ~12MB (로딩 시)
```

---

## 캐시 무효화

### 원본 파일 수정 시
```python
if cache_mtime < original_mtime:
    # 캐시 무효화, 재로딩
```

### 캐시 크기 초과 시
```python
if total_size > max_cache_size:
    # 오래된 파일부터 삭제
```

---

## 의존성
- `PySide6.QtCore`: QThread, Signal
- `PySide6.QtGui`: QPixmap, QImage
- `Pillow (PIL)`: 최적화된 이미지 디코딩 (선택적)
- `concurrent.futures`: 병렬 처리
- `queue.PriorityQueue`: 우선순위 큐
- `hashlib`: 캐시 파일명 해싱
- `pathlib.Path`: 경로 처리
