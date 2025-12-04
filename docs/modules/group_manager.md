# group_manager.py 문서

## 개요
파일 매칭 및 그룹 생성 로직을 담당하는 모듈입니다. 일반 카메라를 기준으로 NIR과 복합 카메라를 매칭하여 데이터 그룹을 생성합니다.

**파일 경로**: `script/domain/group_manager.py`  
**파일 크기**: 286 라인  
**총 클래스**: 1개 (`GroupManager`)  
**총 메서드**: 10개  
**업데이트**: 2025-12-04

**테스트 파일**:
- `tests/test_group_sorting_unit.py` - 정렬 동작 단위 테스트
- `tests/test_group_sorting_properties.py` - 정렬 속성 기반 테스트
- `tests/test_drain_cam_timestamp_handling.py` - 타임스탬프 처리 테스트

---

## 상수

```python
CAM_MATCH_MIN_DIFF = 4.0  # 최소 시간 차이 기본값 (초)
CAM_MATCH_MAX_DIFF = 6.0  # 최대 시간 차이 기본값 (초)
```
복합카메라 시간 기반 매칭 범위 기본값 (사용자 설정으로 변경 가능)

---

## GroupManager 클래스

### 속성
- `log`: 로그 출력 함수
- `group_counter`: 그룹 ID 카운터

### 메서드

#### 초기화
```python
__init__(self, log_emitter_func)
```

---

#### 그룹 생성 (메인 함수)
```python
build_all_groups(
    self,
    unmatched_data,
    consumed_nir_keys,
    nir_match_time_diff=1.0,
    use_cam_time_matching=True,
    cam_match_min_diff=4.0,
    cam_match_max_diff=6.0
) -> list
```

**매개변수**:
- `unmatched_data`: 매칭되지 않은 파일 데이터
- `consumed_nir_keys`: 이미 사용된 NIR 키 집합
- `nir_match_time_diff`: NIR 매칭 시간 차이 (초)
- `use_cam_time_matching`: 복합카메라 시간 기반 매칭 사용 여부
- `cam_match_min_diff`: 복합카메라 최소 시간 차이 (초, 기본: 4.0)
- `cam_match_max_diff`: 복합카메라 최대 시간 차이 (초, 기본: 6.0)

**처리 순서**:
1. 라인1 그룹 생성 (`_build_line_groups(line=1)`)
2. 라인2 그룹 생성 (`_build_line_groups(line=2)`)
3. 통합 및 반환

**Returns**: 그룹 리스트

---

#### 라인별 그룹 생성
```python
_build_line_groups(
    self,
    unmatched_data,
    consumed_nir_keys,
    line=1,
    nir_match_time_diff=1.0,
    use_cam_time_matching=True,
    cam_match_min_diff=4.0,
    cam_match_max_diff=6.0
) -> list
```

**처리 순서**:
1. 라인별 데이터 추출
   - 라인1: `normal`, `nir`, `cam1~3`
   - 라인2: `normal2`, `nir2`, `cam4~6`
2. 복합카메라 파일 평탄화
3. 일반 카메라 폴더 순회
4. 각 폴더당 1개 그룹 생성
5. NIR 매칭 (타임스탬프 차이 기준)
6. 복합카메라 매칭 (시간 기반 또는 순차)
7. 남은 복합카메라 파일 → cam-only 그룹 생성
8. NIR-only 그룹 생성 (매칭되지 않은 NIR)
9. **최종 시간순 정렬** (모든 그룹 타입 포함)

**정렬 동작**:
- 정렬은 모든 그룹(normal, cam-only, NIR-only) 생성 후 한 번만 수행
- 이를 통해 cam-only와 NIR-only 그룹이 리스트 끝에 추가되지 않고 타임스탬프에 따라 올바른 시간순 위치에 배치됨
- Python의 stable sort 사용으로 동일 타임스탬프 그룹의 상대적 순서 유지
- 정렬 키: 각 그룹의 `"time"` 필드 (ISO 8601 형식 문자열)
- 정렬 순서: 오름차순 (과거 → 현재)

