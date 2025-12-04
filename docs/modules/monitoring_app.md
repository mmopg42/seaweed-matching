# monitoring_app.py 문서

## 개요
메인 모니터링 애플리케이션으로, 파일 매칭 및 관리를 위한 GUI 인터페이스를 제공합니다.
파일 시스템 감시, 자동 매칭, 이미지 프리뷰, 파일 이동/삭제 기능을 포함합니다.

**파일 경로**: `script/apps/monitoring_app.py` (모듈화 후 이동)  
**파일 크기**: 약 140KB (2,838 라인)  
**총 클래스**: 1개 (`MainWindow`)  
**업데이트**: 2025-12-04

---

## 🔥 최신 변경사항 (2025-12-03 ~ 2025-12-04)

### 1. 디버그 모니터링 추가
- **MemoryMonitor**: 60초마다 메모리 사용량 체크 (80% 임계값)
- **Heartbeat**: 30초마다 상태 로깅 ("살아있음" 증명)
- **logging 모듈**: `print()` → `logging` 전환 (모든 로그가 파일에 기록)

### 2. z-score 표시 기능
- 이미지 크기 옆에 z-score 표시
- 형식: `200x150 (z:0.5,0.3)`
- `AbnormalDetector`의 API 변경 반영: `is_abnormal, z_w, z_h = add_and_check_image(w, h)`

### 3. Stop 버튼 기능 개선
- `stop_watch()`: 타이머 중지, FileMatcher 비활성화, 이벤트 큐 클리어
- `start_watch()`: FileMatcher 활성화
- `is_watching` 플래그로 상태 확인 강화

### 4. NIR 상태 모니터링 (신규)
- `NIRStatusWidget`: NIR 앱의 실행 상태를 메인 앱에 표시
- 파일 기반 IPC로 2초마다 상태 확인

### 5. 모듈화
- 경로 변경: `monitoring_app.py` → `apps/monitoring_app.py`
- `DragSelectWidget` 분리: `ui/drag_select_widget.py`

---

## 임포트 및 의존성

### 외부 라이브러리
- `PySide6`: Qt GUI 프레임워크 (PyQt6 → PySide6 변경)
- `watchdog`: 파일 시스템 감시
- `psutil`: 메모리 모니터링 (MemoryMonitor 사용)

### 내부 모듈 (모듈화 후 경로)

#### Debug 모듈
- `debug.heartbeat`: Heartbeat 모니터링
- `debug.memory_monitor`: 메모리 모니터링

#### Domain 모듈
- `domain.group_manager`: 그룹 매칭 로직
- `domain.file_matcher`: 파일 매칭 엔진
- `domain.file_operation_manager`: 파일 작업 관리
- `domain.group_state_manager`: 그룹 상태 관리

#### Infrastructure 모듈
- `infrastructure.config_manager`: 설정 관리
- `infrastructure.watchdog_manager`: Watchdog 관리

#### Image 모듈
- `image.image_loader`: 이미지 비동기 로딩
- `image.image_manager`: 이미지 관리
- `image.image_registry`: 이미지 레지스트리

#### Services 모듈
- `services.abnormal_detector`: 이상치 감지 (z-score 기반)
- `services.file_operations`: 파일 작업 워커
- `services.file_count_worker`: 파일 개수 카운트
- `services.delete_manager`: 삭제 관리
- `services.monitoring_orchestrator`: 모니터링 조율
- `services.nir_pruning_service`: NIR 정리 서비스
- `services.operation_planner`: 작업 계획
- `services.operation_validator`: 작업 검증
- `services.statistics_presenter`: 통계 표시
- `services.statistics_calculator`: 통계 계산

#### UI 모듈
- `ui.components.ui_components`: UI 컴포넌트 (FlowLayout_, SettingDialog, MonitorRow 등)
- `ui.components.nir_status_widget`: NIR 상태 위젯 (신규)
- `ui.dialogs.preview_dialog`: 미리보기 다이얼로그
- `ui.panels.log_panel`: 로그 패널
- `ui.builders.ui_builder`: UI 빌더
- `ui.drag_select_widget`: 드래그 선택 위젯 (분리됨)
- `ui.utils.tooltips`: 툴팁 관리
- `ui.utils.window_state_manager`: 윈도우 상태 관리

#### Utils 모듈
- `utils.utils`: 유틸리티 함수
- `utils.path_utils`: 경로 유틸리티

