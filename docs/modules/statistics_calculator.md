# statistics_calculator.py 문서

## 개요
그룹 데이터를 기반으로 통계를 계산하는 모듈입니다. 통합 모드와 분리 모드에 따라 다른 통계 산출 방식을 제공합니다.

**파일 경로**: `script/services/statistics_calculator.py`  
**파일 크기**: 85 라인  
**총 클래스**: 1개 (`StatisticsCalculator`)  
**총 메서드**: 2개  
**업데이트**: 2025-12-04

---

## 클래스: StatisticsCalculator

그룹 데이터를 기반으로 통계를 계산하는 클래스

### 메서드

#### `calculate_stats(groups: list) -> dict`
- **설명**: 통합 모드 통계 계산
- **매개변수**: `groups` - 전체 그룹 리스트
- **반환값**: 통계 정보 딕셔너리

**반환 구조**:
```python
{
    'total': int,        # 전체 개수 (카메라 있는 그룹)
    'with_nir': int,     # NIR 있는 개수
    'without_nir': int,  # NIR 없는 개수
    'fail': int,         # 누락발생 개수
    'abnormal': int      # 이상치 개수 (기본값: 0)
}
```

**계산 로직**:
- `total`: 카메라 데이터가 있는 그룹 개수
- `with_nir`: NIR 데이터가 있는 그룹 개수
- `without_nir`: `total - with_nir` (음수 방지)
- `fail`: type이 "누락발생"이거나 카메라 데이터가 없는 그룹
- `abnormal`: 0 (호출하는 쪽에서 별도 계산)

**참고**: 이상치 개수는 `AbnormalDetector`를 필요로 하므로 외부에서 계산하여 업데이트해야 합니다.

---

#### `calculate_stats_separated(line1_groups: list, line2_groups: list) -> dict`
- **설명**: 분리 모드 통계 계산 (라인별)
- **매개변수**:
  - `line1_groups`: 라인1 그룹 리스트
  - `line2_groups`: 라인2 그룹 리스트
- **반환값**: 라인별 통계 정보 딕셔너리

**반환 구조**:
```python
{
    'line1': {
        'total': int,
        'with_nir': int,
        'without_nir': int,
        'fail': int,
        'abnormal': int
    },
    'line2': {
        'total': int,
        'with_nir': int,
        'without_nir': int,
        'fail': int,
        'abnormal': int
    }
}
```

**동작**:
- 라인1과 라인2 그룹을 각각 독립적으로 통계 계산
- 각 라인의 통계는 `calculate_stats()`와 동일한 로직 사용

---

## 사용 예시

### 통합 모드
```python
from statistics_calculator import StatisticsCalculator

calculator = StatisticsCalculator()

groups = [
    {"카메라": {...}, "NIR": {...}},
    {"카메라": {...}},
    {"type": "누락발생"},
]

stats = calculator.calculate_stats(groups)
print(f"전체: {stats['total']}")
print(f"NIR 있음: {stats['with_nir']}")
print(f"NIR 없음: {stats['without_nir']}")
print(f"실패: {stats['fail']}")
```

### 분리 모드
```python
line1_groups = [...]  # 라인1 그룹
line2_groups = [...]  # 라인2 그룹

stats = calculator.calculate_stats_separated(line1_groups, line2_groups)

# 라인1 통계
print(f"라인1 전체: {stats['line1']['total']}")
print(f"라인1 NIR: {stats['line1']['with_nir']}")

# 라인2 통계
print(f"라인2 전체: {stats['line2']['total']}")
print(f"라인2 NIR: {stats['line2']['with_nir']}")
```

---

## 그룹 데이터 구조

```python
# 정상 그룹
{
    "카메라": {"folder_name": "...", ...},  # 일반 카메라
    "NIR": {"filename": "...", ...},        # NIR 파일 (선택적)
    "cam1": {...},                          # 복합 카메라 1 (선택적)
    "line": 1                               # 라인 번호
}

# 실패 그룹
{
    "type": "누락발생"
}
```

---

## 통계 항목 설명

| 항목 | 설명 | 계산 방식 |
|------|------|-----------|
| **total** | 전체 그룹 개수 | 카메라 데이터가 있는 그룹 |
| **with_nir** | NIR 보유 그룹 | NIR 데이터가 있는 그룹 |
| **without_nir** | NIR 미보유 그룹 | total - with_nir |
| **fail** | 누락발생 그룹 | type="누락발생" OR 카메라 없음 |
| **abnormal** | 이상치 그룹 | 외부에서 계산 (기본값: 0) |

---

## MainWindow와의 통합

`MainWindow`는 통계를 계산한 후 UI 업데이트 시 이상치 개수를 추가로 계산합니다:

```python
# monitoring_app.py에서 사용 예시
stats = self.statistics_calculator.calculate_stats(self.groups)

# 이상치 개수 별도 계산
abnormal_count = sum(
    1 for g in self.groups 
    if self.abnormal_detector.is_group_abnormal(g)
)
stats['abnormal'] = abnormal_count

# UI 업데이트
self._update_stats(
    stats['total'],
    stats['with_nir'],
    stats['without_nir'],
    stats['fail']
)
```

---

## 의존성
없음 (순수 Python 표준 라이브러리만 사용)

---

## 향후 개선 사항

1. **이상치 통합**: `AbnormalDetector`를 주입받아 이상치 개수도 직접 계산
2. **추가 통계**: 복합 카메라 개수, 파일 크기 합계 등
3. **성능 최적화**: 대량 그룹 처리 시 리스트 컴프리헨션 최적화