**정렬 전후 비교**:

정렬 전 (문제 상황):
```python
[
    {"time": "2025-01-20T16:44:52", "type": "normal"},    # 164452
    {"time": "2025-01-20T16:29:32", "type": "cam-only"},  # 162932 (잘못된 위치)
    {"time": "2025-01-20T16:50:15", "type": "NIR-only"}   # 165015 (잘못된 위치)
]
```

정렬 후 (올바른 상황):
```python
[
    {"time": "2025-01-20T16:29:32", "type": "cam-only"},  # 162932 (올바른 위치)
    {"time": "2025-01-20T16:44:52", "type": "normal"},    # 164452
    {"time": "2025-01-20T16:50:15", "type": "NIR-only"}   # 165015 (올바른 위치)
]
```

---

#### 복합카메라 평탄화
```python
flatten_cam_files(self, cam_bucket) -> list
```
복합카메라 버킷을 단일 리스트로 변환 (타임스탬프 기준 정렬)

**입력**:
```python
{
    "cam1": [(filename, path, datetime_obj), ...],
    "cam2": [...],
    ...
}
```

**출력**:
```python
[
    (filename, path, datetime_obj, "cam1"),
    (filename, path, datetime_obj, "cam2"),
    ...
]  # 타임스탬프 오름차순 정렬
```

---

#### 큐 처리
```python
pop_one(self, queue) -> item or None
```
큐에서 첫 항목을 꺼내어 반환 (없으면 None)

---

#### 복합카메라 매칭 검증
```python
is_valid_cam_match(
    self,
    normal_dt,
    cam_dt,
    cam_match_min_diff=4.0,
    cam_match_max_diff=6.0
) -> bool
```

**매개변수**:
- `normal_dt`: 일반카메라 타임스탬프
- `cam_dt`: 복합카메라 타임스탬프
- `cam_match_min_diff`: 최소 시간 차이 (초, 기본: 4.0)
- `cam_match_max_diff`: 최대 시간 차이 (초, 기본: 6.0)

**매칭 조건**:
1. 복합카메라 촬영 시간이 일반카메라보다 늦어야 함: `cam_dt > normal_dt`
2. 시간 차이가 설정 범위 내: `cam_match_min_diff <= (cam_dt - normal_dt).total_seconds() <= cam_match_max_diff`

**Returns**: True=매칭 성공, False=매칭 실패

---

#### 복합카메라 파일 찾기
```python
find_matching_cam_file(
    self,
    normal_dt,
    cam_queue,
    use_time_matching=True,
    cam_match_min_diff=4.0,
    cam_match_max_diff=6.0
) -> tuple or None
```

**매개변수**:
- `normal_dt`: 일반카메라 타임스탬프
- `cam_queue`: 복합카메라 파일 큐
- `use_time_matching`: 시간 기반 매칭 사용 여부
- `cam_match_min_diff`: 최소 시간 차이 (초, 기본: 4.0)
- `cam_match_max_diff`: 최대 시간 차이 (초, 기본: 6.0)

**시간 기반 매칭** (`use_time_matching=True`):
- 큐를 순회하며 `is_valid_cam_match()` 검증
- 설정된 시간 범위 내의 파일만 매칭
- 매칭되면 큐에서 제거 후 반환
- 매칭 실패 시 None

**순차 매칭** (`use_time_matching=False`):
- 큐에서 첫 항목 꺼내어 반환

**Returns**: `(filename, path, datetime_obj, role)` 또는 None

---

#### cam-only 그룹 생성
```python
drain_cam_to_groups(self, groups, cam_key, queue)
```
큐에 남은 복합카메라 파일을 개별 그룹으로 생성
- 1개 파일 = 1개 그룹
- `has_nir=False`
- 일반 카메라 없음

**타임스탬프 추출 우선순위**:

복합카메라 파일의 타임스탬프는 다음 우선순위로 추출됩니다:

1. **파일명에서 추출** (`extract_datetime_from_composite_cam`)
   - 파일명이 `YYYYMMDD_HHMMSS` 형식을 포함하는 경우
   - 예: `20250120_162932_001.jpg` → `2025-01-20T16:29:32`
   - 가장 정확한 촬영 시간 정보
   - 권장 방법

