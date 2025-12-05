# performance_monitor.py

## 개요

이미지 로딩 성능 모니터링 시스템입니다. 캐시 히트율, 로딩 시간, 에러율 등의 성능 메트릭을 수집하고 분석합니다.

## 책임

- 캐시 히트/미스 카운트 추적
- 이미지 로딩 시간 기록 및 통계 계산
- 에러 및 타임아웃 추적
- 재시도 성공률 모니터링
- 성능 통계 제공

## 주요 클래스

### PerformanceMonitor

이미지 로딩 성능 메트릭을 수집하고 분석하는 클래스입니다.

**주요 메서드:**

- `record_cache_hit()`: 캐시 히트 기록
- `record_cache_miss()`: 캐시 미스 기록
- `record_load_time(duration_ms)`: 로딩 시간 기록
- `record_timeout()`: 타임아웃 발생 기록
- `record_error()`: 에러 발생 기록
- `record_permanent_failure()`: 영구 실패 기록
- `record_retry()`: 재시도 시도 기록
- `record_retry_success()`: 재시도 성공 기록
- `get_cache_hit_rate()`: 캐시 히트율 계산 (0.0 ~ 1.0)
- `get_average_load_time()`: 평균 로딩 시간 계산 (ms)
- `get_median_load_time()`: 중앙값 로딩 시간 계산 (ms)
- `get_percentile_load_time(percentile)`: 백분위수 로딩 시간 계산 (ms)
- `get_error_rate()`: 에러율 계산 (0.0 ~ 1.0)
- `get_retry_success_rate()`: 재시도 성공률 계산 (0.0 ~ 1.0)
- `get_statistics()`: 모든 성능 메트릭을 포함하는 딕셔너리 반환
- `get_summary_string()`: 사용자에게 보여줄 간단한 요약 문자열 반환
- `reset()`: 모든 통계 초기화

**통계 항목:**

캐시 통계:
- cache_hits: 캐시 히트 횟수
- cache_misses: 캐시 미스 횟수
- cache_hit_rate: 캐시 히트율 (0.0 ~ 1.0)

로딩 시간 통계:
- total_loads: 총 로딩 횟수
- avg_load_time_ms: 평균 로딩 시간 (밀리초)
- median_load_time_ms: 중앙값 로딩 시간 (밀리초)
- p95_load_time_ms: 95th percentile 로딩 시간 (밀리초)
- min_load_time_ms: 최소 로딩 시간 (밀리초)
- max_load_time_ms: 최대 로딩 시간 (밀리초)

에러 통계:
- error_count: 에러 횟수
- timeout_count: 타임아웃 횟수
- permanent_failure_count: 영구 실패 횟수
- error_rate: 에러율 (0.0 ~ 1.0)

재시도 통계:
- retry_count: 재시도 횟수
- retry_success_count: 재시도 성공 횟수
- retry_success_rate: 재시도 성공률 (0.0 ~ 1.0)

시스템 통계:
- uptime_seconds: 모니터 가동 시간 (초)
- loads_per_second: 초당 로딩 횟수

## 의존성

**외부 라이브러리:**
- collections.deque: 최근 N개 로딩 시간 기록 유지

**내부 모듈:**
- 없음 (독립적인 모듈)

## 사용 예시

```python
from image.performance_monitor import PerformanceMonitor

# 모니터 초기화
monitor = PerformanceMonitor(max_history=1000)

# 캐시 히트 기록
monitor.record_cache_hit()

# 로딩 시간 기록
monitor.record_load_time(150.0)  # 150ms

# 통계 조회
stats = monitor.get_statistics()
print(f"캐시 히트율: {stats['cache_hit_rate']*100:.1f}%")
print(f"평균 로딩 시간: {stats['avg_load_time_ms']:.0f}ms")

# 요약 문자열 출력
print(monitor.get_summary_string())
```

## 통합

### ImageLoaderWorker 통합

`ImageLoaderWorker`는 `PerformanceMonitor`를 사용하여 이미지 로딩 성능을 자동으로 추적합니다:

```python
# ImageLoaderWorker 초기화 시 PerformanceMonitor 생성
self.performance_monitor = PerformanceMonitor(max_history=1000)

# 캐시 히트/미스 자동 기록
if cached_data:
    self.performance_monitor.record_cache_hit()
else:
    self.performance_monitor.record_cache_miss()

# 로딩 시간 자동 기록
self.performance_monitor.record_load_time(elapsed_ms)

# 에러 자동 기록
self.performance_monitor.record_timeout()
self.performance_monitor.record_error()
self.performance_monitor.record_permanent_failure()

# 재시도 자동 기록
self.performance_monitor.record_retry()
self.performance_monitor.record_retry_success()
```

### UI 통합

성능 통계는 `PerformanceStatsDialog`를 통해 사용자에게 표시됩니다:

```python
from ui.dialogs.performance_stats_dialog import PerformanceStatsDialog

# 성능 통계 다이얼로그 표시
dlg = PerformanceStatsDialog(image_loader, parent)
dlg.exec()
```

## 성능 고려사항

- 로딩 시간 기록은 `deque`를 사용하여 최근 N개만 유지 (메모리 절약)
- 기본값: 최근 1000개 로딩 시간 기록
- 통계 계산은 O(n) 시간 복잡도 (n = 기록된 로딩 시간 개수)
- 백분위수 계산 시 정렬 필요 (O(n log n))

## 테스트

테스트 파일: `tests/test_cache_hit_rate_tracking.py`

**테스트 커버리지:**
- PerformanceMonitor 초기화
- 캐시 히트율 계산 정확성
- 로딩 시간 통계 (평균, 중앙값, 백분위수)
- 에러율 계산
- 재시도 성공률 계산
- 통계 딕셔너리 반환
- 통계 초기화
- ImageLoader 통합
- 성능 요약 문자열 생성
- 로딩 시간 기록 제한

## 관련 문서

- [image_loader.md](./image_loader.md): 이미지 로더 (PerformanceMonitor 사용)
- [performance_stats_dialog.md](../ui/dialogs/performance_stats_dialog.md): 성능 통계 UI

## 버전 히스토리

- 2025-01-XX: 초기 구현 (Task 11.1 - 캐시 히트율 추적)
