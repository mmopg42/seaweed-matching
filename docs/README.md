# 파일별 문서 요약

리팩토링 전 현재 파일들의 함수와 기능을 정리한 문서입니다.

## 문서 목록

### 핵심 모듈
1. **[monitoring_app.md](monitoring_app.md)** - 메인 모니터링 애플리케이션
2. **[main.md](main.md)** - 통합 컨트롤러
3. **[file_matcher.md](file_matcher.md)** - 파일 매칭 엔진
4. **[group_manager.md](group_manager.md)** - 그룹 생성 로직

### 파일 작업
5. **[file_operations.md](file_operations.md)** - 파일 이동/복사
6. **[delete_manager.md](delete_manager.md)** - 파일 삭제 관리
7. **[file_count_worker.md](file_count_worker.md)** - 파일 개수 카운트

### UI 관련
8. **[ui_components.md](ui_components.md)** - UI 컴포넌트
9. **[tooltips.md](tooltips.md)** - 툴팁 관리
10. **[log_panel.md](log_panel.md)** - 로그 패널

### 유틸리티
11. **[config_manager.md](config_manager.md)** - 설정 관리
12. **[image_loader.md](image_loader.md)** - 이미지 로딩
13. **[utils.md](utils.md)** - 유틸리티 함수
14. **[collect_filenames.md](collect_filenames.md)** - 파일명 수집 스크립트

---

## 모듈별 주요 기능

### monitoring_app.py (147KB)
- **클래스**: DragSelectWidget, MainWindow, MonitorRow
- **기능**: 
  - 파일 시스템 감시 (watchdog)
  - 실시간 파일 매칭
  - 이미지 썸네일 표시
  - 파일 이동/복사/삭제
  - 설정 관리
  - 로그 출력
- **특징**: 메인 애플리케이션, 비동기 처리, 외부 연동

### file_matcher.py (16KB)
- **클래스**: Communicate, FolderEventHandler, FileMatcher, FileMatcherWorker
- **기능**:
  - 파일 시스템 이벤트 감지
  - 파일 타입별 매칭
  - 안정화 로직 (3초 대기)
  - WSL 환경 지원
- **특징**: 백그라운드 워커, 시그널 기반 통신

### group_manager.py (11KB)
- **클래스**: GroupManager
- **기능**:
  - 일반 카메라 기준 그룹 생성
  - NIR 매칭 (시간 차이 기준)
  - 복합 카메라 매칭 (시간 기반/순차)
  - cam-only 그룹 생성
- **특징**: 시간 기반 매칭 알고리즘

### file_operations.py (24KB)
- **클래스**: FileOperationWorker
- **기능**:
  - 파일 이동/복사
  - 병렬 처리
  - 충돌 처리
  - 롤백 지원
  - 이동 계획 저장
- **특징**: ThreadPoolExecutor, 버킷 규칙

### delete_manager.py (26KB)
- **함수**: ensure_watching_off, ensure_delete_folder, delete_one_row, delete_selected_rows, set_select_all
- **기능**:
  - 안전한 삭제 (감시 OFF 확인)
  - 버킷 규칙 적용
  - 병렬 삭제
  - 이름 충돌 처리
- **특징**: 가드 함수, 사용자 확인

### ui_components.py (34KB)
- **클래스**: FlowLayout_, PathLineEdit, SettingDialog, ThumbnailWidget, NirInfoWidget
- **기능**:
  - 플로우 레이아웃
  - 드래그 앤 드롭 경로 입력
  - 설정 다이얼로그
  - 썸네일 위젯
  - NIR 정보 위젯
- **특징**: 재사용 가능한 UI 컴포넌트

### config_manager.py (6KB)
- **클래스**: ConfigManager
- **기능**:
  - 설정 로드/저장
  - 이동 로그 관리
  - 폴더 열기
  - 경로 관리
- **특징**: appdirs를 사용한 플랫폼 독립성

### image_loader.py (15KB)
- **클래스**: ThumbnailCache, ImageLoaderWorker
- **기능**:
  - 비동기 이미지 로딩
  - 디스크 캐싱
  - Pillow draft 모드 최적화
  - 우선순위 큐
  - 병렬 로딩
- **특징**: ThreadPoolExecutor, QThread

### utils.py (6KB)
- **클래스**: LruPixmapCache
- **함수**: 타임스탬프 추출, 경로 정규화, 메타데이터 저장
- **기능**:
  - 일반/NIR/복합 카메라 타임스탬프 추출
  - 경로 정규화
  - QPixmap LRU 캐시
- **특징**: 정규표현식, OrderedDict

---

## 데이터 흐름

```
파일 생성
  ↓
FolderEventHandler (file_matcher.py)
  ↓
FileMatcher (file_matcher.py)
  ↓
GroupManager (group_manager.py)
  ↓
MainWindow (monitoring_app.py)
  ↓
FileOperationWorker (file_operations.py)
  ↓
이동 완료
```

---

## 설정 파일

- `config.json`: 애플리케이션 설정
- `groups_state.json`: 그룹 상태 (외부 공정 연동)
- `move_plan.json`: 이동 계획 (시료별)
- `moved_subjects.json`: 이동 기록 (날짜별)

---

## 주요 의존성

### Qt 프레임워크
- `PySide6.QtWidgets`: UI 위젯
- `PySide6.QtCore`: QThread, Signal
- `PySide6.QtGui`: QPixmap, QImage

### 파일 시스템
- `watchdog`: 파일 시스템 감시
- `pathlib`: 경로 처리
- `shutil`: 파일 작업

### 병렬 처리
- `concurrent.futures.ThreadPoolExecutor`: 병렬 작업
- `threading`: 동기화

### 기타
- `appdirs`: 플랫폼별 데이터 디렉토리
- `Pillow`: 이미지 최적화
- `yaml`: YAML 파일 읽기

---

## 아키텍처 패턴

1. **비동기 처리**: QThread를 활용한 백그라운드 작업
2. **시그널/슬롯**: Qt 시그널을 통한 스레드 간 통신
3. **병렬 처리**: ThreadPoolExecutor를 통한 파일 작업 병렬화
4. **캐싱**: 디스크/메모리 캐시를 통한 성능 최적화
5. **모듈화**: 기능별 모듈 분리 (UI, 로직, 작업)

---

## 리팩토링 권장 사항

### 1. monitoring_app.py 분리
- **문제**: 147KB, 92개 함수 → 너무 큼
- **권장**: UI, 로직, 워커를 별도 파일로 분리

### 2. 설정 관리 통합
- config_manager, settings dialog, 경로 자동 설정 등 통합

### 3. 워커 클래스 통합
- FileMatcherWorker, FileCountWorker, ImageLoaderWorker의 공통 인터페이스 정의

### 4. 의존성 역전
- 하위 모듈이 상위 모듈(monitoring_app)을 참조하지 않도록 수정

### 5. 테스트 용이성
- 각 모듈의 핵심 로직을 UI와 분리하여 단위 테스트 가능하도록 수정