2. **파일 mtime (수정 시간)**
   - 파일명에서 추출 실패 시 자동으로 사용
   - 파일 시스템 메타데이터 기반
   - `datetime.fromtimestamp(mtime)`로 변환
   - 파일 복사/이동 시 변경될 수 있어 정확도 낮음

3. **현재 시간 (fallback)**
   - 위 두 방법 모두 실패 시 최후 수단
   - `datetime.now()`로 현재 시간 사용
   - 경고 로그 출력: `"[WARNING] Failed to extract timestamp for {filename}, using current time"`
   - 정렬 시 리스트 끝에 배치됨
   - 데이터 무결성 문제 가능성 있음

**타임스탬프 추출 예시**:

```python
# 케이스 1: 파일명에서 추출 성공 (최선)
filename = "20250120_162932_001.jpg"
→ timestamp = "2025-01-20T16:29:32"

# 케이스 2: 파일명 추출 실패, mtime 사용
filename = "image_001.jpg"
mtime = 1737379772.0
→ timestamp = "2025-01-20T16:29:32"  # fromtimestamp(mtime)

# 케이스 3: 모두 실패, 현재 시간 사용 (최악)
filename = "corrupted.jpg"
mtime = 0 or None
→ timestamp = "2025-01-20T18:00:00"  # now()
→ 경고 로그 출력
```

---

## 그룹 생성 흐름

### 1. 일반 카메라 기준 그룹 생성
```
일반 카메라 폴더 순회
  → 각 폴더당 1개 그룹 생성
  → 타임스탬프 추출 (폴더명 또는 result.yml)
  → NIR 매칭 시도 (시간 차이 기준)
  → 복합카메라 매칭 시도
      - cam1, cam2, cam3 각각 1장씩
      - 시간 기반 매칭 또는 순차 매칭
```

### 2. cam-only 그룹 생성
```
복합카메라 큐에 남은 파일들
  → 각 파일당 1개 그룹 생성
  → 일반 카메라, NIR 없음
```

---

## 데이터 구조

### 그룹
```python
{
    "group_id": "G_001",
    "nir": "run_120250926T103033",          # NIR 키 (파일명 아님)
    "norm": "C250926T103030_0",             # 일반 카메라 폴더명
    "cam1": "20250926_103035_001.jpg",      # 복합 카메라 1
    "cam2": "20250926_103035_002.jpg",      # 복합 카메라 2
    "cam3": "20250926_103035_003.jpg",      # 복합 카메라 3
    "line": 1,                              # 라인 번호
    "has_nir": True                         # NIR 보유 여부
}
```

### cam-only 그룹
```python
{
    "group_id": "G_050",
    "cam1": "20250926_150000_001.jpg",
    "line": 1,
    "has_nir": False
}
```

---

## 매칭 로직

### NIR 매칭
```python
일반카메라 타임스탬프: 2025-09-26 10:30:30
NIR 타임스탬프:       2025-09-26 10:30:31

차이: 1초 → nir_match_time_diff=1.0 이하 → 매칭 성공
```

### 복합카메라 시간 기반 매칭
```python
일반카메라 타임스탬프: 2025-09-26 10:30:30
복합카메라 타임스탬프: 2025-09-26 10:30:35

차이: 5초
cam_match_min_diff: 4.0초 (설정값)
cam_match_max_diff: 6.0초 (설정값)

4.0 ≤ 5.0 ≤ 6.0 → 매칭 성공
```

**사용자 설정 가능**:
- 최소/최대 시간 차이는 설정 다이얼로그에서 변경 가능
- 촬영 환경에 따라 적절한 범위로 조정
- 기본값: 4.0~6.0초

### 복합카메라 순차 매칭
```python
일반카메라 C250926T103030_0
  → cam_queue에서 첫 번째 파일 꺼내서 매칭
  → 시간 무시
```

---

## 예시

### 예시 1: 기본 그룹 생성 및 정렬

