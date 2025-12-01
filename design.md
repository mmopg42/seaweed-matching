# monitoring_app 리팩토링 설계 문서

## 1. 개요 (Overview)

### 1.1 리팩토링 목적

본 설계는 약 3,000줄에 달하는 단일 스크립트 `monitoring_app.py`를, 기존 28개 모듈을 효과적으로 활용하면서 "책임 단위"로 분리하는 것을 목표로 합니다.

**핵심 목표:**
- 단일 책임 원칙(SRP) 준수: 각 모듈이 명확한 하나의 책임만 담당
- 기존 28개 모듈의 중복 제거 및 효과적 활용
- 새로운 GUI(Rust 기반 계획) 구현 시 비즈니스 로직 재사용 가능한 구조
- 테스트 가능성 향상 및 유지보수성 개선

### 1.2 최종 비전

리팩토링 완료 후, 비즈니스 로직은 GUI 프레임워크와 완전히 독립되어야 하며, 향후 다음과 같은 시나리오를 지원해야 합니다:
- 기존 PySide6 GUI를 그대로 사용
- Rust 기반 새 GUI로 전환 (비즈니스 로직 재사용)
- CLI 도구로 파일 작업 실행
- 외부 공정과의 연동 강화 (groups_state.json 등)

---

## 2. 현재 구조 요약 (Current Situation)

### 2.1 monitoring_app.py의 현황

**파일 크기:** 약 3,000줄 (139KB)  
**클래스:** 2개 (`MainWindow`, `DragSelectWidget`)  
**메서드:** 92개 (MainWindow), 5개 (DragSelectWidget)

**현재 담당 책임:**
1. **UI 초기화 및 레이아웃**: 툴바, 탭, 통계 바, 로그 패널 구성
2. **UI 이벤트 처리**: 버튼 클릭, 입력 필드 변경 등
3. **파일 시스템 감시 제어**: watchdog 시작/중지, 이벤트 큐 관리
4. **파일 매칭 조정**: FileMatcher 제어, provisional NIR 관리
5. **이미지 로딩 및 표시**: ImageManager 제어, 썸네일 업데이트
6. **통계 계산 및 표시**: StatisticsCalculator 호출, UI 업데이트
7. **파일 작업 실행**: execute_file_operation(), NIR 정리, 이동 계획 생성
8. **그룹 상태 관리**: groups_state.json 저장/로드
9. **설정 관리**: ConfigManager 위임, 설정 다이얼로그
10. **윈도우 상태 관리**: WindowStateManager 위임

### 2.2 문제점

1. **과도한 책임 집중**: 한 클래스가 UI, 비즈니스 로직, 상태 관리를 모두 담당
2. **테스트 어려움**: GUI 의존성 없이 비즈니스 로직 테스트 불가능
3. **GUI 종속성**: 비즈니스 로직이 PySide6에 강하게 결합
4. **코드 가독성**: 3,000줄의 단일 파일로 인한 탐색 어려움
5. **확장성 제한**: 새 기능 추가 시 영향 범위 파악 어려움

---

## 3. 목표 아키텍처 개요 (Target Architecture Overview)

### 3.1 레이어 구조

리팩토링 후 시스템은 다음 3개 레이어로 구성됩니다:

```
┌─────────────────────────────────────────────────┐
│         프레젠테이션 레이어 (Presentation)          │
│  - MainWindow (PySide6 GUI)                      │
│  - UIBuilder (UI 생성)                           │
│  - UI Components (재사용 컴포넌트)                 │
├─────────────────────────────────────────────────┤
│         서비스/도메인 레이어 (Service/Domain)       │
│  - MonitoringOrchestrator (감시 조정)             │
│  - TabController (탭/라인 모드 관리)               │
│  - StatisticsPresenter (통계 표시 로직)            │
│  - FileOperationManager (파일 작업 로직)          │
│  - GroupManager (그룹 생성)                      │
│  - StatisticsCalculator (통계 계산)               │
├─────────────────────────────────────────────────┤
│         인프라 레이어 (Infrastructure)             │
│  - FileMatcher (파일 매칭 엔진)                   │
│  - FileOperationWorker (파일 작업 워커)           │
│  - ImageManager (이미지 로딩)                    │
│  - ConfigManager (설정)                          │
│  - GroupStateManager (상태 영속성)                │
│  - WindowStateManager (윈도우 상태)               │
│  - DeleteManager (삭제 관리)                     │
│  - AbnormalDetector (이상치 감지)                 │
└─────────────────────────────────────────────────┘
```

### 3.2 기존 28개 모듈 매핑

#### 프레젠테이션 레이어 (6개)
- `monitoring_app.py` (리팩토링 대상)
- `ui_builder.py` (기존 활용)
- `ui_components.py` (기존 활용)
- `log_panel.py` (기존 활용)
- `preview_dialog.py` (기존 활용)
- `tooltips.py` (기존 활용)

#### 서비스/도메인 레이어 (8개)
- **신규 모듈 (설계 필요):**
  - `monitoring_orchestrator.py` - 전체 감시 프로세스 조정
  - `tab_controller.py` - 탭 및 라인 모드 관리
  - `statistics_presenter.py` - 통계 UI 업데이트 로직

