# statistics_presenter.py

## 📋 개요

**파일 경로:** `script/services/statistics_presenter.py`  
**파일 크기:** 약 100 라인  
**생성:** Phase 3 (2025-11-29)  
**목적:** 통계 데이터를 UI 표시 형식으로 변환하는 Presenter  
**업데이트:** 2025-12-04

## 🎯 책임

- 통계 데이터를 UI 표시용 문자열로 포맷팅
- 통합 모드 vs 분리 모드 통계 처리
- 파일 개수 통계 포맷팅

## 📦 주요 클래스

### `StatisticsPresenter`

통계 데이터를 UI에 적합한 형식으로 변환하는 Presenter 클래스.

**초기화:**
```python
StatisticsPresenter()
```

**주요 메서드:**

#### `format_unified_stats(total, with_nir, without_nir, fail) -> dict`
- 통합 모드 통계를 UI 레이블용 문자열로 변환
- 반환: 각 레이블의 텍스트 딕셔너리

**반환 구조:**
```python
{
    "lbl_total": "총 25개",
    "lbl_with_nir": "NIR 있음: 20개 (80%)",
    "lbl_without_nir": "NIR 없음: 3개 (12%)",
    "lbl_fail": "누락발생: 2개 (8%)"
}
```

#### `format_separated_stats(...) -> dict`
- 분리 모드 통계를 라인별로 포맷팅
- 라인1과 라인2 통계를 각각 처리

**반환 구조:**
```python
{
    "lbl_total_line1": "총 12개",
    "lbl_with_nir_line1": "NIR 있음: 10개 (83%)",
    "lbl_without_nir_line1": "NIR 없음: 1개 (8%)",
    "lbl_fail_line1": "누락발생: 1개 (8%)",
    "lbl_total_line2": "총 13개",
    ...
}
```

#### `format_file_counts(...) -> dict`
- 파일 개수 통계를 포맷팅
- 통합/분리 모드에 따라 다른 형식 반환

**반환 구조:**
```python
{
    "lbl_nir_count": "NIR: 45개",
    "lbl_normal_count": "일반: 50개",
    "lbl_cam_count": "복합: 150개 (1:50/2:50/3:50)"
}
```

## 🔄 Phase 3 이전과의 차이

### 이전 (monitoring_app.py 내부)
```python
# UI 업데이트 로직 내부에 포맷팅 산재
self.lbl_total.setText(f"총 {total}개")
percent = (with_nir / total * 100) if total > 0 else 0
self.lbl_with.setText(f"NIR 있음: {with_nir}개 ({percent:.0f}%)")
# ...
```

### 이후 (StatisticsPresenter 사용)
```python
# 깔끔한 분리
display_data = self.statistics_presenter.format_unified_stats(
    total, with_nir, without_nir, fail
)
self.lbl_total.setText(display_data["lbl_total"])
self.lbl_with.setText(display_data["lbl_with_nir"])
# ...
```

## 🔗 의존성

**Import:** 없음 (순수 Python)

**사용처:**
- `monitoring_app.py` (_update_stats, _update_stats_separated)

## 📊 통계

- **라인 수:** 204줄
- **주요 메서드:** 3개 (format_unified_stats, format_separated_stats, format_file_counts)
- **지원 모드:** 2가지 (통합, 분리)

## 💡 설계 특징

1. **Presenter 패턴:** 데이터와 표현을 분리
2. **일관된 포맷:** 모든 통계를 동일한 형식으로 반환
3. **백분율 계산:** 자동으로 퍼센티지 계산 및 포맷팅
4. **Zero-safe:** 0 나누기 방지

## 🧪 사용 예시

```python
presenter = StatisticsPresenter()

# 통합 모드 통계
stats = presenter.format_unified_stats(
    total=25, with_nir=20, without_nir=3, fail=2
)
print(stats["lbl_total"])  # "총 25개"

# 분리 모드 통계
stats = presenter.format_separated_stats(
    total_line1=12, with_nir_line1=10, ...
)
```

---

**작성일:** 2025-12-01  
**관련 Phase:** Phase 3 (통계 표시 로직 분리)