---

## 참고사항

### DragSelectWidget 클래스
- **이 클래스는 더 이상 monitoring_app.py에 없습니다**
- **새 위치**: `script/ui/drag_select_widget.py`
- **문서**: `docs/modules/drag_select_widget.md` 참조

---

## 클래스: MainWindow
메인 애플리케이션 윈도우 (핵심 클래스)

### 초기화 및 UI

#### `__init__()`
- **설명**: MainWindow 초기화
- **초기화 컴포넌트**:
  - `config_manager`: 설정 관리자
  - `window_state_manager`: 윈도우 상태 관리자
  - `abnormal_detector`: 이상치 감지기
  - `group_manager`: 그룹 매칭 관리자
  - `file_matcher`: 파일 매칭 엔진
  - `image_registry`: 이미지 레지스트리
  - `view_manager`: 뷰 관리자
  - `delete_manager`: 삭제 관리자
  - `statistics_calculator`: 통계 계산기
  - 각종 워커 스레드 (file_matcher_worker, file_count_worker, image_loader)
  - watchdog 관련 변수
  - UI 상태 변수

#### `init_ui()`
- **설명**: 전체 UI 레이아웃 구성
- **구성 요소**:
  - 상단 툴바 (설정, Run/Stop, 새로고침, Move, Delete 버튼)
  - 입력 필드 (날짜, 시료명, NIR/데이터 개수 제한)
  - 통계 표시 영역 (파일 개수, 매칭 통계)
  - 탭 인터페이스 (라인1, 라인2, 통합 뷰)
  - 로그 패널
- **중첩 함수**:
  - `chip(label_text)`: 통계용 칩 위젯 생성

#### `restore_window_bounds()`
- **설명**: 저장된 윈도우 위치/크기 복원
- **위임**: `window_state_manager.restore_window_bounds()`

#### `save_window_bounds()`
- **설명**: 현재 윈도우 위치/크기 저장
- **위임**: `window_state_manager.save_window_bounds()`

---

### 설정 관리

#### `show_setting_dialog()`
- **설명**: 설정 다이얼로그 표시
- **설정 항목**:
  - 폴더 경로 (NIR, 일반, 복합 카메라, 출력, 삭제)
  - UI 설정 (썸네일 크기, 라인 모드)
  - 매칭 설정 (시간 차이, 복합카메라 시간 범위)
  - 작업 설정 (스캔 간격)

#### `apply_settings(new_settings)`
- **설명**: 설정 적용 및 저장
- **동작**:
  - `config_manager.save()`로 설정 저장
  - UI 업데이트 (라인 모드, 툴팁)
  - 워커 스레드에 설정 전달

#### `path_auto_setting_edit_config()`
- **설명**: 날짜 기반 경로 자동 변경
- **동작**:
  - `today_edit`에 입력된 날짜(YYYYMMDD)를 기준으로
  - 설정 경로들의 날짜 부분을 자동으로 교체
  - 확인 대화상자 표시 후 적용

#### `save_today_date()`
- **설명**: 작업 날짜 저장
- **저장 항목**: `today` (YYYYMMDD)

#### `save_subject_folder()`
- **설명**: 시료명 저장
- **저장 항목**: `subject`

#### `save_subject_folder2()`
- **설명**: 시료명2 저장 (라인2용)
- **저장 항목**: `subject2`

#### `save_nir_count()`
- **설명**: NIR 개수 제한 저장
- **저장 항목**: `nir_count_limit`

#### `save_data_count()`
- **설명**: 데이터 개수 제한 저장
- **저장 항목**: `data_count_limit`

#### `get_effective_normal_path(folder_key: str)`
- **설명**: 일반카메라의 실제 검색 경로 반환
- **매개변수**: `folder_key` - "normal" 또는 "normal2"
- **반환값**: 실제 검색할 경로 (camera 하위폴더 옵션 반영)
- **동작**:
  - `use_camera_subfolder=True`: `base_path/camera` 반환
  - `use_camera_subfolder=False`: `base_path` 반환

#### `should_use_recursive_watch(folder_type: str)`
- **설명**: 폴더 타입에 따라 재귀 감시 여부 결정
- **매개변수**: `folder_type` - "normal", "normal2", "nir" 등
- **반환값**: True (재귀 감시), False (단일 레벨 감시)
- **로직**:
  - 일반카메라 + camera 하위폴더 모드: False
  - 기타: True

---

