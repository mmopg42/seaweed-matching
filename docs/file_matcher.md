# file_matcher.py 문서

## 개요
파일 시스템 감시 및 파일 매칭 로직을 담당하는 핵심 모듈입니다. watchdog를 사용하여 실시간으로 파일 변경을 감지하고, 각 파일 타입(NIR, 일반, 복합 카메라)을 매칭합니다.

**파일 크기**: 16KB (384 라인)  
**총 클래스/함수**: 23개

---

## 주요 클래스

### Communicate
파일 시스템 이벤트를 Qt 시그널로 전달하는 통신 클래스

#### 시그널
```python
file_changed = Signal(str, str, str)  # (file_path, folder_type, event_type)
```
- `file_path`: 변경된 파일/폴더 경로
- `folder_type`: "nir", "normal", "normal2", "cam1~6" 등
- `event_type`: "created", "modified", "deleted" 등

---

### FolderEventHandler
watchdog 파일 시스템 이벤트 핸들러

#### 속성
- `comm`: Communicate 인스턴스
- `folder_type`: 감시 중인 폴더 타입
- `settings`: 설정 정보

#### 메서드
```python
__init__(comm: Communicate, folder_type, settings=None)
```

```python
should_ignore_deep_folder(path: str) -> bool
```
camera 하위폴더 모드에서 깊은 경로 무시 여부 판단
- `use_camera_subfolder=True`일 때만 동작
- `/base/camera/file.jpg` → 처리 O
- `/base/camera/subfolder/file.jpg` → 무시
- **Returns**: True면 무시, False면 처리

```python
on_any_event(event)
```
파일 시스템 이벤트 발생 시 호출
- `.tmp`, `Thumbs.db` 등 임시 파일 무시
- `should_ignore_deep_folder()` 체크
- `file_changed` 시그널 발생

---

### FileMatcher
파일 매칭 엔진 (메인 클래스)

#### 시그널
```python
log_signal = Signal(str)       # 로그 메시지
```

#### 속성
- `unmatched_files`: 매칭되지 않은 파일들
  ```python
  {
      "nir": {"run_120250926T103033": "path/to/file.spc"},
      "normal": {"C250926T103030_0": "path/to/folder"},
      "normal2": {...},
      "cam1": [(filename, path, datetime_obj), ...],
      "cam2": [...],
      ...
  }
  ```
- `consumed_nir_keys`: 이미 사용된 NIR 파일 키 집합

#### 주요 메서드

##### 상태 관리
```python
reset_state(self)
```
모든 상태 초기화 (새로운 스캔 시작 전)

```python
load_state(self, unmatched_files, consumed_nir_keys)
```
외부 상태 로드 (groups_state.json에서)

##### 경로 계산
```python
get_effective_path(base_path: str, use_camera_subfolder: bool) -> str
```
실제 검색 경로 계산
- `use_camera_subfolder=True`: `base_path/camera` 반환
- `use_camera_subfolder=False`: `base_path` 반환

##### 파일 추가/제거
```python
add_or_update_file(self, file_path, folder_type)
```
파일 생성/수정 이벤트 처리
- `unmatched_files`에 즉시 추가
- NIR 파일: `add_nir_immediately()` 호출

```python
remove_from_unmatched(self, file_path, folder_type)
```
파일 삭제 이벤트 처리
- `unmatched_files`에서 제거

##### NIR 처리
```python
add_nir_immediately(self, file_path)
```
NIR 파일을 즉시 처리
- 파일명에서 키 추출: `run_120250926T103033.spc` → `run_120250926T103033`
- NIR 파일 쌍(.spc + .txt) 확인
- 쌍이 완성되면 `unmatched_files["nir"]`에 추가
- 로그 출력

##### 전체 스캔
```python
scan_and_build_unmatched(self, settings) -> dict
```
모든 폴더를 스캔하여 unmatched_files 구축
- NIR 폴더: `.spc` 파일 검색
- 일반 카메라: 폴더 검색 (`C` prefix)
- 복합 카메라: 이미지 파일 검색 (`YYYYMMDD_HHMMSS_XXX` 패턴)
- camera 하위폴더 옵션 반영
- 타임스탬프 추출 및 정렬
- `consumed_nir_keys` 필터링
- **Returns**: `unmatched_files` 딕셔너리

---

### FileMatcherWorker
백그라운드에서 주기적으로 파일 스캔을 수행하는 워커 (QThread)

> WSL 환경에서 watchdog 이벤트가 발생하지 않는 문제 해결용

#### 시그널
```python
scan_completed = Signal(dict)  # unmatched_files
```

#### 속성
- `file_matcher`: FileMatcher 인스턴스
- `is_enabled`: 워커 활성화 상태
- `settings`: 현재 설정
- `needs_scan`: 스캔 요청 플래그
- `is_running`: 워커 실행 상태

