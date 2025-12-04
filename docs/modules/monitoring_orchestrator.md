# monitoring_orchestrator.py

## 📋 개요

**파일 경로:** `script/services/monitoring_orchestrator.py`  
**파일 크기:** 190 라인  
**생성:** Phase 5 (2025-11-30)  
**목적:** 모니터링 워크플로우 조율 및 이벤트 처리 중앙 관리  
**업데이트:** 2025-12-04

## 🎯 책임

- 초기 파일 스캔 및 그룹 구성 조율
- 파일 시스템 이벤트 처리 및 그룹 재구성
- FileMatcher와 GroupManager 간 협업 조율
- 모니터링 상태 관리 및 초기화

## 📦 주요 클래스

### `MonitoringOrchestrator`

모니터링 워크플로우를 조율하는 오케스트레이터 클래스.

**초기화:**
```python
MonitoringOrchestrator(
    file_matcher: FileMatcher,
    group_manager: GroupManager,
    settings: dict,
    log_callback: Callable
)
```

**주요 메서드:**

#### `reset_state()`
- 모니터링 상태 초기화
- FileMatcher 상태 리셋

#### `perform_initial_scan(force_full_scan=False) -> dict`
- 초기 전체 파일 스캔 수행
- 그룹 구성 및 통계 계산
- 반환: `{"groups": [...], "stats": {...}}`

**반환 구조:**
```python
{
    "groups": [그룹 리스트],
    "stats": {
        "total": 전체 그룹 수,
        "with_nir": NIR 있는 그룹 수,
        "without_nir": NIR 없는 그룹 수,
        "fail": 누락 발생 그룹 수
    }
}
```

#### `process_file_events(event_queue: list) -> dict`
- 파일 시스템 이벤트 큐 처리
- 이벤트 타입별 처리 (created, modified, moved, deleted)
- 그룹 재구성 및 통계 업데이트
- 반환 구조는 `perform_initial_scan()`과 동일

**이벤트 처리 로직:**
- `created`/`modified`: 파일 추가 및 매칭
- `deleted`/`moved`: 파일 제거 및 그룹 재구성

## 🔄 Phase 5 이전과의 차이

### 이전 (monitoring_app.py 내부)
```python
# monitoring_app.py에서 직접 처리
def process_updates(self, initial=False):
    unmatched = self.file_matcher.scan_and_build_unmatched(...)
    self.groups = self.group_manager.build_all_groups(...)
    # ... 복잡한 통계 계산 로직
```

### 이후 (MonitoringOrchestrator 위임)
```python
# monitoring_app.py는 Orchestrator 호출
result = self.monitoring_orchestrator.perform_initial_scan()
self.groups = result["groups"]
# 통계는 result["stats"]에서 가져옴
```

## 🔗 의존성

**Import:**
- `file_matcher.FileMatcher`
- `group_manager.GroupManager`

**사용처:**
- `monitoring_app.py` (MainWindow 클래스)

## 📊 통계

- **라인 수:** 179줄
- **주요 메서드:** 3개 (reset_state, perform_initial_scan, process_file_events)
- **조율 대상:** FileMatcher, GroupManager

## 💡 설계 특징

1. **Orchestrator 패턴:** 여러 서비스의 협업을 조율
2. **통합 반환 구조:** 그룹 + 통계를 함께 반환하여 일관성 보장
3. **이벤트 기반:** 파일 시스템 이벤트를 그룹 변경으로 변환
4. **상태 관리:** 중앙에서 모니터링 상태 초기화 및 관리

## 🧪 사용 예시

```python
# 초기화
orchestrator = MonitoringOrchestrator(
    file_matcher=self.file_matcher,
    group_manager=self.group_manager,
    settings=self.settings,
    log_callback=self.log_to_box
)

# 상태 초기화
orchestrator.reset_state()

# 초기 스캔
result = orchestrator.perform_initial_scan()
groups = result["groups"]
stats = result["stats"]

# 이벤트 처리
events = [("created", "/path/to/file.bmp", "cam1"), ...]
result = orchestrator.process_file_events(events)
updated_groups = result["groups"]
```

## 🔗 협업 흐름

```
MonitoringOrchestrator
         │
         ├─> FileMatcher.scan_and_build_unmatched()
         │   └─> 파일 수집 및 매칭 준비
         │
         └─> GroupManager.build_all_groups()
             └─> 그룹 구성 및 NIR 매칭
```

## 📝 참고사항

- Phase 5에서 `monitoring_app.py`로부터 분리됨
- 파일 매칭과 그룹 생성의 중간 조율 역할
- UI 업데이트 로직은 여전히 `monitoring_app.py`에 위치

---

**작성일:** 2025-12-01  
**관련 Phase:** Phase 5 (감시 및 오케스트레이션 분리)