### UI 업데이트

#### `update_line_mode_ui()`
- **설명**: 라인 모드에 따라 UI 업데이트
- **동작**:
  - 통합 모드: 통합 탭만 표시
  - 분리 모드: 라인1/라인2 탭 표시

#### `update_tooltips()`
- **설명**: 도움말 표시 설정에 따라 툴팁 업데이트
- **동작**:
  - `show_help=True`: 툴팁 설정
  - `show_help=False`: 툴팁 제거

#### `_update_stats(total, with_nir, without_nir, fail)`
- **설명**: 통합 모드 통계 업데이트
- **매개변수**:
  - `total`: 전체 그룹 수
  - `with_nir`: NIR 보유 그룹 수
  - `without_nir`: NIR 미보유 그룹 수
  - `fail`: 실패 그룹 수
- **표시**: FlowLayout_을 사용한 칩 형태

#### `_update_stats_separated(total_line1, with_nir_line1, without_nir_line1, fail_line1, total_line2, with_nir_line2, without_nir_line2, fail_line2)`
- **설명**: 분리 모드 통계 업데이트
- **매개변수**: 라인1/라인2 각각의 통계
- **표시**: 각 라인별 통계 칩

---

### 그룹 상태 관리

#### `_groups_to_canonical_json(groups: list)`
- **설명**: 그룹 리스트를 정규화된 JSON 문자열로 변환
- **반환값**: JSON 문자열 (정렬됨)

#### `_calc_group_hash(group: dict)`
- **설명**: 개별 그룹의 해시 계산
- **용도**: UI 업데이트가 필요한지 판단
- **반환값**: SHA256 해시

#### `_calc_groups_hash(groups: list)`
- **설명**: 전체 그룹 리스트의 해시 계산
- **반환값**: SHA256 해시

#### `_maybe_save_groups_json(groups: list, debounce_ms=300)`
- **설명**: groups_state.json 저장 (디바운싱)
- **매개변수**:
  - `groups`: 그룹 리스트
  - `debounce_ms`: 디바운스 시간 (밀리초)
- **동작**:
  - 변경 감지 (해시 비교)
  - 디바운스 타이머로 중복 저장 방지
  - 파일에 저장

#### `_maybe_load_groups_json()`
- **설명**: 외부 공정이 groups_state.json을 바꿨다면 불러와 UI 반영
- **동작**:
  - 파일 수정 시간 확인
  - 변경된 경우 로드 및 UI 갱신

---

### 파일 매칭 관련

#### `on_scan_completed(unmatched)`
- **설명**: 백그라운드 워커가 풀스캔을 완료했을 때 호출됨 (메인 스레드)
- **매개변수**: `unmatched` - scan_and_build_unmatched()의 결과
- **동작**:
  - provisional 상태 업데이트
  - `process_updates()` 호출

#### `refresh_rows_action()`
- **설명**: 전체 재스캔 후, provisional NIR을 즉시 안정화 승격해서 화면에 바로 반영하는 '새로고침 전용' 함수
- **동작**:
  - 풀스캔 실행
  - provisional NIR 즉시 안정화
  - UI 갱신
  - 화면 최하단으로 스크롤

#### `process_updates(initial=False, force_full_scan=False)`
- **설명**: 매칭 데이터를 UI에 반영
- **매개변수**:
  - `initial`: 초기 스캔 여부
  - `force_full_scan`: 강제 풀스캔 여부
- **동작**:
  - unmatched 데이터에서 그룹 생성
  - 카메라 파일 그룹화
  - NIR 매칭
  - UI 업데이트

---

### 뷰 및 모니터링 행 관리

#### `reset_monitor_rows()`
- **설명**: 모든 모니터링 행 초기화
- **동작**: 각 탭의 스크롤 영역 내 위젯 제거

#### `ensure_rows_for_layout(layout, count)`
- **설명**: 특정 레이아웃에 대해 위젯 재사용 방식으로 필요한 행 수를 확보
- **매개변수**:
  - `layout`: 대상 레이아웃
  - `count`: 필요한 행 수
- **동작**: 삭제 대신 숨기기를 사용하여 위젯 생성/삭제 비용 제거

#### `ensure_rows(count)`
- **설명**: 위젯 재사용 방식으로 필요한 행 수를 확보
- **매개변수**: `count` - 필요한 행 수
- **동작**: 현재 활성 탭에 따라 적절한 레이아웃에 행 확보

