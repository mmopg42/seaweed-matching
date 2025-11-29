# 복합카메라 시간 기반 매칭 구현 태스크

## 개요
일반카메라 폴더와 복합카메라(cam1-6) 파일 간 타임스탬프 기반 매칭 로직 추가

### 요구사항
- **일반카메라**: 폴더명에서 타임스탬프 추출 (기존 로직 활용 - `extract_datetime_from_str(folder_name, "C")`)
- **복합카메라**: 파일명에서 타임스탬프 추출 (형식: `YYYYMMDD_HHMMSS_XXX`)
- **매칭 조건**: 복합카메라 촬영 시간이 일반카메라 촬영 시간보다 늦어야 하며, 4초 이상 6초 이하 늦게 찍힌 경우에만 매칭 (즉, 4초 <= (복합카메라 시각 - 일반카메라 시각) <= 6초)
- **시간 방향**: 복합카메라 촬영 시각은 항상 일반카메라 이후여야 함 (과거 시각은 매칭 대상 제외)
- **범위 외**: 4초 미만 또는 6초 초과 시 다음 후보 파일로 이동
- **라인 지원**: 라인1 (normal + cam1-3), 라인2 (normal2 + cam4-6)
- **설정 옵션**: 시간 기반 매칭 활성화/비활성화 (기본값: **활성화**)
  - 활성화: 4-6초 범위 체크하여 매칭
  - 비활성화: 기존 방식대로 순차적으로 할당

---

## Phase 1: 분석 및 준비 ✅

### Task 1.1: 현재 구현 분석 ✅
**목적**: 기존 매칭 로직과 데이터 구조 이해

- [x] `group_manager.py`의 `_build_line_groups` 메서드 분석
- [x] 라인1 (normal + cam1-3) 처리 확인
- [x] 라인2 (normal2 + cam4-6) 처리 확인
- [x] cam 큐 평탄화(`flatten_cam_files`) 로직 확인
- [x] 현재 cam 할당 방식: 순차적 pop (시간 무관)

**발견 사항**:
- cam4, cam5, cam6은 이미 라인2용으로 설정됨 (line 58)
- 현재는 `pop_one(queue)` 방식으로 큐에서 순차적으로 가져옴 (line 84)
- 시간 기반 매칭 없음 - 단순 순서대로 배정

---

## Phase 2: 타임스탬프 추출 유틸리티 ✅

### Task 2.1: 복합카메라 파일명 타임스탬프 추출 함수 구현 ✅
**파일**: `utils.py`

**기능**:
```python
def extract_datetime_from_composite_cam(filename: str) -> Optional[datetime.datetime]:
    """
    복합카메라 파일명에서 타임스탬프 추출

    형식: YYYYMMDD_HHMMSS_XXX (XXX는 3자리 이상 숫자)
    예: 20250120_143052_001.jpg → 2025-01-20 14:30:52

    Args:
        filename: 파일명 (예: "20250120_143052_001.jpg")

    Returns:
        datetime 객체 또는 None (파싱 실패 시)
    """
```

**구현 내용**:
- [x] 정규표현식으로 `YYYYMMDD_HHMMSS_숫자` 패턴 매칭
- [x] 첫 번째 언더바(`_`) 앞: 날짜 8자리 (YYYYMMDD)
- [x] 두 번째 언더바 앞: 시간 6자리 (HHMMSS)
- [x] datetime 객체로 변환
- [x] 예외 처리: 파싱 실패 시 None 반환

**테스트 케이스**:
- `20250120_143052_001.jpg` → `2025-01-20 14:30:52` ✅
- `20250120_143052_12345.jpg` → `2025-01-20 14:30:52` (숫자 5자리도 지원) ✅
- `invalid_filename.jpg` → `None` ✅

