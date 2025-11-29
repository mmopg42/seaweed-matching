# group_manager.py 문서

## 개요
파일 매칭 및 그룹 생성 로직을 담당하는 모듈입니다. 일반 카메라를 기준으로 NIR과 복합 카메라를 매칭하여 데이터 그룹을 생성합니다.

**파일 크기**: 11KB (286 라인)  
**총 함수**: 10개

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

### 입력 (unmatched_data)
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

### 출력 (groups)
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
        "has_nir": True
    },
    {
        "group_id": "G_002",
        "nir": "run_120250926T103050",
        "norm": "C250926T103045_0",
        "cam1": "20250926_103050_001.jpg",
        "line": 1,
        "has_nir": True
    }
]
```

---

## 의존성
- `utils.extract_datetime_from_composite_cam`: 복합카메라 타임스탬프 추출
- `datetime`: 시간 차이 계산
- `pathlib.Path`: 경로 처리
- `os`: 파일 시스템 작업