#### `update_monitoring_view(update_ui=True)`
- **설명**: 변경 감지 기반 UI 업데이트
- **매개변수**: `update_ui` - True면 전체 UI + 이미지 로드, False면 통계만
- **동작**:
  - 그룹 해시 비교로 변경 감지
  - 변경된 경우에만 UI 갱신
  - 이미지 프리페칭

#### `_update_tab_view(scroll_area, scroll_layout, display_items)`
- **설명**: 개별 탭 뷰 업데이트
- **매개변수**:
  - `scroll_area`: 스크롤 영역
  - `scroll_layout`: 스크롤 레이아웃
  - `display_items`: 표시할 그룹 데이터
- **동작**:
  - 행 수 확보
  - 각 행에 그룹 데이터 업데이트
  - 이미지 로딩

#### `_update_row_widget(row_widget, group, is_visible=True)`
- **설명**: 개별 MonitorRow 위젯 업데이트
- **매개변수**:
  - `row_widget`: MonitorRow 위젯
  - `group`: 그룹 데이터
  - `is_visible`: 가시성 여부
- **중첩 함수**:
  - `_first_name_and_path(d)`: 딕셔너리에서 첫 파일명과 경로 추출
- **동작**:
  - 그룹 정보 표시
  - 썸네일 이미지 로딩
  - 이상치 마킹

#### `is_row_visible(scroll_area, row_widget)`
- **설명**: 행이 화면(viewport)에 보이는지 확인
- **매개변수**:
  - `scroll_area`: QScrollArea 위젯
  - `row_widget`: MonitorRow 위젯
- **반환값**: True (보임), False (안 보임)

---

### 이미지 관리

#### `show_image_preview(thumb_pixmap, image_path)`
- **설명**: 미리보기 다이얼로그 표시
- **매개변수**:
  - `thumb_pixmap`: 썸네일 픽스맵
  - `image_path`: 이미지 파일 경로
- **동작**: PIL + BytesIO로 파일 핸들 즉시 해제

#### `get_placeholder_pixmap()`
- **제거됨**: ImageRegistry의 `get_placeholder_pixmap()` 사용으로 통합됨

#### `get_cached_pixmap(path, priority=5)`
- **설명**: 비동기 이미지 로딩
- **변경**: ImageManager를 통해 캐시 접근
- **동작**:
  - ImageManager에서 캐시 확인
  - 없으면 백그라운드 로더에 요청하고 ImageRegistry에서 플레이스홀더 반환
- **반환값**: QPixmap

#### `on_image_loaded(image_path: str, pixmap: QPixmap, request_id: str = "")`
- **설명**: 이미지 로딩 완료 콜백
- **동작**:
  - ImageRegistry를 통해 메모리 캐시에 저장
  - 즉시 UI 갱신 (디바운싱 제거)

#### `refresh_single_image(image_path: str, pixmap: QPixmap)`
- **설명**: 특정 이미지 경로만 찾아서 즉시 업데이트 (Registry Pattern 적용)
- **최적화**: O(N^2) → O(1)
- **동작**: 이미지 레지스트리 조회 후 위젯 업데이트

#### `refresh_visible_images()`
- **설명**: 화면에 표시된 행들의 이미지를 캐시에서 다시 로드하여 갱신
- **호출 시점**:
  - 새로고침 버튼 클릭 시
  - 이미지 로딩 완료 시 (타이머를 통해)
- **최적화**: 화면에 보이는 행만 업데이트

---

### 스크롤 관리

#### `scroll_to_bottom()`
- **설명**: 현재 활성 탭의 스크롤을 최하단으로 이동

#### `scroll_to_bottom_for_area(scroll_area)`
- **설명**: 특정 스크롤 영역을 최하단으로 이동
- **매개변수**: `scroll_area` - QScrollArea

---

### 파일 시스템 감시

#### `start_watch()`
- **설명**: 감시 시작 (Run 버튼) - 폴더 자동 생성 포함
- **동작**:
  - 시료 폴더 자동 생성
  - file_matcher_worker 활성화
  - file_count_worker 활성화
  - watchdog 시작
  - UI 상태 업데이트

#### `stop_watch()`
- **설명**: 감시 중지 (Stop 버튼)
- **동작**:
  - watchdog 중지
  - file_matcher_worker 비활성화
  - file_count_worker 비활성화
  - UI 상태 업데이트