- **기존 모듈 (활용):**
  - `file_operation_manager.py` - 파일 작업 비즈니스 로직
  - `group_manager.py` - 그룹 생성 로직
  - `statistics_calculator.py` - 통계 계산
  - `delete_manager.py` - 삭제 관리
  - `abnormal_detector.py` - 이상치 감지

#### 인프라 레이어 (14개)
- **파일 처리:** `file_matcher.py`, `file_count_worker.py`, `file_operations.py`
- **이미지 처리:** `image_manager.py`, `image_loader.py`, `image_registry.py`
- **상태 관리:** `group_state_manager.py`, `config_manager.py`, `window_state_manager.py`
- **유틸리티:** `utils.py`, `path_utils.py`
- **NIR 관련:** `nir_app.py`, `nir_spectrum_monitor.py`
- **기타:** `collect_filenames.py`

### 3.3 새 GUI 지원 전략

새 GUI (Rust 기반)를 지원하기 위해:
1. **서비스 레이어 독립성**: 모든 비즈니스 로직을 서비스 레이어에 격리
2. **인터페이스 정의**: 각 서비스는 명확한 공개 API를 가짐
3. **데이터 중심 통신**: JSON, 딕셔너리 등 직렬화 가능한 데이터 구조 사용
4. **이벤트 기반 아키텍처**: 상태 변경 시 콜백/시그널을 통한 통지

---

## 4. 모듈 설계 (Module Design)

### 4.1 신규 모듈: MonitoringOrchestrator

**파일명:** `monitoring_orchestrator.py`  
**위치:** 서비스/도메인 레이어

#### 책임 (Responsibility)
- 전체 모니터링 프로세스의 조정 (Run/Stop 로직)
- FileMatcher, FileCountWorker, watchdog의 생명주기 관리
- 파일 이벤트 큐 처리 및 그룹 업데이트 조정

#### Public API
```python
class MonitoringOrchestrator:
    def __init__(self, settings, file_matcher, file_count_worker, 
                 group_state_manager, on_groups_updated_callback)
    
    def start_monitoring() -> bool
    def stop_monitoring() -> None
    def is_monitoring() -> bool
    def refresh() -> None  # 전체 재스캔 + 즉시 UI 반영
    def process_updates(initial=False, force_full_scan=False) -> None
```

#### 기존 모듈 활용
- `file_matcher.py`: FileMatcher, FileMatcherWorker
- `file_count_worker.py`: FileCountWorker
- `group_manager.py`: 그룹 생성 로직
- `group_state_manager.py`: 상태 저장/로드
- `watchdog`: 파일 시스템 감시

#### monitoring_app에서 가져올 책임
- `start_watch()`, `stop_watch()` → `start_monitoring()`, `stop_monitoring()`
- `start_watchdog()`, `stop_watchdog()` → 내부 메서드로 이동
- `handle_file_event()`, `process_event_queue()` → 내부화
- `on_scan_completed()`, `process_updates()` → `process_updates()`
- `refresh_rows_action()` → `refresh()`
- **⚠️ 사용자 승인 필요**: monitoring_app의 해당 메서드들을 삭제하고 orchestrator 위임으로 대체

#### 리스크
- **책임 집중 가능성**: 감시 관련 모든 로직이 이 모듈로 집중될 수 있음 (현재 예상 메서드 수: ~15개)
- **대응책**: watchdog 관련 로직을 별도 `WatchdogManager` 클래스로 분리 고려

---

### 4.2 신규 모듈: TabController

**파일명:** `tab_controller.py`  
**위치:** 서비스/도메인 레이어

#### 책임 (Responsibility)
- 탭 전환 및 라인 모드(통합/분리) 관리
- 탭별 데이터 필터링 (라인1, 라인2, 통합 뷰)
- 현재 활성 탭 추적

#### Public API
```python
class TabController:
    def __init__(self, settings)
    
    def get_current_tab_index() -> int
    def set_current_tab(index: int) -> None
    def is_separated_mode() -> bool
    def filter_groups_for_tab(all_groups: list, tab_index: int) -> dict
        # Returns: {"line1": [...], "line2": [...]}
    def get_visible_layout_for_current_tab() -> str
        # Returns: "line1", "line2", "combined_line1", "combined_line2"
```

#### 기존 모듈 활용
- `config_manager.py`: 라인 모드 설정 조회

#### monitoring_app에서 가져올 책임
- `update_line_mode_ui()` → UI 업데이트 로직은 MainWindow에 남기되, 데이터 필터링 로직은 이동
- 탭별 그룹 분리 로직 (현재 `update_monitoring_view()` 내부)
- **⚠️ 사용자 승인 필요**: 일부 UI 업데이트 로직과 데이터 로직 분리

---

### 4.3 신규 모듈: StatisticsPresenter

**파일명:** `statistics_presenter.py`  
**위치:** 서비스/도메인 레이어

#### 책임 (Responsibility)
- 통계 데이터를 UI 표시용 형식으로 변환
- 통계 UI 업데이트 로직 (칩 생성 제외, 값 업데이트만)
- AbnormalDetector와 StatisticsCalculator 조합

