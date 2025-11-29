# file_operations.py 문서

## 개요
파일 작업(이동/복사)을 처리하는 모듈입니다. Python 네이티브 방식을 사용하여 Windows Defender 호환성을 개선하고, 병렬 처리로 성능을 최적화합니다.

**파일 크기**: 24KB (539 라인)  
**총 클래스/함수**: 20개

---

## 주요 클래스

### FileOperationWorker
파일 작업을 백그라운드에서 실행하는 워커 (QThread)

#### 시그널
```python
progress = Signal(int, int)     # (current, total)
finished = Signal(int, int, int, str)  # (success, failed, total, operation_type)
error = Signal(str)
conflict = Signal(str, str)     # (destination, source_hint)
```

#### 속성
- `processed_data`: 처리할 데이터 (그룹 리스트)
- `output_path`: 출력 경로
- `mode`: "이동" 또는 "복사"
- `operation_type`: "file_op" (작업 타입)
- `user_response`: 충돌 시 사용자 응답
- `user_response_event`: 응답 대기 이벤트
- `rollback_items`: 롤백 대상 리스트

#### 메서드

##### 초기화
```python
__init__(self, processed_data, output_path, mode, operation_type="file_op")
```
- `processed_data`: 이동/복사할 그룹 데이터
- `output_path`: 목적지 폴더
- `mode`: "이동" 또는 "복사"

##### 사용자 응답 처리
```python
set_user_response(self, response: str)
```
메인 스레드에서 호출하여 사용자 응답 전달
- `response`: "overwrite_all", "overwrite", "cancel"

##### 경로 처리
```python
_rel(self, p: str) -> str
```
상대 경로 반환 (로그용)

```python
_same_device(self, src: str, dst_dir: str) -> bool
```
소스와 대상이 같은 디바이스인지 확인

```python
_ensure_dir(self, d: str)
```
디렉토리 존재 보장 (없으면 생성)

##### 충돌 처리
```python
_check_conflict(self, dst: str, src_hint: str = "") -> bool
```
대상 경로 충돌 확인 및 사용자 응답 대기
- **Returns**: True=계속, False=취소

##### 디렉토리 작업
```python
_copy_dir_native(self, src: str, dst: str) -> int
```
shutil을 사용한 디렉토리 복사
- **Returns**: 성공 시 1, 실패 시 0

```python
_move_dir_native(self, src: str, dst: str) -> int
```
shutil을 사용한 디렉토리 이동
- 같은 디바이스: `shutil.move()` (빠름)
- 다른 디바이스: `shutil.copytree()` + `shutil.rmtree()` (느림)
- **Returns**: 성공 시 1, 실패 시 0

##### 파일 배치 작업
```python
_copy_files_batch(self, src_dir: str, dst_dir: str, filenames: list[str]) -> int
```
파일 여러 개를 복사
- **Returns**: 성공 개수

```python
_move_files_batch(self, src_dir: str, dst_dir: str, filenames: list[str]) -> int
```
파일 여러 개를 이동
- `shutil.move()` 사용
- **Returns**: 성공 개수

##### 롤백
```python
_rollback(self)
```
작업 실패 시 이미 처리된 항목 롤백
- 이동 작업: 원래 위치로 복원
- 복사 작업: 복사본 삭제

##### 이동 계획 생성
```python
_build_move_plan_nested(self) -> dict
```
중첩 구조로 이동 계획 생성
```python
{
    "subject1": {
        "with_nir": {
            "Nir": {
                "dirs": ["path/to/nir"],
                "files": {"dirname": ["file1.spc", "file2.spc"]}
            },
            "일반": {...},
            "복합 카메라": {
                "cam1": {"files": {...}},
                "cam2": {...},
                "cam3": {...}
            }
        },
        "without_nir": {...}
    },
    "subject2": {...}
}
```

##### 계획 저장
```python
_save_move_plan(self, plan: dict) -> dict
```
각 시료별로 move_plan.json 저장
- 경로: `<app_dir>/<YYYYMMDD>/<subject>/move_plan.json`
- **Returns**: `{subject: plan_path}`

##### 메인 실행
```python
run(self)
```
백그라운드 스레드 메인 함수
1. 이동 계획 생성
2. 계획 저장
3. 작업 실행 (`_execute_bucketed()`)
4. 성공/실패 통계 계산
5. 이동 기록 저장
6. `finished` 시그널 발생

```python
_execute_bucketed(self, plan: dict)
```
이동 계획 실행 (핵심 로직)
1. 시료별 처리
2. with_nir/without_nir 분기
3. 폴더별 처리 (Nir, 일반, 복합 카메라)
4. 디렉토리 작업 (병렬)
5. 파일 배치 작업 (병렬)
6. 진행 상태 업데이트
7. 실패 시 롤백

##### 이동 기록
```python
_record_move(self, plan: dict, plan_paths: dict, stats: dict)
```
이동 완료 기록 저장
- `moved_subjects.json`에 기록
- 시료명, 시각, 모드, 통계 저장