#### 입력 (unmatched_data)
```python
{
    "nir": {
        "run_120250926T103033": "path/to/nir1.spc",
        "run_120250926T103050": "path/to/nir2.spc"
    },
    "normal": {
        "C250926T103030_0": "path/to/folder1",
        "C250926T103045_0": "path/to/folder2"
    },
    "cam1": [
        ("20250926_103035_001.jpg", "path/to/cam1_1.jpg", datetime(2025,9,26,10,30,35)),
        ("20250926_103050_001.jpg", "path/to/cam1_2.jpg", datetime(2025,9,26,10,30,50))
    ],
    "cam2": [...],
    "cam3": [...]
}
```

#### 출력 (groups)
```python
[
    {
        "group_id": "G_001",
        "nir": "run_120250926T103033",
        "norm": "C250926T103030_0",
        "cam1": "20250926_103035_001.jpg",
        "cam2": "20250926_103035_002.jpg",
        "cam3": "20250926_103035_003.jpg",
        "line": 1,
        "has_nir": True,
        "time": "2025-09-26T10:30:30"
    },
    {
        "group_id": "G_002",
        "nir": "run_120250926T103050",
        "norm": "C250926T103045_0",
        "cam1": "20250926_103050_001.jpg",
        "line": 1,
        "has_nir": True,
        "time": "2025-09-26T10:30:45"
    }
]
```

### 예시 2: 혼합 그룹 타입의 시간순 정렬

이 예시는 normal, cam-only, NIR-only 그룹이 모두 포함된 경우 시간순 정렬을 보여줍니다.

#### 입력 데이터
```python
{
    "normal": {
        "C250120T164452_0": "path/to/folder1",  # 16:44:52
    },
    "nir": {
        "run_120250120T165015": "path/to/nir1.spc",  # 16:50:15 (매칭 안됨)
    },
    "cam1": [
        ("20250120_162932_001.jpg", "path/cam1.jpg", datetime(2025,1,20,16,29,32)),  # 16:29:32 (매칭 안됨)
        ("20250120_164455_001.jpg", "path/cam2.jpg", datetime(2025,1,20,16,44,55)),  # 16:44:55 (normal과 매칭)
    ]
}
```

#### 생성 과정
1. Normal 그룹 생성: `C250120T164452_0` (16:44:52)
2. Cam 매칭: `20250120_164455_001.jpg` → normal 그룹에 추가
3. Cam-only 그룹 생성: `20250120_162932_001.jpg` (16:29:32)
4. NIR-only 그룹 생성: `run_120250120T165015` (16:50:15)
5. **최종 정렬 수행**

#### 출력 (시간순 정렬됨)
```python
[
    {
        "group_id": "G_001",
        "type": "cam-only",
        "cam1": "20250120_162932_001.jpg",
        "line": 1,
        "has_nir": False,
        "time": "2025-01-20T16:29:32"  # 가장 이른 시간
    },
    {
        "group_id": "G_002",
        "type": "normal",
        "norm": "C250120T164452_0",
        "cam1": "20250120_164455_001.jpg",
        "line": 1,
        "has_nir": False,
        "time": "2025-01-20T16:44:52"  # 중간 시간
    },
    {
        "group_id": "G_003",
        "type": "NIR-only",
        "nir": "run_120250120T165015",
        "line": 1,
        "has_nir": True,
        "time": "2025-01-20T16:50:15"  # 가장 늦은 시간
    }
]
```

**핵심 포인트**:
- Cam-only 그룹(16:29:32)이 normal 그룹(16:44:52)보다 먼저 배치됨
- NIR-only 그룹(16:50:15)이 가장 마지막에 배치됨
- 그룹 타입과 무관하게 순수하게 타임스탬프 순서로 정렬됨

### 예시 3: 동일 타임스탬프의 Stable Sort

동일한 타임스탬프를 가진 그룹들의 상대적 순서가 유지됩니다.

