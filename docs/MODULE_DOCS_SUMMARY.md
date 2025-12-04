# 모듈 문서 요약

본 프로젝트의 모든 모듈에 대한 종합 문서입니다.
모든 모듈에 대한 문서는 docs/modules 에 존재합니다.
---

## 📚 전체 모듈 (28개)

### 코어 모듈

**[monitoring_app.md](monitoring_app.md)** - 메인 윈도우 (약 3000줄)
- MainWindow 클래스, 전체 앱 조정
- UI 이벤트 처리, 파일 작업 실행

**[main.md](main.md)** - 애플리케이션 진입점
- QApplication 초기화, 메인 함수

---

### UI 관련

**[ui_builder.md](ui_builder.md)** - UI 생성 전담 (548줄)
- 위젯 생성, 레이아웃 구성
- 툴바, 탭, 통계바 빌드

**[ui_components.md](ui_components.md)** - 재사용 UI 컴포넌트
- FlowLayout, PathLineEdit, SettingDialog
- ThumbnailWidget, NirInfoWidget

**[log_panel.md](log_panel.md)** - 로그 패널 위젯 (80줄)
- 로그 표시, 상세 로그 다이얼로그

**[preview_dialog.md](preview_dialog.md)** - 이미지 미리보기 (56줄)
- 반응형 크기, 클릭으로 닫기
- DPI 스케일링 지원

**[tooltips.md](tooltips.md)** - 툴팁 관리 (118줄)
- 모든 UI 요소 툴팁 텍스트
- 동적 툴팁 설정/제거

**[drag_select_widget.md](drag_select_widget.md)** - 드래그 선택 위젯 (76줄)
- 마우스 드래그로 다중 행 선택
- 스크롤 영역 연동

---

### 파일 처리

**[file_matcher.md](file_matcher.md)** - 파일 매칭 로직 (384줄)
- 일반 카메라, NIR, 복합 카메라 매칭
- watchdog 기반 실시간 감지
- 타임스탬프 기반 그룹화

**[file_count_worker.md](file_count_worker.md)** - 파일 개수 모니터링 (231줄)
- 백그라운드 파일 카운트
- watchdog 감시, 10초 주기 확인

**[file_operations.md](file_operations.md)** - 파일 작업 워커 (539줄)
- 백그라운드 복사/이동
- 병렬 처리 (ThreadPoolExecutor)
- PermissionError 재시도 로직

**[file_operation_manager.md](file_operation_manager.md)** - 파일 작업 비즈니스 로직 (372줄)
- 입력 검증, 그룹 선택
- NIR 정리, 데이터 구성

---

### 그룹 및 상태 관리

**[group_manager.md](group_manager.md)** - 그룹 생성 로직 (286줄)
- NIR 매칭 (타임스탬프 ±10초)
- 복합 카메라 매칭 (±3초)
- 시간순 정렬 (normal, cam-only, NIR-only 통합)
- 타임스탬프 추출 우선순위: 파일명 → mtime → fallback
- 모든 그룹 타입 생성 후 최종 정렬 수행
- 성능: 1000개 그룹 정렬 < 100ms (실측 ~0.5ms)
- 테스트: test_group_sorting_unit.py (8개 단위 테스트), test_group_sorting_properties.py (7개 속성 테스트, 100회 반복), test_drain_cam_timestamp_handling.py

**[group_state_manager.md](group_state_manager.md)** - 그룹 상태 영속성 (149줄)
- groups.json 저장/로드
- 해시 기반 변경 감지

**[statistics_calculator.md](statistics_calculator.md)** - 통계 계산 (85줄)
- 통합/분리 모드 통계
- total, with_nir, fail 계산

---

### 서비스 레이어 (Phase 2-5)

**[operation_validator.md](operation_validator.md)** - 파일 작업 검증 (108줄)
- 작업 전 입력 검증
- 경로 유효성 확인
- 검증 오류 메시지 생성

**[operation_planner.md](operation_planner.md)** - 파일 작업 계획 (131줄)
- 작업 대상 그룹 결정
- 워커 데이터 구성
- 탭/모드별 데이터 분리

**[nir_pruning_service.md](nir_pruning_service.md)** - NIR 파일 정리 (138줄)
- NIR 파일 개수 제한
- 시간순 정렬 및 삭제
- 안전 삭제 처리

**[statistics_presenter.md](statistics_presenter.md)** - 통계 표시 (204줄)
- 통계 데이터 → UI 문자열 변환
- 통합/분리 모드 포맷팅
- 백분율 자동 계산

**[monitoring_orchestrator.md](monitoring_orchestrator.md)** - 모니터링 조율 (179줄)
- 초기 스캔 조율
- 이벤트 처리 조율
- FileMatcher + GroupManager 협업

---

### 인프라 레이어 (Phase 5)

**[watchdog_manager.md](watchdog_manager.md)** - Watchdog 관리 (173줄)
- Observer 생명주기 관리
- 폴더별 이벤트 핸들러
- 상태 모니터링 및 자동 재시작