#### `toggle_watch()`
- **설명**: 하위 호환성을 위해 남겨둔 메서드 (내부에서 사용)
- **동작**: 감시 상태에 따라 start_watch() 또는 stop_watch() 호출

#### `start_watchdog()`
- **설명**: watchdog 파일 시스템 감시 시작
- **동작**:
  - 각 폴더에 대해 FolderEventHandler 생성
  - Observer 스레드 시작
  - 재귀 감시 옵션 반영

#### `stop_watchdog()`
- **설명**: watchdog 중지
- **동작**:
  - Observer 스레드 중지 및 정리

#### `check_watchdog_status()`
- **설명**: Watchdog 상태 확인 및 자동 재시작
- **동작**: Observer 상태 확인 후 필요 시 재시작

#### `handle_file_event(event_type, src_path, folder_type)`
- **설명**: watchdog 이벤트 처리
- **매개변수**:
  - `event_type`: "created", "modified", "deleted" 등
  - `src_path`: 파일 경로
  - `folder_type`: 폴더 타입
- **동작**: 이벤트 큐에 추가

#### `process_event_queue()`
- **설명**: 이벤트 큐 처리
- **동작**: 큐에서 이벤트를 꺼내 FileMatcher에 전달

#### `update_group_on_delete(basename)`
- **설명**: 파일 삭제 시 그룹 업데이트
- **매개변수**: `basename` - 파일 베이스명
- **동작**: 해당 파일을 포함한 그룹 갱신

---

### 파일 개수 업데이트

#### `on_file_counts_updated(nir_count, nir2_count, normal_count, normal2_count, cam1_count, cam2_count, cam3_count, cam4_count, cam5_count, cam6_count)`
- **설명**: 별도 스레드에서 카운트된 파일 개수를 받아서 UI 업데이트
- **매개변수**: 각 폴더의 파일 개수
- **동작**: 통계 라벨 업데이트

---

### 선택 및 삭제

#### `toggle_select_all()`
- **설명**: 전체 선택/해제 토글
- **동작**: 모든 MonitorRow의 선택 상태 반전

#### `on_row_delete_requested(_clicked_row_idx: int)`
- **설명**: 개별 행 삭제 요청 처리
- **매개변수**: `_clicked_row_idx` - 클릭된 행 인덱스
- **동작**: delete_manager에 위임

---

### 파일 작업 (이동/복사)

#### `_nir_base(fname: str)`
- **설명**: NIR 파일명에서 베이스명 추출
- **매개변수**: `fname` - 파일명
- **반환값**: 베이스명 (확장자 제거)

#### `_nir_dt(base: str, any_path: str | None)`
- **설명**: NIR 타임스탬프 추출
- **매개변수**:
  - `base`: NIR 베이스명
  - `any_path`: 파일 경로
- **반환값**: datetime 객체 또는 None

#### `prune_nir_files_before_op(keep_count: int, subject, target_groups: list)`
- **설명**: 이동 대상 그룹의 NIR 타임스탬프 묶음 중 오래된 순으로 keep_count개만 남기고 나머지는 삭제 폴더로 이동
- **매개변수**:
  - `keep_count`: 유지할 NIR 개수
  - `subject`: 시료명
  - `target_groups`: 이동 대상 그룹 목록
- **동작**:
  - NIR 타임스탬프별 그룹핑
  - 오래된 순 정렬
  - keep_count 초과 파일 삭제 폴더로 이동

#### `_has_valid_file_entry(data_dict)`
- **설명**: dict 구조 안에 absolute_path가 있는지 확인
- **매개변수**: `data_dict` - 데이터 딕셔너리
- **반환값**: bool

#### `_is_group_fully_matched(group)`
- **설명**: 일반 카메라 + 모든 cam 슬롯이 채워졌는지 검사 (NIR은 선택사항)
- **매개변수**: `group` - 그룹 데이터
- **반환값**: bool

#### `_filter_fully_matched_groups(groups)`
- **설명**: 완전히 매칭된 그룹만 필터링
- **매개변수**: `groups` - 그룹 리스트
- **반환값**: 필터링된 그룹 리스트

#### `_log_skipped_groups(skipped, line_label="")`
- **설명**: 스킵된 그룹 로그 출력
- **매개변수**:
  - `skipped`: 스킵된 그룹 리스트
  - `line_label`: 라인 라벨

