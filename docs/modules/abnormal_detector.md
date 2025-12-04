# abnormal_detector.py 문서

## 개요
통계적 z-score 기반 이상치 감지 모듈입니다. 슬라이딩 윈도우 방식으로 최근 데이터의 분포를 분석하여 이상치 여부를 판정합니다.

**파일 경로**: `script/services/abnormal_detector.py`  
**파일 크기**: 약 160 라인  
**총 클래스**: 1개 (`AbnormalDetector`)  
**총 메서드**: 5개  
**업데이트**: 2025-12-04

---

## 🔥 중요: API 변경 (Breaking Change) - 2025-12-03

### Before (2025-12-02 이전)
```python
is_abnormal = detector.add_and_check_image(width, height)
# Returns: bool
```

### After (2025-12-03)
```python
is_abnormal, z_w, z_h = detector.add_and_check_image(width, height)
# Returns: tuple[bool, Optional[float], Optional[float]]
```

**주요 변경사항:**
- **반환값 확장**: `bool` → `(is_abnormal, z_width, z_height)` 튜플
- **z-score 반환**: UI에서 z-score 표시 가능 (예: `(z:0.5,0.3)`)
- **None 반환**: 판정 보류 시 z-score는 None

---

## 클래스: AbnormalDetector

통계적 z-score 기반 이상치 감지 클래스

### 초기화 파라미터

```python
AbnormalDetector(window_size=100, min_samples=10, threshold=3.0)
```

- `window_size`: 슬라이딩 윈도우 크기 (기본 100)
- `min_samples`: 최소 샘플 수, 이 개수 미만이면 판정 보류 (기본 10)
- `threshold`: z-score 임계값 (기본 3.0 = 3σ)

### 메서드

#### `add_and_check_image(width: int, height: int) -> tuple[bool, Optional[float], Optional[float]]`
- **설명**: 이미지 크기를 버퍼에 추가하고 이상치 여부 판정 + z-score 반환
- **매개변수**: 
  - `width` - 이미지 가로 크기 (픽셀)
  - `height` - 이미지 세로 크기 (픽셀)
- **반환값**: `(is_abnormal, z_width, z_height)` 튜플
  - `is_abnormal` (bool): True (이상치), False (정상 또는 판정 보류)
  - `z_width` (float | None): 가로 크기의 z-score (판정 보류 시 None)
  - `z_height` (float | None): 세로 크기의 z-score (판정 보류 시 None)
- **동작**:
  1. 버퍼에 width/height 추가
  2. 윈도우 크기 초과 시 오래된 데이터 제거
  3. 최소 샘플 수 미만이면 `(False, None, None)` 반환 (판정 보류)
  4. z-score 계산: `z = (value - mean) / std`
  5. `|z| > threshold` 이면 이상치로 판정
  6. z-score 값 반환 (UI 표시용)

**예시 (API 변경 후)**:
```python
detector = AbnormalDetector()

# 처음 9개: 판정 보류 (min_samples=10)
for i in range(9):
    is_abnormal, z_w, z_h = detector.add_and_check_image(200, 150)
    # is_abnormal = False, z_w = None, z_h = None (판정 보류)

# 10번째부터 정상 판정
is_abnormal, z_w, z_h = detector.add_and_check_image(200, 150)
# is_abnormal = False, z_w = 0.0, z_h = 0.0 (정상)

# 튀는 값 발견
is_abnormal, z_w, z_h = detector.add_and_check_image(350, 150)
# is_abnormal = True, z_w = 3.5, z_h = 0.0 (이상치!)

# UI에서 z-score 표시
print(f"200x150 (z:{z_w:.1f},{z_h:.1f})")  # "200x150 (z:0.0,0.0)"
```

---

#### `is_image_abnormal(image_path: str) -> bool`
- **설명**: 이미지 이상치 여부 판정 (레거시 호환성 메서드)
- **매개변수**: `image_path` - 이미지 파일 절대 경로
- **반환값**: True (이상치), False (정상)
- **동작**:
  1. `get_image_dimensions()`로 이미지 크기 조회
  2. 크기를 알 수 없으면 정상으로 간주
  3. 내부적으로 `add_and_check_image()` 호출

---

#### `is_group_abnormal(group: dict) -> bool`
- **설명**: 그룹이 이상치인지 판정
- **매개변수**: `group` - 그룹 데이터 딕셔너리
- **반환값**: True (이상치), False (정상)
- **판정 기준**:
  1. **NIR-only 그룹**: 카메라 데이터 없고 NIR만 있는 경우
- **동작**:
  1. 그룹에서 카메라/NIR 데이터 추출
  2. 카메라 없고 NIR만 있으면 이상치로 판정
  3. 에러 발생 시 정상으로 간주

**그룹 데이터 구조**:
```python
{
    "카메라": {...},  # 일반 카메라 폴더 정보
    "NIR": {...},     # NIR 파일 정보
    ...
}
```

---

#### `_calculate_z_score(value: float, values_list: list) -> Optional[float]`
- **설명**: z-score 계산 (내부 메서드)
- **매개변수**:
  - `value`: 판정할 값
  - `values_list`: 기준이 되는 값들의 리스트