---

### 이미지 처리

**[image_manager.md](image_manager.md)** - 이미지 로딩 조정 (234줄)
- ImageLoader 제어
- 가시성 기반 로딩
- 캐시 관리

**[image_loader.md](image_loader.md)** - 백그라운드 이미지 로더
- QThread 기반 비동기 로딩
- 디스크 캐시, 썸네일 최적화
- Pillow 사용

**[image_registry.md](image_registry.md)** - 이미지-위젯 매핑
- Registry Pattern, O(1) 조회
- QPixmap 캐싱

---

### 설정 및 상태

**[config_manager.md](config_manager.md)** - 설정 관리 (173줄)
- config.json 읽기/쓰기
- 플랫폼별 경로 (appdirs)
- 이동 로그 관리

**[window_state_manager.md](window_state_manager.md)** - 윈도우 상태 (63줄)
- 위치/크기 저장/복원
- DPI 스케일링 지원
- QByteArray 방식

---

### 유틸리티

**[utils.md](utils.md)** - 범용 유틸리티 (218줄)
- 타임스탬프 추출 (8가지 패턴)
- LruPixmapCache
- 이미지 크기 조회, 경로 정규화

**[path_utils.md](path_utils.md)** - 경로 유틸리티 (271줄)
- 날짜 검증, 경로 변경 계획
- 폴더 자동 생성
- 썸네일 경로 계산
- `get_effective_path()` - camera 하위폴더 옵션

**[delete_manager.md](delete_manager.md)** - 삭제 관리
- 삭제 폴더로 파일 이동
- 타임스탬프 기반 경로 생성

**[abnormal_detector.md](abnormal_detector.md)** - 이상치 감지 (76줄)
- 이미지 크기 이상 (185px / 210px)
- NIR-only 그룹 감지

---

### 기타

**[nir_app.md](nir_app.md)** - NIR 모니터링 GUI (428줄)
- 독립 실행 NIR 전용 모니터링
- 경로 자동 날짜 업데이트
- watchdog 기반 실시간 감시

**[nir_spectrum_monitor.md](nir_spectrum_monitor.md)** - NIR 스펙트럼 분석 (203줄)
- 김 검출 알고리즘 (pandas 최적화)
- Y 변화 구간 찾기 (0.05-0.1)
- 파일 자동 이동/삭제

**[collect_filenames.md](collect_filenames.md)** - 파일명 수집
- 폴더 구조 분석 유틸리티

**[README.md](README.md)** - 프로젝트 개요
- 설치, 사용법, 개발 가이드

---

## 📊 통계

- **총 모듈**: 34개
- **코어/UI**: 8개
- **파일 처리**: 4개
- **그룹/상태**: 3개
- **서비스 레이어**: 5개 (Phase 2-5)
- **인프라 레이어**: 1개 (Phase 5)
- **이미지**: 3개
- **설정/상태**: 2개
- **유틸리티**: 4개
- **NIR 관련**: 2개 (nir_app, nir_spectrum_monitor)
- **기타**: 2개

**총 코드 라인 수**: 약 10,133줄 (NIR 631줄 포함, Phase 2-5 추가 933줄)

---

## 🔗 의존성 관계

```
monitoring_app (메인)
  ├─ ui_builder (UI 생성)
  ├─ group_state_manager (상태)
  ├─ image_manager (이미지)
  ├─ file_matcher (매칭)
  ├─ file_count_worker (카운트)
  ├─ file_operation_manager (작업 로직)
  ├─ file_operations (작업 워커)
  ├─ statistics_calculator (통계)
  ├─ config_manager (설정)
  ├─ window_state_manager (윈도우)
  ├─ abnormal_detector (이상치)
  ├─ path_utils (경로)
  └─ utils (유틸리티)
```

---

## 🎯 핵심 기능 흐름

### 1. 파일 감시
```
file_matcher (watchdog)
  → 파일 변화 감지
  → 타임스탬프 기반 그룹화
  → monitoring_app에 알림
```

### 2. 이미지 표시
```
monitoring_app
  → image_manager (로딩 조정)
  → image_loader (비동기 로딩)
  → image_registry (위젯 매핑)
```

### 3. 파일 작업
```
monitoring_app
  → file_operation_manager (입력 검증, 그룹 선택)
  → file_operations (워커, 병렬 처리)
  → 완료 후 groups.json 저장
```

### 4. 상태 관리
```
group_state_manager
  → groups.json 저장/로드
  → 해시 기반 변경 감지
  → 앱 시작 시 복원
```

---

## 📝 검증 완료

모든 문서 검증 완료 (100%)
- ✅ 정확: 10개
- ✅ 수정 완료: 2개 (file_matcher, file_count_worker)
- 🗑️ 삭제: 1개 (view_manager - UIBuilder/ImageManager와 중복)
- ✅ 리팩토링: get_effective_path() 중복 제거 → path_utils로 통합
- ✅ NIR 모듈: 2개 (nir_app 정확, nir_spectrum_monitor 신규 작성)