#### `_ensure_minimum_nir(selected_groups, sorted_pool, keep_n, line_label="")`
- **설명**: 이동NIR수 제한: NIR이 있는 데이터를 keep_n개까지만 선택
- **매개변수**:
  - `selected_groups`: 선택된 그룹 리스트
  - `sorted_pool`: 정렬된 그룹 풀
  - `keep_n`: 유지할 NIR 개수
  - `line_label`: 라인 라벨
- **중첩 함수**:
  - `has_nir(group)`: NIR 보유 여부 확인
- **동작**: NIR 있는 그룹을 keep_n개까지 선택

#### `execute_file_operation(clicked_checked=False)`
- **설명**: 파일 이동/복사 실행 (Move 버튼)
- **매개변수**: `clicked_checked` - 체크 여부 (미사용)
- **동작**:
  - 감시 상태 확인
  - 출력 폴더 확인
  - 그룹 필터링 (완전 매칭)
  - NIR/데이터 개수 제한 적용
  - 이동 계획 생성 (move_plan.json)
  - FileOperationWorker 시작
  - 진행 상태 표시
- **중첩 함수**:
  - `_on_finished(msg)`: 작업 완료 처리

#### `_handle_file_conflict(filename: str, src: str, dst: str)`
- **설명**: 파일 충돌 시 사용자에게 확인
- **매개변수**:
  - `filename`: 파일명
  - `src`: 원본 경로
  - `dst`: 대상 경로
- **반환값**: "overwrite", "skip", "cancel" 중 하나

#### `execute_metadata_only_operation()`
- **설명**: 메타데이터만 저장 (파일 이동 없이)
- **동작**: move_plan.json만 생성

#### `save_move_metadata(metadata)`
- **설명**: 이동 메타데이터 저장 (move_plan.json)
- **위임**: `_save_metadata()`

#### `save_standalone_metadata(metadata)`
- **설명**: 독립 메타데이터 저장
- **위임**: `_save_metadata()`

#### `_save_metadata(metadata, filename)`
- **설명**: 메타데이터를 JSON 파일로 저장
- **매개변수**:
  - `metadata`: 메타데이터 딕셔너리
  - `filename`: 파일명

---

### 폴더 관리

#### `_auto_create_subject_folders()`
- **설명**: 시료 폴더 자동 생성 (Run 버튼 클릭 시 호출)
- **동작**:
  - 조용히 실패 (에러 대화상자 없이 로그만)
  - with NIR / without NIR 하위 폴더 자동 생성
- **반환값**: bool (성공 여부)

#### `create_subject_folder()`
- **설명**: 시료 폴더 수동 생성 (설정 다이얼로그에서)
- **동작**: 사용자 확인 후 폴더 생성