#### Public API
```python
class StatisticsPresenter:
    def __init__(self, statistics_calculator, abnormal_detector)
    
    def calculate_and_prepare_stats(groups: list, line_mode: str) -> dict
        # Returns formatted stats ready for UI display
        # {
        #   "unified": {"total": 10, "with_nir": 5, ...},
        #   "separated": {"line1": {...}, "line2": {...}}
        # }
    
    def format_file_counts(counts: dict) -> dict
        # Formats file count data for UI
```

#### 기존 모듈 활용
- `statistics_calculator.py`: 통계 계산
- `abnormal_detector.py`: 이상치 감지

#### monitoring_app에서 가져올 책임
- `_update_stats()` → 값 설정 로직만 (칩 생성은 UIBuilder에 남김)
- `_update_stats_separated()` → 값 설정 로직만
- `on_file_counts_updated()` → 파일 개수 포맷팅 로직
- **⚠️ 사용자 승인 필요**: 통계 표시 로직 분리, UI 업데이트는 MainWindow에서 presenter를 호출하는 형태

---

### 4.4 FileOperationManager 및 3개 서비스 분리 (R2 승인)

#### 4.4.1 기존 모듈: FileOperationManager (축소)

**파일명:** `file_operation_manager.py` (기존)  
**위치:** 서비스/도메인 레이어
**최종 크기:** ~300줄 (조정 로직만)

**핵심 책임:**
- 파일 작업 전체 흐름 조정 (orchestration)
- 탭/라인 모드별 그룹 선택
- 워커 생성 및 실행

**Public API:**
```python
class FileOperationManager:
    def __init__(self, settings, validator, planner, nir_service, ...)
    
    # 기존 메서드 (유지)
    def select_groups(tab_index, line_mode, data_count_limit) -> dict
    
    # 신규 메서드 (monitoring_app에서 이관)
    def execute_operation(tab_index, line_mode, subject, subject2,
                         nir_count_limit, data_count_limit,
                         on_finished_callback) -> FileOperationWorker
        # 전체 흐름: 검증 → 그룹 선택 → NIR 정리 → 계획 생성 → 워커 실행
```

**의존 서비스:**
- `OperationValidator`: 입력 검증
- `OperationPlanner`: 계획 생성
- `NirPruningService`: NIR 정리
- `FileOperationWorker`: 실제 파일 작업

---

#### 4.4.2 신규 모듈: NirPruningService

**파일명:** `nir_pruning_service.py`  
**위치:** 서비스/도메인 레이어  
**예상 크기:** ~150줄

**책임:**
- NIR 개수 제한 적용
- NIR 베이스명별 묶음 생성
- 오래된 묶음을 삭제 폴더로 이동

**Public API:**
```python
class NirPruningService:
    def __init__(self, delete_manager, log_callback)
    
    def prune_excess_nirs(target_groups: list, keep_count: int,
                         subject: str) -> dict
        # Returns: {"pruned_count": int, "pruned_files": [str]}
```

**기존 모듈 활용:**
- `delete_manager.py`: 삭제 폴더 이동

**monitoring_app에서 가져올 메서드:**
- `prune_nir_files_before_op()` (~70줄)
- `_nir_base()`, `_nir_dt()`

---

#### 4.4.3 신규 모듈: OperationPlanner

**파일명:** `operation_planner.py`  
**위치:** 서비스/도메인 레이어  
**예상 크기:** ~120줄

**책임:**
- move_plan.json 생성
- 그룹 데이터를 파일 작업용 구조로 변환
- with NIR / without NIR 분류

**Public API:**
```python
class OperationPlanner:
    def __init__(self, config_manager)
    
    def create_operation_plan(groups_line1: list, groups_line2: list,
                             subject: str, subject2: str, date: str) -> dict
    
    def save_plan_to_file(plan: dict, filename: str) -> bool
```

**monitoring_app에서 가져올 메서드:**
- `save_move_metadata()`, `save_standalone_metadata()`, `_save_metadata()`
- `_build_file_operation_data()` (일부)

---

#### 4.4.4 신규 모듈: OperationValidator

**파일명:** `operation_validator.py`  
**위치:** 서비스/도메인 레이어  
**예상 크기:** ~150줄

**책임:**
- 입력값 검증 (시료명, 경로 등)
- 그룹 유효성 검사 (완전 매칭 여부)
- 에러 메시지 생성

**Public API:**
```python
class OperationValidator:
    def __init__(self, settings)
    
    def validate_inputs(tab_index: int, line_mode: str,
                       subject: str, subject2: str) -> tuple
        # Returns: (is_valid: bool, errors: list)
    
    def filter_valid_groups(groups: list) -> dict
        # Returns: {"valid": [...], "skipped": [...]}
    
    def is_group_complete(group: dict) -> bool
```

**monitoring_app에서 가져올 메서드:**
- `_validate_file_operation_basic_inputs()`
- `_has_valid_file_entry()`, `_is_group_fully_matched()`
- `_filter_fully_matched_groups()`, `_log_skipped_groups()`