**구현 완료**: 2025-01-25
- 함수 구현: [utils.py:60-89](utils.py#L60-L89)
- 테스트 코드: [test_phase2.py](test_phase2.py)
- 테스트 결과: 8개 테스트 모두 통과 ✅

---

## Phase 3: 시간 기반 매칭 로직 ✅

### Task 3.1: 일반카메라-복합카메라 시간 차이 계산 함수 ✅
**파일**: `group_manager.py`

**기능**:
```python
def is_valid_cam_match(normal_dt: datetime.datetime, cam_dt: datetime.datetime) -> bool:
    """
    일반카메라와 복합카메라의 타임스탬프가 매칭 범위 내인지 확인

    매칭 조건: 4초 <= 시간 차이 <= 6초

    Args:
        normal_dt: 일반카메라 폴더 타임스탬프
        cam_dt: 복합카메라 파일 타임스탬프

    Returns:
        True: 매칭 성공 (4-6초 범위)
        False: 매칭 실패 (범위 벗어남)
    """
```

**구현 내용**:
- [x] 시간 차이 계산: `diff = (cam_dt - normal_dt).total_seconds()`
- [x] diff가 음수이면 매칭 실패 (복합카메라가 더 먼저 찍힌 경우)
- [x] 4.0 <= diff <= 6.0 인 경우에만 매칭 성공
- [x] 상수 정의: `CAM_MATCH_MIN_DIFF`, `CAM_MATCH_MAX_DIFF`

**구현 완료**: [group_manager.py:174-198](group_manager.py#L174-L198)

---

### Task 3.2: cam 큐에서 시간 기반 매칭 파일 찾기 ✅
**파일**: `group_manager.py`

**기능**:
```python
def find_matching_cam_file(normal_dt: datetime.datetime, cam_queue: list, use_time_matching: bool) -> Optional[tuple]:
    """
    cam 큐에서 일반카메라 타임스탬프와 매칭되는 파일 찾기

    Args:
        normal_dt: 일반카메라 폴더 타임스탬프
        cam_queue: [(filename, abspath, mtime, ctime, is_copy), ...]
        use_time_matching: True이면 시간 기반 매칭, False이면 순차 pop

    Returns:
        매칭된 파일 튜플 또는 None
        반환 시 큐에서 해당 항목 제거
    """
```

**구현 로직**:
1. ✅ cam_queue를 순회
2. ✅ 각 파일명에서 타임스탬프 추출 (`extract_datetime_from_composite_cam`)
3. ✅ 일반카메라 타임스탬프와 비교 (`is_valid_cam_match`)
4. ✅ 매칭 성공 시: 해당 항목을 큐에서 제거하고 반환
5. ✅ 매칭 실패 시: 다음 항목으로 계속 탐색
6. ✅ 큐 전체 탐색 후 매칭 없으면 None 반환
7. ✅ `use_time_matching=False`일 경우 기존 순차 방식 유지

**구현 완료**: [group_manager.py:200-242](group_manager.py#L200-L242)

---

### Task 3.3: `_build_line_groups` 메서드 수정 ✅
**파일**: `group_manager.py`

**수정 전 (기존 로직)**:
```python
for i, queue in enumerate(cam_queues):
    picked = self.pop_one(queue)  # 단순 순차 pop
    if picked:
        fname, abspath, mtime, ctime, is_copy = picked
        cam_data[i] = {fname: {"absolute_path": abspath}}
```

**수정 후 (시간 기반 매칭)**:
```python
for i, queue in enumerate(cam_queues):
    # 시간 기반 매칭 또는 순차 매칭
    picked = self.find_matching_cam_file(t_norm, queue, use_time_matching=use_cam_time_matching)
    if picked:
        fname, abspath, mtime, ctime, is_copy = picked
        cam_data[i] = {fname: {"absolute_path": abspath}}
```

**변경 사항**:
- [x] `pop_one(queue)` → `find_matching_cam_file(t_norm, queue, use_time_matching)`
- [x] 시간 기반 매칭으로 변경
- [x] `use_cam_time_matching` 파라미터 추가
- [x] `build_all_groups`와 `_build_line_groups` 메서드 시그니처 수정
- [x] 매칭 실패 시 빈 딕셔너리 유지 (기존 동일)

**구현 완료**: [group_manager.py:16-107](group_manager.py#L16-L107)

---

## Phase 4: 예외 처리 및 로깅

### Task 4.1: 매칭 실패 케이스 처리
**구현 사항**:
- [ ] 매칭 범위 내 파일 없을 때: 해당 cam 슬롯 비움 (기존 동일)
- [ ] 타임스탬프 추출 실패 시: 로그 출력 후 스킵
- [ ] 큐가 비어있을 때: 기존 로직 유지

### Task 4.2: 디버그 로깅 추가
**로그 메시지 예시**:
```
[매칭] normal: C_20250120_143046 (14:30:46) + cam1: 20250120_143052_001.jpg (14:30:52) → 차이 6초 ✓
[매칭] normal: C_20250120_143046 (14:30:46) + cam2: 20250120_143050_002.jpg (14:30:50) → 차이 4초 ✓
[매칭] normal: C_20250120_143046 (14:30:46) + cam3: 20250120_143047_003.jpg (14:30:47) → 차이 1초 ✗ (범위 벗어남)
[매칭] cam3: 매칭 실패 - 큐에 적합한 파일 없음
```

**구현**:
- [ ] `is_valid_cam_match`에서 차이값 로그
- [ ] `find_matching_cam_file`에서 매칭 결과 로그
- [ ] 설정에 따라 로그 on/off 가능 (선택 사항)

---

## Phase 5: 설정 옵션 추가 ✅

### Task 5.1: 설정 다이얼로그에 체크박스 추가 ✅
**파일**: `ui_components.py` - `SettingDialog` 클래스

**구현 내용**:
- [x] 체크박스 변수 추가: `self.use_cam_time_matching`
- [x] UI에 체크박스 추가
  - 위치: 공통 설정 섹션 (디스크 캐시 아래)
  - 라벨: "복합카메라 시간 기반 매칭 사용 (4-6초 범위)"
  - 툴팁: 시간 기반 매칭 vs 순차 매칭 상세 설명
- [x] 기본값: `True` (체크 상태 - 활성화 권장)

**구현 완료**: [ui_components.py:422-432](ui_components.py#L422-L432)

---

### Task 5.2: 설정 저장/로드 구현 ✅
**파일**: `ui_components.py`, `monitoring_app.py`

**구현 내용**:
- [x] `SettingDialog.get_settings()`에 설정 추가
  - 키: `"use_cam_time_matching"`
  - 값: 체크박스 상태 (bool)
  - 구현: [ui_components.py:507](ui_components.py#L507)

- [x] `monitoring_app.py`의 `show_setting_dialog()`에서 설정 로드
  - 기본값: `True` (새 기능 활성화 권장)
  - 구현: [monitoring_app.py:962-963](monitoring_app.py#L962-L963)

- [x] config.json에 자동 저장/로드

**설정 파일 예시**:
```json
{
  "use_cam_time_matching": true
}
```

---

### Task 5.3: 매칭 로직에 설정 적용 ✅
**파일**: `monitoring_app.py`

**구현 내용**:
- [x] `build_all_groups` 호출 시 설정 파라미터 전달
- [x] 4개 호출 위치 모두 수정:
  ```python
  self.groups = self.group_manager.build_all_groups(
      self.file_matcher.unmatched_files,
      self.file_matcher.consumed_nir_keys,
      nir_match_time_diff=nir_match_time_diff,
      use_cam_time_matching=self.settings.get("use_cam_time_matching", True)
  )
  ```

- [x] `group_manager.py`는 Phase 3에서 이미 구현 완료
  - `find_matching_cam_file()` 메서드가 `use_time_matching` 파라미터 지원
  - 설정에 따라 시간 기반 / 순차 매칭 자동 분기

**구현 완료**:
- [monitoring_app.py:605-610](monitoring_app.py#L605-L610)
- [monitoring_app.py:869-874](monitoring_app.py#L869-L874)
- [monitoring_app.py:1468-1473](monitoring_app.py#L1468-L1473)
- [monitoring_app.py:1956-1961](monitoring_app.py#L1956-L1961)

---

## Phase 6: 라인2 검증

### Task 6.1: 라인2 (normal2 + cam4-6) 테스트
**확인 사항**:
- [ ] cam4, cam5, cam6 폴더 경로 설정 확인
- [ ] normal2 폴더와 cam4-6 시간 매칭 테스트
- [ ] 라인1과 동일한 매칭 로직 적용 확인

**테스트 데이터**:
- normal2 폴더: `C_20250120_150010` (15:00:10)
- cam4 파일: `20250120_150014_001.jpg` (15:00:14) → 차이 4초 ✓
- cam5 파일: `20250120_150016_002.jpg` (15:00:16) → 차이 6초 ✓
- cam6 파일: `20250120_150020_003.jpg` (15:00:20) → 차이 10초 ✗

---

## Phase 7: 통합 테스트

### Task 7.1: 설정 옵션 테스트
**테스트 시나리오**:

1. **시간 기반 매칭 활성화 (기본값)**
   - 설정 체크박스: ✓ 활성화
   - 예상: 4-6초 범위 내 파일만 매칭
   - 범위 외 파일은 스킵

2. **시간 기반 매칭 비활성화**
   - 설정 체크박스: ✗ 비활성화
   - 예상: 기존 방식대로 큐에서 순차적으로 pop
   - 시간 무관하게 매칭

3. **설정 변경 후 재시작**
   - 설정 저장 → 프로그램 재시작
   - 예상: 설정이 유지되어 로드됨

---

### Task 7.2: 엣지 케이스 테스트
**테스트 시나리오**:

1. **정확히 4초 차이**
   - normal: 14:30:00
   - cam1: 14:30:04
   - 예상: 매칭 성공 ✓

2. **정확히 6초 차이**
   - normal: 14:30:00
   - cam1: 14:30:06
   - 예상: 매칭 성공 ✓

3. **3.9초 차이 (범위 미만)**
   - normal: 14:30:00
   - cam1: 14:30:03.9
   - 예상: 매칭 실패 ✗

4. **6.1초 차이 (범위 초과)**
   - normal: 14:30:00
   - cam1: 14:30:06.1
   - 예상: 매칭 실패 ✗

5. **큐에 여러 파일 (첫 번째 불일치, 두 번째 일치)**
   - normal: 14:30:00
   - cam1 큐: [14:30:02 (2초 ✗), 14:30:05 (5초 ✓), 14:30:10 (10초 ✗)]
   - 예상: 14:30:05 파일 매칭

6. **파일명 파싱 실패**
   - cam1 파일: `invalid_name.jpg`
   - 예상: 타임스탬프 추출 실패 → 매칭 스킵

### Task 6.2: 실제 데이터 테스트
**준비**:
- [ ] 실제 일반카메라 폴더 구조 확인
- [ ] 실제 복합카메라 파일 네이밍 확인
- [ ] 라인1, 라인2 모두 실제 데이터로 검증

---

## Phase 8: 성능 최적화 (선택 사항)

### Task 8.1: 큐 탐색 최적화
**현재**: 선형 탐색 O(n)

**개선 방안 (필요 시)**:
- [ ] 큐를 타임스탬프 기준으로 정렬 (이미 mtime 정렬됨)
- [ ] 이진 탐색으로 범위 내 파일 찾기
- [ ] 조기 종료 조건 추가 (시간 차이가 너무 크면 중단)

---

## Phase 9: 문서화 및 마무리

### Task 9.1: 코드 주석 추가
- [ ] 함수 docstring 작성
- [ ] 매칭 로직 설명 주석
- [ ] 매직 넘버 상수화 (4초, 6초)

### Task 9.2: 사용자 가이드 작성
- [ ] 설정 옵션 설명 추가
  - "복합카메라 시간 기반 매칭 사용" 옵션 설명
  - 활성화/비활성화 시 동작 차이
  - 권장 설정 (기본값: 활성화)

### Task 9.3: 설정 파일 업데이트 (필요 시)
- [ ] 매칭 시간 범위를 설정 가능하게 변경? (현재는 4-6초 고정)
  - 고급 옵션: `"cam_match_min_diff"`, `"cam_match_max_diff"`
  - 향후 확장 가능성 고려

### Task 9.4: 최종 검증
- [ ] 모든 테스트 케이스 통과 확인
- [ ] 라인1, 라인2 동작 확인
- [ ] 매칭 로그 확인
- [ ] 성능 이슈 없는지 확인

---

## 상수 정의

```python
# group_manager.py 상단에 추가
# 복합카메라 매칭 시간 범위 (초)
CAM_MATCH_MIN_DIFF = 4.0  # 최소 시간 차이
CAM_MATCH_MAX_DIFF = 6.0  # 최대 시간 차이
```

---

## 구현 우선순위

1. **Phase 2 (Task 2.1)**: 타임스탬프 추출 함수 - 핵심 기능
2. **Phase 3 (Task 3.1, 3.2, 3.3)**: 매칭 로직 구현 - 핵심 기능
3. **Phase 5 (Task 5.1, 5.2, 5.3)**: 설정 옵션 추가 - 사용자 제어
4. **Phase 4 (Task 4.1)**: 예외 처리 - 안정성
5. **Phase 7 (Task 7.1, 7.2)**: 통합 테스트 - 검증
6. **Phase 4 (Task 4.2)**: 로깅 - 디버깅 편의
7. **Phase 6 (Task 6.1)**: 라인2 검증
8. **Phase 9**: 문서화 및 마무리

**Phase 8 (성능 최적화)**: 성능 이슈 발생 시에만 진행

---

## 예상 소요 시간

- Phase 2: 30분 (타임스탬프 추출 함수 + 테스트)
- Phase 3: 1시간 (매칭 로직 구현)
- Phase 5: 45분 (설정 옵션 UI + 저장/로드 + 로직 적용)
- Phase 4: 30분 (예외 처리 + 로깅)
- Phase 7: 1시간 (통합 테스트)
- **총 예상**: 3시간 45분

---

## 리스크 및 고려사항

1. **파일명 형식 변동**
   - 현재: `YYYYMMDD_HHMMSS_XXX`
   - 만약 형식이 다르면 정규표현식 수정 필요
   - 대응: 실제 파일명 샘플 확인 후 구현

2. **시간 차이 범위 (4-6초)**
   - 현재 고정값 (상수로 정의)
   - 향후 설정 가능하게 변경 가능성
   - 대응: Phase 5에서 기본 on/off 옵션 추가, 향후 범위 설정 추가 가능

3. **설정 기본값**
   - 시간 기반 매칭: 기본 활성화
   - 기존 사용자는 비활성화 가능
   - 대응: UI에 명확한 설명과 툴팁 제공

4. **큐 탐색 성능**
   - cam 파일이 매우 많을 경우 선형 탐색 느릴 수 있음
   - 대응: Phase 7 최적화 (필요 시)

5. **라인2 데이터 부족**
   - cam4-6 실제 데이터가 없을 수 있음
   - 대응: 테스트 데이터 생성 또는 라인1으로 우선 검증

---

---

## 현재 진행 상황

### ✅ 완료된 Phase
- **Phase 1**: 분석 및 준비 (Task 1.1 완료)
- **Phase 2**: 타임스탬프 추출 유틸리티 (Task 2.1 완료)
  - `extract_datetime_from_composite_cam()` 함수 구현 완료
  - 테스트 코드 작성 및 검증 완료 (8/8 테스트 통과)
- **Phase 3**: 시간 기반 매칭 로직 (Task 3.1, 3.2, 3.3 완료)
  - `is_valid_cam_match()` 함수 구현 완료
  - `find_matching_cam_file()` 함수 구현 완료
  - `_build_line_groups` 메서드 수정 완료
  - 테스트 코드 작성 및 검증 완료 (모든 테스트 통과)
- **Phase 5**: 설정 옵션 추가 (Task 5.1, 5.2, 5.3 완료) ✅
  - 설정 다이얼로그에 체크박스 추가 완료
  - 설정 저장/로드 구현 완료
  - 매칭 로직에 설정 적용 완료 (4개 호출 위치)

### 🔄 진행 예정
- **Phase 4**: 예외 처리 및 로깅 (선택 사항)
- **Phase 6**: 라인2 검증 (대기 중)
- **Phase 7**: 통합 테스트 (대기 중) ⬅️ 다음 권장 단계
- **Phase 8**: 성능 최적화 (선택 사항)
- **Phase 9**: 문서화 및 마무리 (대기 중)

### 📊 전체 진행률
- **완료**: 4 / 9 Phase (약 44%)
- **핵심 기능 완료**: Phase 2, 3, 5 (타임스탬프 추출 + 시간 기반 매칭 + 설정 옵션)
- **다음 작업**: Phase 7 - 통합 테스트 (실제 데이터로 검증)

### 📝 완료 세부 사항

#### Phase 2 완료 항목
- ✅ `extract_datetime_from_composite_cam()` 함수 구현 ([utils.py:60-89](utils.py#L60-L89))
- ✅ 정규표현식 패턴 매칭: `YYYYMMDD_HHMMSS_숫자`
- ✅ datetime 객체 변환 및 예외 처리
- ✅ 테스트 코드 작성 ([test_phase2.py](test_phase2.py))
- ✅ 모든 테스트 케이스 통과 (8/8)

#### Phase 3 완료 항목
- ✅ 상수 정의: `CAM_MATCH_MIN_DIFF = 4.0`, `CAM_MATCH_MAX_DIFF = 6.0`
- ✅ `is_valid_cam_match()` 메서드 구현 ([group_manager.py:169-193](group_manager.py#L169-L193))
  - 시간 차이 계산 및 4-6초 범위 검증
- ✅ `find_matching_cam_file()` 메서드 구현 ([group_manager.py:195-237](group_manager.py#L195-L237))
  - 시간 기반 매칭 및 순차 매칭 모드 지원
  - 큐에서 매칭 파일 검색 및 제거
- ✅ `build_all_groups()` 메서드 수정 ([group_manager.py:16-50](group_manager.py#L16-L50))
  - `use_cam_time_matching` 파라미터 추가
- ✅ `_build_line_groups()` 메서드 수정 ([group_manager.py:60-107](group_manager.py#L60-L107))
  - `pop_one()` → `find_matching_cam_file()` 변경
- ✅ 테스트 코드 작성 ([test_phase3.py](test_phase3.py))
  - is_valid_cam_match 테스트: 7/7 통과
  - find_matching_cam_file 테스트: 3/3 통과
  - 엣지 케이스 테스트: 3/3 통과

#### Phase 5 완료 항목
- ✅ 설정 다이얼로그 UI 추가 ([ui_components.py:422-432](ui_components.py#L422-L432))
  - 체크박스: "복합카메라 시간 기반 매칭 사용 (4-6초 범위)"
  - 상세 툴팁 제공 (시간 기반 vs 순차 매칭)
  - 기본값: True (활성화)
- ✅ 설정 저장/로드 ([ui_components.py:507](ui_components.py#L507), [monitoring_app.py:962-963](monitoring_app.py#L962-L963))
  - `get_settings()` 메서드에 `use_cam_time_matching` 추가
  - `show_setting_dialog()`에서 설정 로드
  - config.json 자동 저장/로드
- ✅ 매칭 로직 적용 ([monitoring_app.py](monitoring_app.py))
  - 4개 `build_all_groups()` 호출 위치 모두 수정
  - 설정값을 group_manager에 전달

---

**마지막 업데이트**: 2025-01-25
**작성자**: Claude Code
**상태**: Phase 5 완료, 핵심 기능 구현 완료 (Phase 2+3+5)
