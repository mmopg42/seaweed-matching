# 구현 진행 상황

## Phase 1: NIR 모니터링 폴더 열기 기능 ✅ 완료

### T1: NIR 앱에 폴더 열기 메서드 추가 ✅ 완료
**구현 내용**:
- [x] `nir_app.py`에 `subprocess`, `platform` import 추가
- [x] `NIRMonitorApp` 클래스에 `open_folder(edit_widget: QLineEdit)` 메서드 추가
- [x] 경로 검증 로직 구현 (비어있음, 존재하지 않음)
- [x] 플랫폼 감지 로직 구현 (Windows, macOS, Linux)
- [x] 플랫폼별 폴더 열기 API 호출
  - Windows: `os.startfile(path)`
  - macOS: `subprocess.run(["open", path])`
  - Linux: `subprocess.run(["xdg-open", path])`
- [x] 에러 처리 및 로그 출력 (FileNotFoundError, PermissionError, Exception)

**파일**: [nir_app.py:240-270](nir_app.py#L240-L270)

### T2: NIR 앱 UI에 폴더 열기 버튼 추가 ✅ 완료
**구현 내용**:
- [x] 모니터링 폴더 섹션에 "폴더열기" 버튼 추가
  - 버튼 생성 및 시그널 연결: `lambda: self.open_folder(self.monitor_path_edit)`
- [x] 이동 폴더 섹션에 "폴더열기" 버튼 추가
  - 버튼 생성 및 시그널 연결: `lambda: self.open_folder(self.move_path_edit)`
- [x] 기존 UI 레이아웃 유지

**파일**: [nir_app.py:117-139](nir_app.py#L117-L139)

**UI 레이아웃**:
```
[NIR 파일 감시 폴더:] [경로 입력란.......] [찾아보기] [폴더열기]
[김 검출 파일 이동 폴더:] [경로 입력란.......] [찾아보기] [폴더열기]
```

---

## Phase 2: 카메라 폴더 열기 기능 수정 ✅ 완료

### T3: 카메라 폴더 열기 버튼 동작 검증 ✅ 완료
**검증 결과**:
- [x] cam1~cam6 모두 "폴더열기" 버튼 존재 확인
- [x] 버튼이 open_folder() 메서드에 연결됨 확인
- [x] lambda 클로저 문제 없음 (`e=edit` 사용)
- [x] subprocess, platform import 확인

**발견된 문제점**:
- ❌ 에러 처리 부족 (print만 하고 사용자 피드백 없음)
- ❌ os.path.isdir 체크 누락
- ❌ subprocess.Popen 사용 (NIR 앱과 불일치)
- ❌ 경로 유효성 체크 시 사용자 알림 없음

**파일**: [ui_components.py:224-415](ui_components.py#L224-L415)

### T4: 카메라 폴더 열기 기능 수정 ✅ 완료
**구현 내용**:
- [x] QMessageBox import 추가
- [x] open_folder() 메서드 개선
  - 경로 비어있음: QMessageBox.warning
  - 경로 존재하지 않음: QMessageBox.warning
  - 폴더가 아님: QMessageBox.warning 추가
  - 플랫폼 감지 로직 명확화 (system 변수)
  - subprocess.run(..., check=True) 사용 (NIR 앱과 일관성)
  - FileNotFoundError: QMessageBox.critical
  - PermissionError: QMessageBox.critical
  - Exception: QMessageBox.critical
- [x] cam1~cam6 모두 동일한 로직 적용

**파일**: [ui_components.py:397-430](ui_components.py#L397-L430)

**개선 효과**:
- ✅ 사용자에게 명확한 에러 메시지 제공
- ✅ NIR 앱과 동일한 방식으로 통일
- ✅ 모든 카메라에서 일관된 동작 보장

---

## Phase 3: 일반카메라 camera 하위폴더 옵션 ✅ 완료

### T5: 설정 다이얼로그에 camera 하위폴더 체크박스 추가 ✅ 완료
**구현 내용**:
- [x] `ui_components.py`의 `SettingDialog.__init__`에 체크박스 변수 추가
  - `self.use_camera_subfolder_normal`: 일반 폴더용 체크박스
  - `self.use_camera_subfolder_normal2`: 일반2 폴더용 체크박스
- [x] 일반 폴더 입력란 아래에 체크박스 UI 추가
- [x] 일반2 폴더 입력란 아래에 체크박스 UI 추가
- [x] 툴팁 추가: "체크 시: {경로}/camera/ 하위에서 검색\n해제 시: {경로}/ 직하위에서 검색"
- [x] 20px 들여쓰기로 시각적 계층 구조 표현

**파일**: [ui_components.py:233-235, 267-277, 311-321](ui_components.py#L233-L235)

### T6: camera 하위폴더 옵션 설정 저장/로드 ✅ 완료
**구현 내용**:
- [x] `get_settings()` 메서드에 체크박스 상태 추가
  - `"use_camera_subfolder_normal"`: 일반 폴더 체크박스 상태
  - `"use_camera_subfolder_normal2"`: 일반2 폴더 체크박스 상태
- [x] `monitoring_app.py`의 `show_setting_dialog()`에서 설정 로드
  - 기본값 False로 하위 호환성 보장

**파일**:
- [ui_components.py:483-484](ui_components.py#L483-L484)
- [monitoring_app.py:913-915](monitoring_app.py#L913-L915)

### T7: 일반카메라 실제 경로 계산 로직 구현 ✅ 완료
**구현 내용**:
- [x] `monitoring_app.py`의 `MainWindow` 클래스에 `get_effective_normal_path()` 메서드 추가
- [x] 메서드 기능:
  - 인자: `folder_key` ("normal" 또는 "normal2")
  - 반환: 실제 검색할 경로
  - 체크박스 OFF: `{base_path}` 반환
  - 체크박스 ON + camera 폴더 존재: `{base_path}/camera/` 반환
  - 체크박스 ON + camera 폴더 없음: 경고 로그 + `{base_path}` 반환
- [x] 에러 처리 및 사용자 피드백 (log_to_box)

**파일**: [monitoring_app.py:968-993](monitoring_app.py#L968-L993)

### T8: FileMatcher에 camera 하위폴더 로직 적용 ✅ 완료
**구현 내용**:
- [x] `file_matcher.py`의 `FileMatcher` 클래스에 `get_effective_path()` 메서드 추가
  - 기본 경로와 camera 하위폴더 옵션을 받아 실제 검색 경로 반환
  - camera 하위폴더가 존재하지 않으면 기본 경로 사용
- [x] `scan_and_build_unmatched()` 메서드 수정
  - normal, normal2 경로 가져올 때 camera 하위폴더 옵션 적용
  - `get_effective_path()` 메서드 사용하여 실제 검색 경로 계산
- [x] `file_count_worker.py`의 `FileCountWorker` 클래스에 `get_effective_path()` 메서드 추가
- [x] `start_watchdog()` 메서드 수정
  - normal, normal2 폴더 감시 시작 시 camera 하위폴더 옵션 적용
- [x] `run()` 메서드 수정
  - 파일 개수 카운트 시 camera 하위폴더 옵션 적용

**파일**:
- [file_matcher.py:61-79, 183-186](file_matcher.py#L61-L79)
- [file_count_worker.py:50-68, 105-108, 146-152](file_count_worker.py#L50-L68)

**효과**:
- ✅ 협력업체 프로그램의 camera 하위폴더 구조 지원
- ✅ 체크박스 ON 시 {경로}/camera/ 하위에서 파일 검색
- ✅ 체크박스 OFF 시 {경로}/ 직하위에서 파일 검색
- ✅ FileMatcher와 FileCountWorker 모두 일관된 경로 사용

---

## Phase 4: Watchdog 딥스캔 제한 ✅ 완료

### T9: Watchdog recursive 옵션 제어 로직 구현 ✅ 완료
**구현 내용**:
- [x] `monitoring_app.py`의 `MainWindow` 클래스에 `should_use_recursive_watch()` 메서드 추가
  - 폴더 타입에 따라 재귀 감시 여부 결정
  - camera 하위폴더 사용 시 단일 레벨 감시 (recursive=False)
  - 그 외의 경우 재귀 감시 (recursive=True)
- [x] `start_watchdog()` 메서드 수정
  - normal, normal2의 경우 `get_effective_normal_path()` 사용하여 실제 경로 계산
  - `should_use_recursive_watch()` 호출하여 recursive 옵션 결정
  - 각 폴더에 대해 recursive 옵션을 다르게 적용
  - 로그에 감시 모드 출력 ("재귀 감시" 또는 "단일 레벨 감시")

**파일**: [monitoring_app.py:995-1014, 1632-1648](monitoring_app.py#L995-L1014)

**효과**:
- ✅ camera 하위폴더 모드에서 불필요한 깊은 폴더 감시 제거
- ✅ CPU 사용량 감소 (재귀 감시 비활성화)
- ✅ 파일 시스템 이벤트 처리 부하 감소

### T10: Watchdog 이벤트 핸들러 깊이 필터링 ✅ 완료
**구현 내용**:
- [x] `file_matcher.py`에 `Path` import 추가
- [x] `FolderEventHandler.__init__`에 `settings` 파라미터 추가
- [x] `should_ignore_deep_folder()` 메서드 추가
  - camera 하위폴더 모드에서 경로 깊이 계산
  - camera/ 아래 2단계 이상 경로 무시 (camera/C_xxx/까지만 허용)
  - 상대 경로 parts 개수로 깊이 판단
- [x] `on_any_event()` 메서드 수정
  - 이벤트 처리 전에 `should_ignore_deep_folder()` 호출
  - 깊은 경로의 이벤트는 조기 반환하여 무시
- [x] `monitoring_app.py`의 `start_watchdog()` 수정
  - FolderEventHandler 생성 시 settings 전달

**파일**:
- [file_matcher.py:6, 19-80](file_matcher.py#L19-L80)
- [monitoring_app.py:1643](monitoring_app.py#L1643)

**효과**:
- ✅ 이중 안전장치: recursive=False + 깊이 필터링
- ✅ 예상치 못한 깊은 폴더 이벤트 차단
- ✅ 이벤트 처리 로직 단순화 및 성능 향상

---

## Phase 5: 통합 테스트 및 마무리 (진행 예정)

### T11: 통합 테스트 (대기 중)
### T12: 코드 리뷰 및 리팩토링 (대기 중)
### T13: 문서 업데이트 (대기 중)

---

## 변경 파일 목록

### 수정된 파일
- [x] `nir_app.py` - NIR 모니터링 앱에 폴더 열기 기능 추가
- [x] `ui_components.py` - 카메라 폴더 열기 기능 개선 + camera 하위폴더 체크박스 추가
- [x] `monitoring_app.py` - camera 하위폴더 설정 로드 + 경로 계산 로직 + watchdog recursive 제어
- [x] `file_matcher.py` - camera 하위폴더 스캔 로직 + 이벤트 핸들러 깊이 필터링
- [x] `file_count_worker.py` - camera 하위폴더 카운트 로직 적용

### 예정 파일
- [ ] 없음 (Phase 5는 테스트 및 문서화)

---

**마지막 업데이트**: 2025-11-20
**진행률**: Phase 1, 2, 3, 4 완료 (10/13 태스크)