**재사용성:** 다른 모듈에서도 그룹 검증 시 활용 가능

---

### 4.5 기존 모듈 유지: UIBuilder

**파일명:** `ui_builder.py` (기존)  
**동작:** 그대로 유지, DragSelectWidget 분리 고려

#### DragSelectWidget 처리 방침
**결정 (Q1 참조):** 
- DragSelectWidget을 별도 모듈 `drag_select_widget.py`로 분리
- 이유: 책임 분리 및 재사용성 확보
- `ui_components.py`는 이미 여러 재사용 컴포넌트를 포함하므로, 드래그 선택 기능은 독립 모듈이 적합

**새 모듈:** `drag_select_widget.py` (프레젠테이션 레이어)
```python
class DragSelectWidget(QWidget):
    # 기존 monitoring_app.py의 DragSelectWidget 클래스 이동
```

**⚠️ 사용자 승인 필요**: DragSelectWidget을 monitoring_app에서 분리하여 새 모듈 생성

---

### 4.6 기존 모듈 유지/소폭 수정: ImageManager

**파일명:** `image_manager.py` (기존)  
**동작:** 대부분 유지, 일부 조정 메서드 확장

#### monitoring_app에서 가져올 책임 (선택적)
- `refresh_visible_images()` → ImageManager로 이관 가능 (현재는 MainWindow에 있음)
- `is_row_visible()` → ImageManager 또는 별도 유틸리티

**⚠️ 중복 검토 필요 (Q6 참조):**
- 코드 리뷰를 통해 `monitoring_app.py`의 이미지 관련 메서드와 `image_manager.py`의 중복 확인
- 중복 발견 시 통합 방안 문서화

---

### 4.7 삭제 후보 메서드 (GroupStateManager 중복)

**위치:** `monitoring_app.py`

다음 메서드들은 이미 `GroupStateManager`로 위임되어 있으며, wrapper 역할만 하고 있음:
- `_groups_to_canonical_json()` → `GroupStateManager.groups_to_canonical_json()`
- `_calc_group_hash()` → `GroupStateManager.calc_group_hash()`
- `_calc_groups_hash()` → `GroupStateManager.calc_groups_hash()`
- `_maybe_save_groups_json()` → `GroupStateManager.maybe_save_groups_json()`
- `_maybe_load_groups_json()` → `GroupStateManager.maybe_load_groups_json()`

**처리 방침 (Q8 참조):**
- MonitoringOrchestrator 또는 MainWindow에서 `GroupStateManager`를 직접 호출
- **⚠️ 사용자 승인 필요**: 사용자에게 삭제 방침을 알린 후, 사용자가 직접 삭제 수행

---

### 4.8 provisional_nirs 로직 처리

**현재 위치:** `monitoring_app.py`

**처리 방침 (Q10 참조):**
- **완전 제거**: provisional_nirs는 안정화 로직의 잔재이며 현재 시스템에서 불필요
- **⚠️ 사용자 승인 필요**: 제거 전 사용자 확인

---

## 5. 데이터 및 상태 관리 설계

### 5.1 핵심 데이터 흐름

```
┌─────────────────┐
│  파일 시스템      │
└────────┬────────┘
         │ watchdog
         ▼
┌─────────────────┐
│  FileMatcher     │ (파일 감지 및 매칭)
└────────┬────────┘
         │ unmatched files
         ▼
┌─────────────────┐
│ MonitoringOrch   │ (그룹 생성 조정)
│   + GroupMgr     │
└────────┬────────┘
         │ groups (list)
         ▼
┌─────────────────┐
│GroupStateMgr    │ (영속성)
└────────┬────────┘
         │ groups_state.json
         ▼
┌─────────────────┐
│  MainWindow      │ (UI 표시)
└─────────────────┘
```

### 5.2 상태 관리 전략

#### 중앙 상태 (Central State)
- **위치:** `MonitoringOrchestrator`
- **내용:** 
  - `groups: list` - 현재 감지된 모든 그룹
  - `is_monitoring: bool` - 감시 상태
  - `unmatched: dict` - 미매칭 파일들

#### 영속 상태 (Persistent State)
- **담당:** `GroupStateManager`
- **파일:** `groups_state.json`
- **내용:** 앱 종료 후 복원용 그룹 데이터

#### UI 상태 (UI State)
- **담당:** `MainWindow`, `TabController`
- **내용:**
  - 현재 활성 탭
  - 선택된 행
  - 스크롤 위치

#### 설정 상태 (Configuration State)
- **담당:** `ConfigManager`
- **파일:** `config.json`
- **내용:** 모든 사용자 설정

### 5.3 GUI 재사용을 위한 설계

새 GUI (Rust 등)에서 재사용하기 쉽도록:

1. **상태 관리 모듈 분리**: MonitoringOrchestrator가 중앙 상태 관리
2. **콜백 기반 통지**: 상태 변경 시 콜백 함수 호출
3. **직렬화 가능 데이터**: JSON으로 직렬화 가능한 데이터 구조만 사용
4. **GUI 독립적 API**: 모든 서비스 레이어는 PySide6 타입 사용 금지

