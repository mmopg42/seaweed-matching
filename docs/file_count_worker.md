# file_count_worker.py 문서

## 개요
파일 개수 카운트 전용 백그라운드 워커입니다. watchdog으로 변화를 감지하고, UI 업데이트와 독립적으로 작동합니다.

**파일 크기**: 10KB (251 라인)  
**총 클래스**: 2개  
**총 메서드**: 15개

---

## 클래스: CountFolderEventHandler

파일 시스템 변화 감지 핸들러

### 메서드

#### `__init__(worker)`
- watchdog 이벤트 핸들러 초기화
- worker 참조 저장

#### `on_any_event(event)`
- 파일 시스템 변화 감지 시 호출
- worker에 알림 (trigger_count)

---

## 클래스: FileCountWorker (QThread)

별도 스레드에서 파일 개수를 실시간으로 카운트하는 워커

### 시그널

```python
counts_updated = Signal(int, int, int, int, int, int, int, int, int, int)
# (nir_count, nir2_count, normal_count, normal2_count, 
#  cam1_count, cam2_count, cam3_count, cam4_count, cam5_count, cam6_count)
```

### 메서드

#### `__init__()`
- FileCountWorker 초기화
- 설정, 상태 변수 초기화

#### `update_settings(settings: dict)`
- **기능**: 폴더 경로 설정 업데이트 (스레드 안전)
- settings 딕셔너리에서 normal, nir, cam1~6 경로 저장

#### `get_effective_path(base_path: str, use_camera_subfolder: bool) -> str`
- **기능**: camera 하위폴더 옵션을 반영한 실제 검색 경로 계산
- `use_camera_subfolder=True`면 `base_path/camera` 반환
- `False`면 `base_path` 반환

#### `should_use_recursive_watch(folder_type: str) -> bool`
- **기능**: 폴더 타입에 따라 재귀 감시 여부 결정
- `normal`, `normal2`: camera 하위폴더 옵션에 따라 결정
- 나머지: False (단일 레벨 감시)

#### `trigger_count()`
- **기능**: 파일 개수 카운트 즉시 트리거
- `needs_count` 플래그 설정

#### `enable()`
- **기능**: 파일 개수 카운트 활성화
- watchdog 시작 및 카운트 트리거

#### `disable()`
- **기능**: 파일 개수 카운트 비활성화
- watchdog 중지 (렉 최소화)

#### `stop()`
- **기능**: 워커 종료
- watchdog 중지 및 스레드 종료 대기

#### `start_watchdog()`
- **기능**: watchdog 감시 시작
- 각 폴더별 Observer 생성 및 시작
- 재귀 감시 옵션 적용

#### `stop_watchdog()`
- **기능**: watchdog 감시 중지
- 모든 Observer 정지 및 정리

#### `run()`
- **기능**: 백그라운드 스레드 메인 루프
- 10초마다 또는 변화 감지 시 카운트 실행
- `counts_updated` 시그널 발생

#### `_count_nir_files(folder_path: str) -> int`
- **기능**: NIR 폴더 내 .spc 파일 개수만 카운트
- .spc 확장자 파일만 집계

#### `_count_folders(folder_path: str) -> int`
- **기능**: 폴더 내 직접 하위 폴더 개수 카운트
- `C`로 시작하는 폴더만 집계 (일반 카메라)

#### `_count_image_files(folder_path: str) -> int`
- **기능**: 폴더 내 이미지 파일 개수 카운트
- .jpg, .jpeg, .png, .bmp 파일 집계

---

## 동작 흐름

```
워커 시작 (enable)
  ↓
start_watchdog() - Observer 생성
  ↓
파일 변화 감지 → on_any_event → trigger_count
  ↓
run() 루프 - 10초마다 또는 needs_count=True 시
  ↓
_count_nir_files, _count_folders, _count_image_files 호출
  ↓
counts_updated 시그널 발생
  ↓
MainWindow에서 UI 업데이트
```

---

## 사용 예시

```python
from file_count_worker import FileCountWorker

# 생성
count_worker = FileCountWorker()
count_worker.counts_updated.connect(on_counts_updated)

# 설정 업데이트
count_worker.update_settings({
    "normal": "e:/data/normal",
    "nir": "e:/data/nir",
    "cam1": "e:/data/cam1",
    ...
})

# 시작
count_worker.start()
count_worker.enable()

# 콜백
def on_counts_updated(nir, nir2, normal, normal2, cam1, cam2, cam3, cam4, cam5, cam6):
    print(f"NIR: {nir}, Normal: {normal}")
```

---

## camera 하위폴더 모드

### use_camera_subfolder = True
```
base_path = "e:/data/normal"
effective_path = "e:/data/normal/camera"
재귀 감시: False (camera/ 직접 하위만)
```

### use_camera_subfolder = False
```
base_path = "e:/data/normal"
effective_path = "e:/data/normal"
재귀 감시: True (모든 하위 폴더)
```

---

## 의존성
- `PySide6.QtCore`: QThread, Signal
- `watchdog.observers`: Observer
- `watchdog.events`: FileSystemEventHandler
- `os`: 파일 시스템 작업
- `time`: 타이밍

---

## 특징

1. **UI 독립성**: UI 업데이트와 완전히 독립적으로 작동 (렉 없음)
2. **지능형 카운트**: 변화가 있을 때만 카운트, 10초 이상 변화 없으면 한 번 확인
3. **재귀 감시**: 폴더 타입에 따라 재귀 감시 여부 자동 결정
4. **스레드 안전**: 설정 업데이트 시 락 사용