#### `open_output_folder_clicked()`
- **설명**: 출력 폴더를 시스템 탐색기에서 열기
- **동작**:
  1. `settings`에서 `"output"` 경로 가져오기
  2. 빈 경로 검증
  3. **경로 정규화**: `os.path.normpath()`를 사용하여 OS에 맞게 경로 변환
     - 혼합된 슬래시(`/`, `\`)를 OS 표준 구분자로 통일
     - 중복된 슬래시 제거
     - 상대 경로 해석
  4. 폴더 존재 여부 확인 (`os.path.isdir()`)
  5. `config_manager.open_folder(path)` 호출하여 탐색기로 열기
- **예외 처리**:
  - 빈 경로 또는 미존재: 경고 메시지 박스 표시

---

### 로그

#### `log_to_box(message)`
- **설명**: 로그 메시지 출력
- **매개변수**: `message` - 로그 메시지
- **동작**: 타임스탬프와 함께 로그 패널에 출력

---

### 상태 저장/복원

#### `closeEvent(event)`
- **설명**: 윈도우 종료 이벤트 처리
- **동작**:
  - 워커 스레드 종료
  - 윈도우 상태 저장
  - 현재 상태 저장

#### `save_current_state()`
- **설명**: 현재 상태 저장
- **동작**:
  - groups_state.json 저장
  - 윈도우 경계 저장

---

## 데이터 구조

### Group (그룹)
```python
{
    "group_id": "G_001",
    "nir": "run_120250926T103033",       # NIR 키 (파일명 아님)
    "norm": "C250926T103030_0",          # 일반 카메라 폴더명
    "cam1": "20250926_103035_001.jpg",   # 복합 카메라 1
    "cam2": "20250926_103035_002.jpg",   # 복합 카메라 2
    "cam3": "20250926_103035_003.jpg",   # 복합 카메라 3
    "line": 1,                           # 라인 번호
    "has_nir": True                      # NIR 보유 여부
}
```

### Unmatched Files
```python
{
    "nir": {"run_120250926T103033": "path/to/file.spc", ...},
    "normal": {"C250926T103030_0": "path/to/folder", ...},
    "cam1": [("20250926_103035_001.jpg", "path/to/file", datetime_obj), ...],
    ...
}
```

### Move Plan (move_plan.json)
```python
{
    "시료명": {
        "with_nir": [
            {
                "group_id": "G_001",
                "normal": {"folder_name": "C250926T103030_0", "absolute_path": "..."},
                "nir": {"filename": "run_120250926T103033.spc", "absolute_path": "..."},
                "cam1": {"filename": "...", "absolute_path": "..."},
                ...
            }
        ],
        "without_nir": [...]
    }
}
```

---

## 주요 워크플로우

### 1. 폴더 감시 시작
```
사용자 [Run 버튼 클릭]
  → start_watch()
  → _auto_create_subject_folders()
  → file_matcher_worker.enable()
  → file_count_worker.enable()
  → start_watchdog()
  → watchdog 시작 (각 폴더 감시)
```

### 2. 파일 감지 및 매칭
```
파일 생성/변경 감지 (watchdog)
  → FolderEventHandler.on_any_event()
  → comm.file_changed 시그널
  → MainWindow.handle_file_event()
  → process_event_queue()
  → file_matcher.add_or_update_file()
  → 3초 대기 (안정화)
  → 매칭 로직 실행
  → scan_completed 시그널 발생
  → MainWindow.on_scan_completed()
  → process_updates()
  → update_monitoring_view()
  → UI 갱신
```

### 3. 파일 이동
```
사용자 [Move 버튼 클릭]
  → execute_file_operation()
  → 그룹 필터링 (완전 매칭)
  → NIR 개수 제한 적용 (prune_nir_files_before_op)
  → 데이터 개수 제한 적용
  → 이동 계획 생성 (move_plan.json)
  → FileOperationWorker 시작
  → 병렬 파일 작업 실행
  → _on_finished()
  → 성공/실패 로그 출력
```

### 4. 파일 삭제
```
사용자 [Delete 버튼 클릭]
  → delete_manager.delete_selected_rows()
  → 감시 OFF 확인
  → 삭제 폴더 확인
  → 사용자 확인
  → 버킷 규칙에 따라 삭제 폴더로 이동
```

---

## 설정 항목

### 폴더 경로
- `normal`: 일반 카메라 (라인1)
- `normal2`: 일반2 카메라 (라인2)
- `nir`: NIR 파일 (라인1)
- `nir2`: NIR2 파일 (라인2)
- `cam1~3`: 복합 카메라 1~3 (라인1)
- `cam4~6`: 복합 카메라 4~6 (라인2)
- `output`: 이동 대상 폴더
- `delete`: 삭제 폴더

### UI 설정
- `img_width/height`: 썸네일 크기
- `nir_width/height`: NIR 정보 영역 크기
- `line_mode`: 통합/분리 모드
- `use_camera_subfolder`: camera 하위폴더 사용 여부
- `show_help`: 도움말 표시 여부

### 매칭 설정
- `use_cam_time_matching`: 복합카메라 시간 기반 매칭 사용 여부
- `nir_match_time_diff`: NIR 매칭 시간 차이 허용 범위 (초)
- `cam_match_min_diff`: 복합카메라 최소 시간 차이 (초, 기본: 4.0)
- `cam_match_max_diff`: 복합카메라 최대 시간 차이 (초, 기본: 6.0)

### 작업 설정
- `scan_interval`: 스캔 간격 (초)
- `subject`: 시료명 (라인1)
- `subject2`: 시료명 (라인2)
- `today`: 작업 날짜 (YYYYMMDD)
- `nir_count_limit`: NIR 개수 제한 (0=전체)
- `data_count_limit`: 데이터 개수 제한 (0=전체)

---

## 파일 간 통신

### 시그널
- `file_matcher.scan_completed`: 매칭 완료 시
- `file_count_worker.counts_updated`: 파일 개수 업데이트 시
- `image_loader.image_loaded`: 이미지 로드 완료 시
- `file_operation_worker.finished`: 파일 작업 완료 시
- `file_operation_worker.progress`: 작업 진행 상태 업데이트

### JSON 파일
- `config.json`: 설정 저장
- `groups_state.json`: 그룹 상태 저장 (외부 공정과 공유)
- `move_plan.json`: 이동 계획 (시료별)
- `moved_subjects.json`: 이동 기록 (날짜별)

---

## 주요 특징

1. **비동기 처리**: QThread를 활용한 백그라운드 작업
2. **실시간 감시**: watchdog를 통한 파일 시스템 감시
3. **안정화 로직**: 파일 생성 후 3초 대기 (NIR은 즉시 처리)
4. **이미지 최적화**: 썸네일 캐싱 및 비동기 로딩, Registry Pattern으로 O(1) 조회
5. **충돌 방지**: 해시 기반 변경 감지 및 디바운싱
6. **위젯 재사용**: 위젯 생성/삭제 대신 show/hide로 성능 개선
7. **외부 연동**: groups_state.json을 통한 외부 공정 연동
8. **이상치 감지**: NIR-only 그룹 및 이미지 크기 이상 자동 감지 (AbnormalDetector)
9. **통계 표시**: 전체/성공/실패/이상치 개수를 칩 형태로 표시
10. **설정 가능한 시간 범위**: 복합카메라 매칭 시간 범위를 사용자가 직접 설정 가능
11. **모듈화 설계**: 기능별로 독립된 모듈로 분리 (ViewManager, DeleteManager, StatisticsCalculator 등)

---

## 성능 최적화

### 이미지 로딩
- **비동기 로딩**: ImageLoaderWorker로 백그라운드 처리
- **메모리 캐싱**: 로드된 이미지를 메모리에 캐시
- **우선순위 큐**: 화면에 보이는 이미지 우선 로딩
- **Registry Pattern**: O(N^2) → O(1) 이미지 업데이트

### UI 업데이트
- **변경 감지**: 해시 기반 변경 감지로 불필요한 업데이트 방지
- **디바운싱**: 짧은 시간 내 중복 저장 방지
- **위젯 재사용**: show/hide로 생성/삭제 비용 제거
- **Viewport 체크**: 화면에 보이는 행만 업데이트

### 파일 작업
- **병렬 처리**: FileOperationWorker로 백그라운드 병렬 작업
- **진행 상태 표시**: 실시간 진행 상태 업데이트
- **롤백 지원**: 실패 시 롤백

---

## 이상치 감지 기준

### 1. NIR-only 그룹
- **조건**: 카메라 파일이 없고 NIR 파일만 존재하는 그룹
- **판정**: 이상치로 분류
- **이유**: 정상적인 촬영 프로세스에서는 카메라와 NIR이 함께 생성되어야 함

### 2. 이미지 크기 이상
- **조건**: 일반 카메라 이미지의 크기가 다음 범위를 벗어남
  - 가로 ≤ 185px
  - 세로 ≥ 210px
- **판정**: 이상치로 분류
- **이유**: 정상 촬영 이미지의 예상 크기 범위를 벗어남

### 통계 표시
- **통합 모드**: 전체 그룹의 이상치 개수 표시
- **분리 모드**: 라인1/라인2 각각의 이상치 개수 표시

---

## 복합카메라 시간 기반 매칭

### 매칭 범위 설정
사용자가 설정에서 복합카메라 시간 매칭 범위를 직접 지정 가능:
- **최소 시간 차이** (`cam_match_min_diff`): 기본값 4.0초
- **최대 시간 차이** (`cam_match_max_diff`): 기본값 6.0초

### 매칭 조건
```python
일반카메라 타임스탬프: 2025-09-26 10:30:30
복합카메라 타임스탬프: 2025-09-26 10:30:35

시간 차이: 5.0초
cam_match_min_diff: 4.0초
cam_match_max_diff: 6.0초

4.0 ≤ 5.0 ≤ 6.0  →  매칭 성공
```

### 매칭 모드
1. **시간 기반 매칭** (`use_cam_time_matching=True`):
   - 일반카메라와 복합카메라의 타임스탬프 차이로 매칭
   - 설정된 시간 범위 내의 파일만 매칭
   - 범위 밖의 파일은 cam-only 그룹으로 생성

2. **순차 매칭** (`use_cam_time_matching=False`):
   - 시간 무시, 큐에서 순서대로 1:1 매칭
   - 먼저 들어온 파일 순서대로 처리