**예시:**
```python
# Rust GUI에서 사용 가능한 형태
orchestrator = MonitoringOrchestrator(
    settings=load_json("config.json"),
    on_groups_updated=lambda groups: update_ui(groups)
)
orchestrator.start_monitoring()
```

---

## 6. 실행/검증 전략 (Execution & Verification)

### 6.1 모듈 단위 테스트

각 신규 모듈에 대해 단위 테스트 작성:

```bash
conda run -n workspace pytest tests/test_monitoring_orchestrator.py
conda run -n workspace pytest tests/test_tab_controller.py
conda run -n workspace pytest tests/test_statistics_presenter.py
```

**테스트 작성 전략:**
- GUI 의존성 제거: Mock 객체 사용
- 각 공개 메서드에 대한 테스트 케이스 작성
- 예외 상황 테스트 (빈 그룹, 잘못된 입력 등)

### 6.2 통합 테스트

전체 워크플로우 테스트:

```bash
conda run -n workspace pytest tests/test_monitoring_integration.py
```

**테스트 시나리오:**
1. 모니터링 시작 → 파일 추가 → 그룹 생성 확인
2. 통계 계산 → UI 업데이트 확인
3. 파일 작업 실행 → 결과 확인

### 6.3 수동 테스트 체크리스트

리팩토링 후 다음 기능들이 정상 작동하는지 수동 확인:

**기본 기능:**
- [ ] Run 버튼 클릭 → 감시 시작
- [ ] 파일 추가 → 10초 이내 UI에 표시
- [ ] 통계 바 업데이트 (파일 개수, 매칭 현황)
- [ ] 이미지 로딩 및 썸네일 표시
- [ ] 탭 전환 (라인1, 라인2, 통합)

**파일 작업:**
- [ ] Move 버튼 → 파일 이동 성공
- [ ] NIR 개수 제한 적용
- [ ] 데이터 개수 제한 적용
- [ ] Delete 버튼 → 삭제 폴더로 이동

**설정:**
- [ ] 설정 다이얼로그 → 변경 저장
- [ ] 경로 자동 설정 (날짜 기반)
- [ ] 윈도우 크기 저장/복원

### 6.4 성능 검증

리팩토링 전후 성능 비교:

```bash
conda run -n workspace python -m cProfile -o profile_before.prof monitoring_app.py
# (리팩토링 후)
conda run -n workspace python -m cProfile -o profile_after.prof monitoring_app.py
```

**측정 지표:**
- 파일 스캔 속도 (1000개 파일 기준)
- 이미지 로딩 속도
- UI 응답 시간 (버튼 클릭 → 반응)

---

## 7. 비기능 설계 (Non-Functional Design)

### 7.1 유지보수성

#### 코드 구조
- **모듈 크기 제한**: 신규 모듈은 500줄 이하 목표, 최대 700줄
- **메서드 수 제한**: 클래스당 20개 메서드 이하 (임계값)
- **명확한 네이밍**: 모듈명과 클래스명이 책임을 명확히 드러냄

#### 디렉토리 구성
```
matching_codex/
├── monitoring_app.py              # MainWindow (GUI 이벤트 핸들러만)
├── main.py
├── services/                      # 신규 디렉토리 (서비스 레이어)
│   ├── __init__.py
│   ├── monitoring_orchestrator.py
│   ├── tab_controller.py
│   └── statistics_presenter.py
├── ui/                            # UI 관련 모듈 (기존 모듈 이동 고려)
│   ├── ui_builder.py
│   ├── ui_components.py
│   ├── drag_select_widget.py      # 신규
│   ├── log_panel.py
│   ├── preview_dialog.py
│   └── tooltips.py
├── domain/                        # 도메인 로직 (기존 모듈)
│   ├── file_operation_manager.py
│   ├── group_manager.py
│   ├── statistics_calculator.py
│   ├── delete_manager.py
│   └── abnormal_detector.py
├── infrastructure/                # 인프라 레이어 (기존 모듈)
│   ├── file_matcher.py
│   ├── file_operations.py
│   ├── image_manager.py
│   ├── config_manager.py
│   └── ...
└── tests/                         # 테스트
    ├── test_monitoring_orchestrator.py
    ├── test_tab_controller.py
    └── ...
```

**⚠️ 주의:** 디렉토리 재구성은 선택사항이며, 임포트 경로 변경 시 영향 범위가 크므로 사용자와 논의 필요

### 7.2 확장성

#### 플러그인 지점
새 기능 추가 시 확장할 위치:

| 기능 추가 | 확장 위치 | 방법 |
|----------|----------|------|
| 새 파일 타입 매칭 | `FileMatcher` | 매칭 로직 추가 |
| 새 통계 항목 | `StatisticsCalculator` | 계산 로직 추가 |
| 새 파일 작업 | `FileOperationManager` | 작업 타입 추가 |
| 새 UI 컴포넌트 | `ui_components.py` | 컴포넌트 클래스 추가 |

#### 새 GUI 추가 (예: Rust)
1. `services/` 모듈들을 그대로 사용
2. Rust에서 Python 서비스 호출 (PyO3 등)
3. 또는 서비스 로직을 Rust로 포팅