#### 메서드
```python
__init__(self, file_matcher: FileMatcher)
```

```python
update_settings(self, settings: dict)
```
설정 업데이트 (스레드 안전)

```python
trigger_scan(self)
```
스캔 트리거 (watchdog 이벤트 등에서 호출)

```python
enable(self)
```
워커 활성화 (감시 시작)

```python
disable(self)
```
워커 비활성화 (감시 중지, Stop 상태)

```python
stop(self)
```
워커 종료 (스레드 종료)

```python
run(self)
```
백그라운드 스레드 메인 루프
- 활성화 상태에서만 스캔 실행
- 트리거 발생 또는 주기 스캔 (scan_interval)
- `file_matcher.scan_and_build_unmatched()` 호출
- `scan_completed` 시그널 발생

---

## 파일 타입별 처리

### NIR 파일 (.spc)
- **폴더**: `settings["nir"]`, `settings["nir2"]`
- **패턴**: `run_1YYYYMMDDTHHMMSS.spc`
- **키 추출**: `run_1YYYYMMDDTHHMMSS`
- **즉시 처리**: 3초 대기 없음

### 일반 카메라 (폴더)
- **폴더**: `settings["normal"]`, `settings["normal2"]`
- **패턴**: `CYYMMDDTHHMMSS_X` (X: 0 또는 1)
- **구조**: 폴더 내부에 이미지 파일 포함
- **안정화**: 3초 대기 후 처리

### 복합 카메라 (이미지)
- **폴더**: `settings["cam1~6"]`
- **패턴**: `YYYYMMDD_HHMMSS_XXX.jpg`
- **타임스탬프 추출**: 파일명에서 datetime 객체 생성
- **안정화**: 3초 대기 후 처리

---

## 데이터 구조

### unmatched_files
```python
{
    "nir": {
        "run_120250926T103033": "e:/data/nir/run_120250926T103033.spc"
    },
    "normal": {
        "C250926T103030_0": "e:/data/normal/C250926T103030_0"
    },
    "normal2": {...},
    "cam1": [
        ("20250926_103035_001.jpg", "e:/data/cam1/20250926_103035_001.jpg", datetime_obj)
    ],
    "cam2": [...],
    ...
}
```



---

## 워크플로우

### 1. 파일 생성 감지
```
파일 생성
  → watchdog 이벤트
  → FolderEventHandler.on_any_event()
  → comm.file_changed 시그널
  → MainWindow에서 수신
  → FileMatcher.add_or_update_file()
  → unmatched_files에 즉시 추가
```

### 2. 주기적 스캔
```
FileMatcherWorker.run() 루프
  → scan_interval 대기
  → enabled 확인
  → scan_and_build_unmatched()
  → unmatched_files 생성
  → scan_completed 시그널
  → MainWindow.on_scan_completed()
  → process_updates()
```

### 3. NIR 즉시 처리
```
NIR 파일 생성 (.spc 또는 .txt)
  → add_nir_immediately()
  → 쌍 파일 존재 확인 (.spc + .txt)
  → 쌍이 완성되면 unmatched_files["nir"]에 즉시 추가
```

---

## camera 하위폴더 모드

### use_camera_subfolder = True
```
base_path = "e:/data/cam1"
effective_path = "e:/data/cam1/camera"

검색 대상:
  e:/data/cam1/camera/file1.jpg  ✓
  e:/data/cam1/camera/file2.jpg  ✓
  e:/data/cam1/file3.jpg         ✗ (무시)
  e:/data/cam1/camera/sub/file4.jpg  ✗ (깊은 폴더 무시)
```

### use_camera_subfolder = False
```
base_path = "e:/data/cam1"
effective_path = "e:/data/cam1"

검색 대상:
  e:/data/cam1/file1.jpg  ✓
  e:/data/cam1/file2.jpg  ✓
  e:/data/cam1/sub/file3.jpg  ✓ (재귀 검색)
```

---

## NIR 파일 쌍 처리

### 쌍 파일 확인
NIR 파일은 `.spc`와 `.txt` 파일 쌍으로 구성됩니다.
- 하나의 파일만 있으면 대기
- 쌍이 완성되면 즉시 `unmatched_files`에 추가

### 즉시 처리 이유
- NIR 장비가 파일을 완전히 쓴 후 이벤트 발생
- 빠른 매칭이 필요함
- 일반 카메라와의 시간 차이 최소화

---

## 의존성
- `PySide6.QtCore`: Qt 코어 (QThread, Signal)
- `watchdog`: 파일 시스템 감시
- `utils`: 타임스탬프 추출 함수
  - `extract_datetime_from_str()`
  - `get_timestamp_from_yml()`
  - `extract_datetime_from_composite_cam()`
- `pathlib`: 경로 처리
- `datetime`: 시간 처리
- `re`: 정규표현식
- `time`: 시간 측정