#### 입력
```python
# 모두 16:30:00 타임스탬프
groups = [
    {"time": "2025-01-20T16:30:00", "type": "normal", "group_id": "G_001"},
    {"time": "2025-01-20T16:30:00", "type": "cam-only", "group_id": "G_002"},
    {"time": "2025-01-20T16:30:00", "type": "NIR-only", "group_id": "G_003"},
]
```

#### 출력 (삽입 순서 유지)
```python
[
    {"time": "2025-01-20T16:30:00", "type": "normal", "group_id": "G_001"},
    {"time": "2025-01-20T16:30:00", "type": "cam-only", "group_id": "G_002"},
    {"time": "2025-01-20T16:30:00", "type": "NIR-only", "group_id": "G_003"},
]
```

Python의 stable sort 특성으로 동일 키 값에 대해 원래 순서가 보존됩니다.

---

## 테스트

### 단위 테스트 (`test_group_sorting_unit.py`)

**테스트 범위**:
- Normal 그룹 시간순 정렬
- Cam-only 그룹 시간순 정렬
- NIR-only 그룹 시간순 정렬
- 혼합 그룹 타입 통합 정렬
- 엣지 케이스 (빈 리스트, 단일 그룹, 동일 타임스탬프)

**주요 테스트 케이스**:
1. `test_normal_groups_sorted_chronologically` - Normal 그룹 정렬 검증
2. `test_cam_only_groups_sorted_chronologically` - Cam-only 그룹 정렬 검증
3. `test_nir_only_groups_sorted_chronologically` - NIR-only 그룹 정렬 검증
4. `test_mixed_group_types_sorted_together` - 혼합 타입 통합 정렬 검증
5. `test_mixed_groups_not_clustered_by_type` - 타입별 클러스터링 방지 검증
6. `test_empty_group_list` - 빈 리스트 처리
7. `test_single_group` - 단일 그룹 처리
8. `test_two_groups_same_timestamp` - 동일 타임스탬프 stable sort 검증

**실행 방법**:
```bash
conda run -n seaweed pytest tests/test_group_sorting_unit.py -v
```

### 속성 기반 테스트 (`test_group_sorting_properties.py`)

**테스트 속성**:
1. **Property 1: Chronological Sorting Invariant**
   - 모든 그룹 타입이 시간순으로 정렬됨을 검증
   - 100회 반복 테스트 (hypothesis)
   - 랜덤 그룹 생성 (0~50개)
   
2. **Property 2: Tie-Breaking Consistency**
   - 동일 타임스탬프 그룹의 stable sort 검증
   - 여러 번 정렬해도 동일한 순서 유지
   - 100회 반복 테스트
   
3. **Property 3: Performance Bound**
   - 1000개 그룹 정렬 성능 검증
   - 100ms 이내 완료 요구사항 검증
   - 100회 반복 테스트
   - 최적화된 전략으로 빠른 테스트 생성
   
4. **Property 4: Error Handling Robustness**
   - 잘못된 타임스탬프 처리 검증
   - 시스템 안정성 확인
   - 누락/잘못된 타임스탬프에도 크래시 없음

**성능 요구사항**:
- 1000개 그룹 정렬: < 100ms
- Python의 Timsort 알고리즘 활용
- 실제 측정: ~0.5ms (요구사항 대비 200배 빠름)

**실행 방법**:
```bash
# 전체 속성 테스트 실행
conda run -n seaweed pytest tests/test_group_sorting_properties.py -v

# 성능 테스트만 실행
conda run -n seaweed pytest tests/test_group_sorting_properties.py::test_property_performance_bound -v
```

### 타임스탬프 처리 테스트 (`test_drain_cam_timestamp_handling.py`)

**테스트 범위**:
- 파일명에서 타임스탬프 추출
- mtime fallback 동작
- 현재 시간 fallback 동작
- 경고 로그 출력 검증

**실행 방법**:
```bash
conda run -n seaweed pytest tests/test_drain_cam_timestamp_handling.py -v
```

---

## 의존성
- `utils.extract_datetime_from_composite_cam`: 복합카메라 타임스탬프 추출
- `datetime`: 시간 차이 계산
- `pathlib.Path`: 경로 처리
- `os`: 파일 시스템 작업