### 7.3 관측 가능성 (Observability)

#### 로그 전략
- **레벨 분리**: DEBUG, INFO, WARNING, ERROR
- **구조화된 로그**: 타임스탬프, 모듈명, 메시지
- **로그 집중화**: 모든 모듈이 중앙 로거 사용

```python
# 예: MonitoringOrchestrator
import logging
logger = logging.getLogger(__name__)

class MonitoringOrchestrator:
    def start_monitoring(self):
        logger.info("모니터링 시작")
        try:
            # ...
        except Exception as e:
            logger.error(f"모니터링 시작 실패: {e}", exc_info=True)
```

#### 에러 핸들링
- **명시적 예외**: 각 모듈별 커스텀 예외 정의
- **복구 전략**: 실패 시 자동 재시도 (watchdog 등)
- **사용자 통지**: 치명적 에러는 대화상자로 표시

---

## 8. 리스크 및 토의 필요 사항 (Risks & Points to Discuss)

### 8.1 설계 상 리스크

#### R1. MonitoringOrchestrator의 책임 집중
**문제:**
- 감시 관련 모든 로직이 `MonitoringOrchestrator`로 집중될 위험
- 예상 메서드 수: ~15개 (임계값 20개 근접)

**완화 방안 (사용자 승인됨):**
- watchdog 관리를 별도 `WatchdogManager` 클래스로 분리 ✅
- 이벤트 큐 처리는 `WatchdogManager` 내부로 통합
- MonitoringOrchestrator는 WatchdogManager를 조율하는 역할만 담당

**WatchdogManager 책임:**
- Observer 생명주기 관리 (start/stop)
- 폴더별 EventHandler 생성 및 관리
- 파일 이벤트 큐 처리
- 재귀 감시 옵션 처리

**배포 환경 참고:**
- 실제 구동 환경: Windows (pyinstaller 패키징)
- 개발 환경: Linux (WSL 등)
- watchdog은 Windows에서 안정적으로 동작하므로 유지
- 추후 개선된 방법 발견 시 WatchdogManager만 교체 가능

**결정:**
- WatchdogManager를 신규 모듈로 생성 (총 5개 신규 모듈)

---

#### R2. FileOperationManager의 파일 크기 증가 (분리 결정) ✅
**문제:**
- 현재 372줄 → 리팩토링 후 약 600~700줄 예상
- NFR-1 (500줄 이하 목표)을 초과할 가능성

**결정 (사용자 승인):**
- **3개 서비스로 완전 분리** 진행
- NIR 정리 로직 → `NirPruningService` (~150줄)
- 이동 계획 생성 → `OperationPlanner` (~120줄)
- 입력 검증 → `OperationValidator` (~150줄)
- FileOperationManager → ~300줄 (조정 로직만)

**결과:**
- 신규 모듈 8개 (5개 제한 + 3개)
- 모든 모듈이 NFR-1 (500줄 이하) 준수
- 각 모듈이 단일 책임 원칙 준수

**상세 설명:** `R2_explanation.md` 참조

---

#### R3. UI와 비즈니스 로직 분리의 복잡도
**문제:**
- 현재 UI 이벤트 핸들러 내에 비즈니스 로직이 혼재
- 완전 분리 시 콜백 체인이 복잡해질 수 있음

**해결 방안 (사용자 승인됨):** ✅
- **동기적 호출 가능한 로직**: 동기로 유지 (직접 호출)
- **비동기 작업만**: 콜백/시그널 사용

**예시:**
```python
# 동기 작업 (통계 계산 등)
def on_button_click(self):
    stats = self.statistics_presenter.calculate_and_prepare_stats(self.groups, "unified")
    self._update_ui_with_stats(stats)  # 동기 호출

# 비동기 작업 (파일 작업 등)
def on_move_button_click(self):
    def on_finished(result):
        self.update_ui_after_operation(result)
    worker = self.file_operation_manager.execute_operation(..., on_finished_callback=on_finished)
    worker.start()
```

**원칙:**
- CPU 집약적이지 않고 즉시 반환되는 로직 → 동기
- I/O 대기, 긴 계산, 파일 작업 → 비동기 (워커 스레드)

---

#### R4. 기존 코드와의 하위 호환성
**해결 방안 (사용자 승인됨):** ✅
- **하위 호환성 불필요**: 임포트 경로를 변경하더라도 진행
- **디렉토리 재구성 진행**: `services/`, `domain/`, `infrastructure/`, `ui/`로 구조화
- **점진적 리팩토링 유지**: Phase별로 진행하되, 각 Phase 완료 시 임포트 경로 일괄 업데이트
- **사용자가 직접 수행**: 임포트 경로 변경 작업은 사용자가 실행

**재구성 계획:**
1. Phase 1 완료 후: 해당 모듈들을 적절한 디렉토리로 이동
2. 모든 임포트 문 업데이트 (사용자 수행)
3. 다음 Phase 진행

**이점:**
- 명확한 레이어 분리로 아키텍처 이해 용이
- 향후 새 GUI 구현 시 서비스 레이어 위치 명확

---