- **반환값**: z-score (float) 또는 None (계산 불가능 시)
- **동작**:
  1. 리스트가 비어있거나 2개 미만이면 None 반환
  2. 평균(mean) 계산: `sum(values) / len(values)`
  3. 분산(variance) 계산: `sum((x - mean)^2) / len(values)`
  4. 표준편차(std) 계산: `sqrt(variance)`
  5. std가 0이면 None 반환 (모든 값이 동일)
  6. z-score 계산: `(value - mean) / std`

**계산식**:
```python
mean = sum(values_list) / len(values_list)
variance = sum((x - mean) ** 2 for x in values_list) / len(values_list)
std = sqrt(variance)

if std == 0:
    return None  # 모든 값이 동일

z_score = (value - mean) / std
```

**특징**:
- **numpy 없이 구현**: 순수 Python으로 계산 (의존성 최소화)
- **0 나누기 방지**: std가 0이면 None 반환
- **빠른 계산**: 리스트 크기가 작아 성능 문제 없음

---

## 이상치 판정 방식

### 통계적 z-score 방식

**개념:**
- 절대값 기준(예: 180px, 220px)이 아닌 **상대적 기준** 사용
- "같은 집단 안에서 얼마나 튀었는지"를 판단
- 채취 조건/지역마다 정상 범위가 달라도 자동 대응

**계산:**
```
z-score = (value - mean) / std

여기서:
- mean: 최근 데이터의 평균
- std: 최근 데이터의 표준편차
```

**판정:**
- `|z| > 3.0` (기본값) → 이상치
- `|z| ≤ 3.0` → 정상
- z = 0: 평균과 동일
- z = 1: 평균보다 1σ 위
- z = -2: 평균보다 2σ 아래

---

### 슬라이딩 윈도우

**동작 방식:**
1. 최근 100개(기본값) 데이터만 유지
2. 새 데이터 들어오면 가장 오래된 데이터 제거
3. 항상 "최근 패턴"을 기준으로 판정

**장점:**
- 라인 조건이 서서히 변해도 자동 적응
- 초기 데이터가 나중에 재평가됨

**예시:**
```
시점 1 (10개): 평균=200, 값=220 → z=1.5 → 정상
시점 50 (50개): 평균=180, 값=220 → z=3.2 → 이상치!
                (분포가 명확해지면서 재평가)
```

---

### NIR-only 그룹 판정

- **조건**: 카메라 파일이 없고 NIR 파일만 존재하는 그룹
- **판정**: 이상치
- **이유**: 정상적인 촬영 프로세스에서는 카메라와 NIR이 함께 생성되어야 함

---

## 사용 예시

### 기본 사용 (슬라이딩 윈도우)

```python
from abnormal_detector import AbnormalDetector

detector = AbnormalDetector()

# 데이터가 실시간으로 들어올 때마다 판정
is_abnormal = detector.add_and_check_image(width=200, height=150)
if is_abnormal:
    print("이상치 감지!")
```

### 레거시 방식 (경로 기반)

```python
# 이미지 경로로 직접 판정 (내부적으로 dimensions 추출)
is_abnormal = detector.is_image_abnormal("/path/to/image.jpg")
if is_abnormal:
    print("이미지 크기 이상 감지")
```

### 그룹 판정

```python
# NIR-only 그룹 확인
group = {
    "카메라": None,
    "NIR": {"filename": "run_120250926T103033.spc"}
}
is_abnormal = detector.is_group_abnormal(group)
if is_abnormal:
    print("NIR-only 그룹 감지")
```

### 커스텀 설정

```python
# 더 엄격한 기준 (2σ)
strict_detector = AbnormalDetector(threshold=2.0)

# 더 큰 윈도우 (200개)
large_window_detector = AbnormalDetector(window_size=200)

# 빠른 판정 (최소 5개)
fast_detector = AbnormalDetector(min_samples=5)
```

---

## 의존성
- `utils.utils.get_image_dimensions`: 이미지 크기 조회 함수

---

## 장점 vs 단점

### ✅ 장점
1. **채취 조건 변화에 자동 대응**: 지역/조건마다 다른 정상 범위 처리
2. **과거 데이터 재평가**: 초기에 놓친 이상치도 나중에 발견 가능
3. **실시간 적응**: 라인 조건이 서서히 변해도 자동 적응
4. **계산 효율**: 100개 데이터 통계 계산은 마이크로초 단위

### ⚠️ 주의사항
1. **초기 판정 불가**: 처음 10개는 판정 보류
2. **동질성 가정**: 같은 윈도우 내 데이터가 동질적이어야 함
3. **threshold 조정 필요**: 실제 데이터로 3.0이 적절한지 검증 필요

---

## 향후 개선 사항

1. **MAD(Median Absolute Deviation) 방식**: 이상치가 많이 섞인 데이터에 더 강건
2. **그룹별 독립 detector**: 라인1/라인2 별도 detector로 더 정확한 판정
3. **로그/통계 수집**: 이상치 유형별 발생 빈도 추적
4. **동적 threshold 조정**: 데이터 품질에 따라 자동 조정