---

## 데이터 구조

### processed_data (입력)
```python
[
    {
        "subject": "sample1",
        "groups": [
            {
                "nir": "run_120250926T103033.spc",
                "norm": "C250926T103030_0",
                "cam1": "20250926_103035_001.jpg",
                ...
            },
            ...
        ]
    },
    ...
]
```

### move_plan (중첩 구조)
```python
{
    "subject1": {
        "with_nir": {
            "Nir": {
                "dirs": ["e:/data/nir/run_120250926T103033"],
                "files": {
                    "e:/data/nir": ["run_120250926T103033.spc"]
                }
            },
            "일반": {
                "dirs": ["e:/data/normal/C250926T103030_0"]
            },
            "복합 카메라": {
                "cam1": {
                    "files": {
                        "e:/data/cam1": ["20250926_103035_001.jpg"]
                    }
                },
                ...
            }
        },
        "without_nir": {...}
    }
}
```

---

## 작업 흐름

### 1. 이동 작업
```
사용자 [Move 버튼 클릭]
  → MainWindow.run_move_files()
  → FileOperationWorker 생성 (mode="이동")
  → worker.start()
  → run()
  → _build_move_plan_nested()
  → _save_move_plan()
  → _execute_bucketed()
  → 디렉토리/파일 이동
  → _record_move()
  → finished 시그널
  → MainWindow.on_file_operation_finished()
```

### 2. 복사 작업
```
mode="복사"로 워커 생성
  → run()
  → _build_move_plan_nested()
  → _save_move_plan()
  → _execute_bucketed()
  → 디렉토리/파일 복사 (shutil.copytree, shutil.copy2)
  → finished 시그널
```

### 3. 충돌 처리
```
파일 덮어쓰기 충돌 발생
  → _check_conflict()
  → conflict 시그널
  → MainWindow: 사용자에게 확인 다이얼로그
  → 사용자 응답
  → set_user_response()
  → 작업 계속/취소/모두 덮어쓰기
```

### 4. 롤백
```
작업 중 오류 발생
  → _rollback()
  → rollback_items 순회
  → 이동: 원래 위치로 복원
  → 복사: 복사본 삭제
  → error 시그널
```

---

## 병렬 처리

### ThreadPoolExecutor 사용
```python
with ThreadPoolExecutor(max_workers=4) as executor:
    futures = []
    for item in items:
        future = executor.submit(_process_item, item)
        futures.append(future)
    
    for future in as_completed(futures):
        result = future.result()
        # 결과 처리
```

### 처리 단위
- **디렉토리**: 각 폴더를 별도 스레드에서 처리
- **파일 배치**: 여러 파일을 배치 단위로 처리

### PermissionError 재시도
최대 3회 재시도 (각 시도 사이 0.5초 대기)
```python
for attempt in range(3):
    try:
        # 작업 수행
        break
    except PermissionError:
        if attempt < 2:
            time.sleep(0.5)
        else:
            raise
```

---

## 버킷 규칙

### with NIR (NIR 보유 그룹)
```
<output>/<subject>/with NIR/
    ├─ Nir/               (NIR 파일)
    ├─ 일반/               (일반 카메라 폴더)
    ├─ 일반2/              (일반2 카메라 폴더, 라인2)
    └─ 복합 카메라/
        ├─ cam1/          (복합 카메라 1)
        ├─ cam2/          (복합 카메라 2)
        ├─ cam3/          (복합 카메라 3)
        ├─ cam4/          (복합 카메라 4, 라인2)
        ├─ cam5/          (복합 카메라 5, 라인2)
        └─ cam6/          (복합 카메라 6, 라인2)
```

### without NIR (NIR 없는 그룹)
```
<output>/<subject>/without NIR/
    ├─ 일반 카메라/        (일반 카메라 폴더)
    ├─ 일반2 카메라/       (일반2 카메라 폴더, 라인2)
    └─ 복합 카메라/
        ├─ cam1/
        ├─ cam2/
        ├─ cam3/
        ├─ cam4/
        ├─ cam5/
        └─ cam6/
```

---

## 성능 최적화

1. **병렬 처리**: ThreadPoolExecutor로 동시 처리
2. **배치 작업**: 여러 파일을 한 번에 처리
3. **디바이스 감지**: 같은 디바이스면 빠른 이동 사용
4. **shutil 사용**: Python 네이티브 방식 (os.system 보다 안정적)

---

## 의존성
- `PySide6.QtCore`: QThread, Signal
- `config_manager.ConfigManager`: 설정 및 로그 관리
- `shutil`: 파일/디렉토리 작업
- `concurrent.futures`: 병렬 처리
- `pathlib`: 경로 처리
- `json`: JSON 파일 읽기/쓰기
- `datetime`: 시간 기록
- `traceback`: 오류 추적
- `threading`: 이벤트 동기화