#### R5. 테스트 코드 부재
**문제:**
- 현재 프로젝트에 단위 테스트가 없음
- 리팩토링 후 기능 동일성 검증 어려움

**완화 방안:**
- 리팩토링 전 핵심 기능에 대한 통합 테스트 작성
- 리팩토링 후 각 모듈별 단위 테스트 작성
- 수동 테스트 체크리스트 활용

**토의 필요:**
- 테스트 작성 우선순위 (어떤 모듈부터?)
- 테스트 커버리지 목표 (예: 80%)

---

### 8.2 monitoring_app 수정 관련 결정 필요 사항

#### D1. DragSelectWidget 분리
**현재 상태:** monitoring_app.py 내부 클래스  
**제안:** 별도 모듈 `drag_select_widget.py`로 분리  
**⚠️ 사용자 승인 필요**

---

#### D2. 삭제 후보 메서드 처리 (확정) ✅
**대상:**
- `_groups_to_canonical_json()`, `_calc_group_hash()`, `_calc_groups_hash()`
- `_maybe_save_groups_json()`, `_maybe_load_groups_json()`

**결정:** GroupStateManager를 직접 호출하도록 변경 후 삭제 확정
**이유:** 다른 곳(GroupStateManager)에 이미 구현되어 있어 중복
**수행:** 사용자가 직접 삭제

---

#### D3. provisional_nirs 제거 (확정) ✅
**현재 상태:** monitoring_app.py에 provisional_nirs 딕셔너리 관리 로직 존재  
**결정:** 완전 제거 확정
**이유:** 안정화 로직의 잔재이며 현재 시스템에서 불필요 (Q10 답변)
**수행:** Phase 1에서 말해주면 사용자가 직접 삭제

---

#### D4. execute_file_operation() 이관
**현재 상태:** monitoring_app.py에 약 300줄  
**제안:** FileOperationManager로 완전 이관  
**⚠️ 사용자 승인 필요**

---

#### D5. 이미지 관련 메서드 중복 검토
**대상:**
- `refresh_visible_images()`, `on_image_loaded()`, `refresh_single_image()`
- 이들이 `image_manager.py`와 중복되는지 확인 필요

**제안 (Q6 참조):**
1. **Phase 1 (검토):** 코드 리뷰 수행 → 중복 여부 문서 작성
2. **Phase 2 (결정):** 사용자와 함께 처리 방안 결정
3. **Phase 3 (실행):** 결정에 따라 통합 또는 유지

**⚠️ 사용자 승인 필요**: 페이즈별 진행

---

### 8.3 아키텍처 결정 필요 사항

#### A1 디렉토리 재구성 (사용자 승인됨) ✅
**현재:** 모든 모듈이 루트에 평탄하게 배치  
**결정:** `services/`, `domain/`, `infrastructure/`, `ui/`로 구조화 진행

**장점:**
- 명확한 레이어 분리
- 탐색 용이성
- 새 GUI 구현 시 서비스 레이어 위치 명확

**실행 방안:**
- 각 Phase 완료 시 해당 모듈을 적절한 디렉토리로 이동
- 임포트 경로 일괄 업데이트 (사용자 수행)
- 하위 호환성 고려하지 않음

**최종 구조는 섹션 7.1 참조**

---

#### A2. 새 모듈 개수 (최종 확정 - 8개)
**원래 제한:** 최대 5개 신규 모듈 생성 (requirements.md Q2 참조)
**최종 결정:** 8개 (5개 + R2 추가 3개) ✅

**최종 신규 모듈 목록:**

**Phase 1: 핵심 서비스 (5개)**
1. `monitoring_orchestrator.py` - 감시 프로세스 조정
2. `tab_controller.py` - 탭/라인 모드 관리
3. `statistics_presenter.py` - 통계 표시 로직
4. `drag_select_widget.py` - 드래그 선택 위젯
5. `watchdog_manager.py` - watchdog 생명주기 관리

**Phase 2: 파일 작업 세분화 (3개, R2 승인)**
6. `nir_pruning_service.py` - NIR 정리 전담
7. `operation_planner.py` - 이동 계획 생성
8. `operation_validator.py` - 입력/그룹 검증

**총 8개** (5개 제한 초과, 사용자 승인 완료)

**정당성:**
- 모든 모듈이 NFR-1 (500줄 이하) 준수
- 각 모듈이 명확한 단일 책임
- 재사용성 및 테스트 용이성 향상

---

#### A3. 콜백 vs 시그널/슬롯
**현재:** PySide6 시그널/슬롯 사용  
**제안:** 서비스 레이어는 콜백 함수 사용 (GUI 독립성)

**트레이드오프:**
- 콜백: GUI 독립적이지만 코드 복잡도 증가
- 시그널: PySide6 의존적이지만 코드 간결

**토의 필요:** 서비스 레이어에서 시그널 사용 허용 여부

---

### 8.4 리팩토링 우선순위 (최종 확정)

**Phase 0: 사전 준비** (사용자 수행)
1. 디렉토리 구조 생성: `services/`, `domain/`, `ui/`, `infrastructure/`
2. `__init__.py` 파일 생성

**Phase 1: 독립적, 낮은 위험도** (AI 수행)
1. DragSelectWidget 분리 → `ui/drag_select_widget.py`
2. provisional_nirs 완전 제거 (확정)
3. wrapper 메서드 5개 삭제 위치 명시 (사용자가 직접 삭제)

**Phase 2: 서비스 레이어 구축** (AI 수행)
4. TabController 생성 → `services/tab_controller.py`
5. StatisticsPresenter 생성 → `services/statistics_presenter.py`

**Phase 3: 핵심 조정 로직** (AI 수행)
6. MonitoringOrchestrator 생성 → `services/monitoring_orchestrator.py`
7. WatchdogManager 분리 → `services/watchdog_manager.py`

**Phase 4: 파일 작업 세분화** (AI 수행)
8. OperationValidator 생성 → `domain/operation_validator.py`
9. OperationPlanner 생성 → `domain/operation_planner.py`
10. NirPruningService 생성 → `domain/nir_pruning_service.py`
11. FileOperationManager에 execute_operation() 이관

**Phase 5: 검증 및 최적화**
12. 통합 테스트 작성 (핵심 모듈만)
13. 성능 측정 및 비교
14. 문서 업데이트 (각 모듈 문서 + MODULE_DOCS_SUMMARY.md)

**각 Phase마다:**
- 테스트 수행 (수동 체크리스트, 섹션 6.3)
- 임포트 경로 업데이트 (500줄 이상 파일은 사용자 수행)
- 다음 Phase 진행

---

## 9. 다음 단계 (Next Steps)

### 9.1 사용자 검토 및 승인 (완료) ✅

본 설계 문서 검토 완료:

**승인 완료:**
- [x] 전체 설계 방향 승인
- [x] 신규 모듈 8개 생성 승인 (5개 + 3개)
- [x] DragSelectWidget 분리 승인
- [x] provisional_nirs 완전 제거 승인
- [x] wrapper 메서드 5개 삭제 승인
- [x] MonitoringOrchestrator 책임 범위 (R1) - WatchdogManager 분리
- [x] FileOperationManager 분리 (R2) - 3개 서비스로 완전 분리
- [x] 디렉토리 재구성 (A1) - 점진적 진행
- [x] Phase별 리팩토링 순서 - Phase 0~5 확정

### 9.2 구현 계획 수립

설계 승인 후:
1. 상세 구현 계획서 작성 (`implementation_plan.md`)
2. Phase별 작업 항목 및 예상 시간 산정
3. 테스트 전략 상세화

### 9.3 프로토타입 검증 (선택)

위험도가 높은 다음 항목에 대해 프로토타입 먼저 검증:
- MonitoringOrchestrator 기본 구조
- FileOperationManager 확장

### 9.4 문서 업데이트 계획

리팩토링 완료 시:
- [ ] 각 신규 모듈에 대한 `docs/modules/*.md` 작성
- [ ] `MODULE_DOCS_SUMMARY.md` 업데이트
- [ ] 아키텍처 다이어그램 작성 (Mermaid)
- [ ] 리팩토링 전후 비교 문서

---

## 부록 A: 모듈별 책임 매핑 테이블

| 현재 (monitoring_app.py) | 리팩토링 후 위치 | 비고 |
|-------------------------|----------------|------|
| start_watch(), stop_watch() | MonitoringOrchestrator.start/stop_monitoring() | 이관 |
| start_watchdog(), stop_watchdog() | MonitoringOrchestrator (내부) | 비공개 |
| process_updates() | MonitoringOrchestrator.process_updates() | 이관 |
| update_line_mode_ui() | TabController (데이터) + MainWindow (UI) | 분리 |
| _update_stats() | StatisticsPresenter + MainWindow | 분리 |
| execute_file_operation() | FileOperationManager.execute_operation() | 이관 |
| DragSelectWidget 클래스 | drag_select_widget.py | 분리 |
| wrapper 메서드 (5개) | 삭제, GroupStateManager 직접 호출 | 삭제 |
| provisional_nirs 관련 | 완전 제거 | 삭제 |
| 나머지 UI 이벤트 핸들러 | MainWindow (유지) | 유지 |

---

## 부록 B: 검증 환경

모든 테스트 및 검증은 다음 환경에서 수행:

```bash
conda activate workspace
conda run -n workspace python --version
# Python 3.x

conda run -n workspace python monitoring_app.py
# GUI 실행 테스트

conda run -n workspace pytest tests/
# 단위 테스트 실행
```

---

## 부록 C: 용어 정리

- **Orchestrator (조정자)**: 여러 컴포넌트를 조율하여 전체 프로세스 관리
- **Presenter (표시자)**: 데이터를 UI 표시용 형식으로 변환
- **Controller (제어자)**: 특정 영역(탭, 라인 모드)의 제어 로직 담당
- **레이어 (Layer)**: 아키텍처의 계층 구조 (Presentation, Service, Infrastructure)

---

**문서 버전:** 1.0  
**작성일:** 2025-11-29  
**검토 필요:** 예  
**승인 필요 항목:** 섹션 8.2, 8.3 참조
