# 시스템 설계 문서 (Design Document)

**프로젝트**: 파일 모니터링 시스템 개선
**버전**: 1.0
**작성일**: 2025-11-20

---

## 1. 개요

본 문서는 파일 모니터링 시스템의 UI 개선 및 성능 최적화를 위한 시스템 설계를 정의합니다.

---

## 2. 아키텍처 개요

```
┌─────────────────────────────────────────────────────────┐
│                     UI Layer                             │
│  ┌────────────────┐         ┌─────────────────────┐    │
│  │  NIRMonitorApp │         │  MonitoringApp      │    │
│  │  (nir_app.py)  │         │  (monitoring_app.py)│    │
│  └────────┬───────┘         └──────────┬──────────┘    │
│           │                             │                │
│           │                             │                │
└───────────┼─────────────────────────────┼────────────────┘
            │                             │
            ▼                             ▼
┌─────────────────────────────────────────────────────────┐
│                  Business Logic Layer                    │
│  ┌──────────────────┐      ┌─────────────────────────┐ │
│  │  ConfigManager   │      │  FileMatcher            │ │
│  │  (설정 관리)      │      │  (파일 매칭 로직)        │ │
│  └──────────────────┘      └─────────────────────────┘ │
│                                                          │
│  ┌──────────────────┐      ┌─────────────────────────┐ │
│  │  UIComponents    │      │  FileCountWorker        │ │
│  │  (UI 컴포넌트)    │      │  (파일 카운트 워커)     │ │
│  └──────────────────┘      └─────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
            │                             │
            ▼                             ▼
┌─────────────────────────────────────────────────────────┐
│                  System Layer                            │
│  ┌──────────────────┐      ┌─────────────────────────┐ │
│  │  File System     │      │  Watchdog Observer      │ │
│  │  (OS API)        │      │  (파일 감시)            │ │
│  └──────────────────┘      └─────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### 2.1 주요 변경 영역

- **nir_app.py**: NIR 경로 입력란에 "폴더 열기" 버튼 추가 (R1)
- **ui_components.py**: 카메라 폴더 열기 기능 수정 (R2)
- **monitoring_app.py**: camera 하위폴더 옵션 로직 추가 (R3)
- **file_matcher.py**: watchdog 감시 범위 제어 로직 추가 (R4)
- **config_manager.py**: 새 설정 키 저장/로드 지원

---

## 3. 주요 컴포넌트 설계

### D1: NIR 모니터링 폴더 열기 버튼 (R1)

**컴포넌트**: `nir_app.py::NIRMonitorApp`

**변경 사항**:
```python
class NIRMonitorApp(QMainWindow):
    def create_settings_panel(self):
        # 기존 코드에 추가

        # 모니터링 폴더 섹션
        monitor_layout = QHBoxLayout()
        # ... 기존 코드 ...
        monitor_browse_btn = QPushButton("찾아보기")
        monitor_layout.addWidget(monitor_browse_btn)

        # ✅ 새로 추가
        monitor_open_btn = QPushButton("폴더열기")
        monitor_open_btn.clicked.connect(
            lambda: self.open_folder(self.monitor_path_edit)
        )
        monitor_layout.addWidget(monitor_open_btn)

        # 이동 폴더 섹션
        move_layout = QHBoxLayout()
        # ... 기존 코드 ...
        move_browse_btn = QPushButton("찾아보기")
        move_layout.addWidget(move_browse_btn)

        # ✅ 새로 추가
        move_open_btn = QPushButton("폴더열기")
        move_open_btn.clicked.connect(
            lambda: self.open_folder(self.move_path_edit)
        )
        move_layout.addWidget(move_open_btn)

    def open_folder(self, edit_widget: QLineEdit):
        """경로 입력란의 폴더를 탐색기에서 열기"""
        path = edit_widget.text().strip()
        if not path:
            self.log("❌ 폴더 경로가 비어있습니다.")
            return

        if not os.path.isdir(path):
            self.log(f"❌ 폴더가 존재하지 않습니다: {path}")
            return

        try:
            # 플랫폼별 폴더 열기
            import platform
            system = platform.system()

            if system == "Windows":
                os.startfile(path)
            elif system == "Darwin":  # macOS
                subprocess.run(["open", path])
            else:  # Linux
                subprocess.run(["xdg-open", path])

            self.log(f"📁 폴더 열기: {path}")
        except Exception as e:
            self.log(f"❌ 폴더 열기 실패: {e}")
```

**UI 레이아웃**:
```
[NIR 파일 감시 폴더:] [경로 입력란.......] [찾아보기] [폴더열기]
[김 검출 파일 이동 폴더:] [경로 입력란.......] [찾아보기] [폴더열기]
```

---

### D2: 카메라 폴더 열기 기능 수정 (R2)

**컴포넌트**: `ui_components.py::SettingDialog`

**문제 분석**:
현재 cam2, cam3에서 폴더 열기가 작동하지 않는 이유는 `open_folder` 메서드 연결이 누락되었거나 잘못된 경로를 참조하기 때문으로 추정됩니다.

**수정 방안**:
```python
class SettingDialog(QDialog):
    def create_camera_row(self, label_text, key, default_path=""):
        """카메라 경로 입력 행 생성 (공통 메서드)"""
        row_layout = QHBoxLayout()

        label = QLabel(label_text)
        label.setMinimumWidth(100)
        row_layout.addWidget(label)

        edit = QLineEdit()
        edit.setText(default_path)
        edit.setPlaceholderText(f"{label_text} 경로 입력")
        row_layout.addWidget(edit)

        # 찾아보기 버튼
        btn_browse = QPushButton("찾아보기")
        btn_browse.clicked.connect(
            lambda: self.browse_folder(edit, label_text)
        )
        row_layout.addWidget(btn_browse)

        # ✅ 폴더 열기 버튼 (모든 카메라에 일관되게 적용)
        btn_open = QPushButton("폴더열기")
        btn_open.clicked.connect(
            lambda: self.open_folder(edit)
        )
        row_layout.addWidget(btn_open)

        # 저장 (key에 맞게)
        if key == "cam1":
            self.cam1_edit = edit
        elif key == "cam2":
            self.cam2_edit = edit
        elif key == "cam3":
            self.cam3_edit = edit
        elif key == "cam4":
            self.cam4_edit = edit
        elif key == "cam5":
            self.cam5_edit = edit
        elif key == "cam6":
            self.cam6_edit = edit

        return row_layout

    def open_folder(self, edit_widget: QLineEdit):
        """경로 입력란의 폴더를 탐색기에서 열기"""
        path = edit_widget.text().strip()
        if not path:
            QMessageBox.warning(self, "경고", "폴더 경로가 비어있습니다.")
            return

        if not os.path.isdir(path):
            QMessageBox.warning(self, "경고", f"폴더가 존재하지 않습니다:\n{path}")
            return

        try:
            import platform
            import subprocess

            system = platform.system()
            if system == "Windows":
                os.startfile(path)
            elif system == "Darwin":
                subprocess.run(["open", path])
            else:
                subprocess.run(["xdg-open", path])
        except Exception as e:
            QMessageBox.critical(self, "오류", f"폴더 열기 실패:\n{e}")
```

**검증 항목**:
- cam1~cam6 모든 경로의 "폴더열기" 버튼이 정상 동작
- 버튼 클릭 시 올바른 edit 위젯의 경로를 참조
- lambda 함수의 클로저 문제 해결 (각 버튼이 올바른 edit를 참조)

---

### D3: 일반카메라 camera 하위폴더 옵션 (R3)

**컴포넌트**: `ui_components.py::SettingDialog`

**UI 추가**:
```python
class SettingDialog(QDialog):
    def __init__(self, settings, parent=None):
        super().__init__(parent)
        self.settings = settings or {}

        # ✅ 체크박스 추가
        self.use_camera_subfolder_normal = QCheckBox("camera 하위폴더 사용")
        self.use_camera_subfolder_normal2 = QCheckBox("camera 하위폴더 사용")

        # ... 기존 코드 ...

    def create_normal_camera_section(self):
        """일반카메라 섹션 생성"""
        group = QGroupBox("일반카메라 경로")
        layout = QVBoxLayout()

        # 일반카메라 1
        row1 = QHBoxLayout()
        # ... 경로 입력 UI ...
        layout.addLayout(row1)

        # ✅ camera 하위폴더 옵션
        checkbox_row1 = QHBoxLayout()
        checkbox_row1.addSpacing(110)  # 라벨 너비만큼 들여쓰기
        self.use_camera_subfolder_normal.setToolTip(
            "체크 시: {경로}/camera/ 하위에서 검색\n"
            "해제 시: {경로}/ 직하위에서 검색"
        )
        checkbox_row1.addWidget(self.use_camera_subfolder_normal)
        checkbox_row1.addStretch()
        layout.addLayout(checkbox_row1)

        # 일반카메라 2
        row2 = QHBoxLayout()
        # ... 경로 입력 UI ...
        layout.addLayout(row2)

        # ✅ camera 하위폴더 옵션
        checkbox_row2 = QHBoxLayout()
        checkbox_row2.addSpacing(110)
        self.use_camera_subfolder_normal2.setToolTip(
            "체크 시: {경로}/camera/ 하위에서 검색\n"
            "해제 시: {경로}/ 직하위에서 검색"
        )
        checkbox_row2.addWidget(self.use_camera_subfolder_normal2)
        checkbox_row2.addStretch()
        layout.addLayout(checkbox_row2)

        group.setLayout(layout)
        return group

    def load_settings_to_ui(self):
        """설정을 UI에 로드"""
        # ... 기존 코드 ...

        # ✅ camera 하위폴더 옵션 로드
        use_camera_normal = self.settings.get("use_camera_subfolder_normal", False)
        use_camera_normal2 = self.settings.get("use_camera_subfolder_normal2", False)

        self.use_camera_subfolder_normal.setChecked(use_camera_normal)
        self.use_camera_subfolder_normal2.setChecked(use_camera_normal2)

    def get_settings(self):
        """UI에서 설정 가져오기"""
        settings = {
            # ... 기존 설정들 ...

            # ✅ 새 설정 추가
            "use_camera_subfolder_normal": self.use_camera_subfolder_normal.isChecked(),
            "use_camera_subfolder_normal2": self.use_camera_subfolder_normal2.isChecked(),
        }
        return settings
```

---

### D4: 일반카메라 경로 로직 수정 (R3)

**컴포넌트**: `monitoring_app.py::MonitoringApp`, `file_matcher.py::FileMatcher`

**경로 계산 로직**:
```python
class MonitoringApp(QMainWindow):
    def get_effective_normal_path(self, folder_key: str) -> str:
        """
        일반카메라의 실제 검색 경로 반환

        Args:
            folder_key: "normal" 또는 "normal2"

        Returns:
            실제 검색할 경로
        """
        base_path = self.settings.get(folder_key, "").strip()
        if not base_path:
            return ""

        # camera 하위폴더 옵션 확인
        use_subfolder_key = f"use_camera_subfolder_{folder_key}"
        use_camera_subfolder = self.settings.get(use_subfolder_key, False)

        if use_camera_subfolder:
            # {경로}/camera/ 사용
            camera_path = os.path.join(base_path, "camera")
            if os.path.isdir(camera_path):
                return camera_path
            else:
                self.log_to_box(
                    f"⚠️ camera 하위폴더가 존재하지 않습니다: {camera_path}\n"
                    f"   기본 경로를 사용합니다: {base_path}"
                )
                return base_path
        else:
            # 기본 경로 사용
            return base_path
```

**FileMatcher 수정**:
```python
class FileMatcher:
    def scan_folders(self, settings):
        """폴더 스캔 시 camera 하위폴더 옵션 적용"""
        # 일반카메라 경로 가져오기
        normal_path_raw = settings.get("normal", "")
        normal_path = self.get_effective_path(
            normal_path_raw,
            settings.get("use_camera_subfolder_normal", False)
        )

        normal2_path_raw = settings.get("normal2", "")
        normal2_path = self.get_effective_path(
            normal2_path_raw,
            settings.get("use_camera_subfolder_normal2", False)
        )

        # ... 나머지 스캔 로직 ...

    def get_effective_path(self, base_path: str, use_camera_subfolder: bool) -> str:
        """실제 검색 경로 계산"""
        if not base_path:
            return ""

        if use_camera_subfolder:
            camera_path = os.path.join(base_path, "camera")
            return camera_path if os.path.isdir(camera_path) else base_path
        else:
            return base_path
```

---

### D5: Watchdog 딥스캔 제한 (R4)

**컴포넌트**: `monitoring_app.py::start_watchdog`, `file_matcher.py::FolderEventHandler`

**Watchdog 시작 로직 수정**:
```python
class MonitoringApp(QMainWindow):
    def start_watchdog(self):
        """watchdog 시작 (camera 하위폴더 옵션에 따라 recursive 제어)"""
        self.stop_watchdog()

        try:
            self.observer = Observer()

            for folder_type in ["normal", "normal2", "nir", "nir2",
                                "cam1", "cam2", "cam3", "cam4", "cam5", "cam6"]:
                folder = self.settings.get(folder_type, "")

                # ✅ 일반카메라의 경우 실제 경로 계산
                if folder_type in ["normal", "normal2"]:
                    use_subfolder_key = f"use_camera_subfolder_{folder_type}"
                    use_camera_subfolder = self.settings.get(use_subfolder_key, False)

                    if use_camera_subfolder:
                        # camera 하위폴더 사용 시 경로 조정
                        folder = self.get_effective_normal_path(folder_type)

                if folder and os.path.isdir(folder):
                    # ✅ recursive 옵션 결정
                    recursive = self.should_use_recursive_watch(folder_type)

                    self.observer.schedule(
                        self.file_handler,
                        folder,
                        recursive=recursive
                    )

                    mode_str = "재귀 감시" if recursive else "단일 레벨 감시"
                    self.log_to_box(f"[Watchdog] {folder_type}: {folder} ({mode_str})")

            self.observer.start()
            self.log_to_box("✅ Watchdog 감시 시작")

        except Exception as e:
            self.log_to_box(f"[ERROR] Watchdog 시작 실패: {e}")

    def should_use_recursive_watch(self, folder_type: str) -> bool:
        """
        폴더 타입에 따라 재귀 감시 여부 결정

        Args:
            folder_type: "normal", "normal2", "nir", etc.

        Returns:
            True: 재귀 감시, False: 단일 레벨 감시
        """
        # 일반카메라에서 camera 하위폴더 사용 시 재귀 감시 비활성화
        if folder_type in ["normal", "normal2"]:
            use_subfolder_key = f"use_camera_subfolder_{folder_type}"
            use_camera_subfolder = self.settings.get(use_subfolder_key, False)

            if use_camera_subfolder:
                return False  # 단일 레벨만 감시

        # 나머지는 기존대로 재귀 감시
        return True
```

**이벤트 핸들러 수정**:
```python
class FolderEventHandler(FileSystemEventHandler):
    def __init__(self, communicate, settings):
        super().__init__()
        self.communicate = communicate
        self.settings = settings

    def on_created(self, event):
        """파일/폴더 생성 이벤트"""
        if event.is_directory:
            # ✅ camera 하위폴더 모드에서는 2단계 이하 무시
            if self.should_ignore_deep_folder(event.src_path):
                return

        # ... 기존 처리 로직 ...

    def should_ignore_deep_folder(self, path: str) -> bool:
        """
        camera 하위폴더 모드에서 2단계 이하 폴더 무시 여부

        Args:
            path: 이벤트 발생 경로

        Returns:
            True: 무시, False: 처리
        """
        for folder_type in ["normal", "normal2"]:
            use_subfolder_key = f"use_camera_subfolder_{folder_type}"
            use_camera_subfolder = self.settings.get(use_subfolder_key, False)

            if use_camera_subfolder:
                base_path = self.settings.get(folder_type, "")
                camera_path = os.path.join(base_path, "camera")

                # camera_path의 하위인지 확인
                if path.startswith(camera_path):
                    # camera/ 아래 깊이 계산
                    rel_path = os.path.relpath(path, camera_path)
                    depth = len(Path(rel_path).parts)

                    # 2단계 이하면 무시
                    if depth > 1:
                        return True

        return False
```

---

## 4. 데이터 모델/스키마

### 4.1 설정 파일 스키마 확장

**기존 설정 (config.json)**:
```json
{
    "normal": "/path/to/normal",
    "normal2": "/path/to/normal2",
    "nir": "/path/to/nir",
    "cam1": "/path/to/cam1",
    ...
}
```

**새 설정 추가**:
```json
{
    "normal": "/path/to/normal",
    "normal2": "/path/to/normal2",
    "use_camera_subfolder_normal": false,    // ✅ 새 필드
    "use_camera_subfolder_normal2": false,   // ✅ 새 필드
    "nir": "/path/to/nir",
    "nir_monitor_path": "/path/to/nir/monitor",  // NIR 앱용
    "nir_move_path": "/path/to/nir/move",        // NIR 앱용
    "cam1": "/path/to/cam1",
    ...
}
```

**설정 키 정의**:
- `use_camera_subfolder_normal` (bool): 일반카메라1의 camera 하위폴더 사용 여부
- `use_camera_subfolder_normal2` (bool): 일반카메라2의 camera 하위폴더 사용 여부
- `nir_monitor_path` (str): NIR 파일 감시 폴더 (NIR 앱 전용)
- `nir_move_path` (str): 김 검출 파일 이동 폴더 (NIR 앱 전용)

---

## 5. API 인터페이스

### 5.1 ConfigManager 확장

```python
class ConfigManager:
    def get_effective_normal_path(self, settings: dict, folder_key: str) -> str:
        """
        일반카메라의 실제 검색 경로 반환

        Args:
            settings: 설정 딕셔너리
            folder_key: "normal" 또는 "normal2"

        Returns:
            실제 검색할 경로 (camera 하위폴더 옵션 반영)
        """
        pass

    def validate_settings(self, settings: dict) -> tuple[bool, str]:
        """
        설정 유효성 검증

        Args:
            settings: 검증할 설정

        Returns:
            (유효 여부, 에러 메시지)
        """
        pass
```

### 5.2 UI Helper 메서드

```python
class NIRMonitorApp(QMainWindow):
    def open_folder(self, edit_widget: QLineEdit):
        """
        경로 입력란의 폴더를 탐색기에서 열기

        Args:
            edit_widget: 경로가 입력된 QLineEdit 위젯
        """
        pass

class SettingDialog(QDialog):
    def open_folder(self, edit_widget: QLineEdit):
        """
        경로 입력란의 폴더를 탐색기에서 열기

        Args:
            edit_widget: 경로가 입력된 QLineEdit 위젯
        """
        pass
```

---

## 6. 시퀀스 다이어그램

### 6.1 Camera 하위폴더 옵션 설정 시퀀스

```
사용자              SettingDialog         MonitoringApp       FileCountWorker
  │                      │                      │                    │
  │  설정 열기           │                      │                    │
  ├────────────────────>│                      │                    │
  │                      │                      │                    │
  │  camera 체크박스 ON  │                      │                    │
  ├────────────────────>│                      │                    │
  │                      │                      │                    │
  │  확인 버튼 클릭      │                      │                    │
  ├────────────────────>│                      │                    │
  │                      │  get_settings()      │                    │
  │                      ├─────────────────────>│                    │
  │                      │  {use_camera_: true} │                    │
  │                      │<─────────────────────┤                    │
  │                      │                      │                    │
  │                      │                      │  save(settings)    │
  │                      │                      ├──────────────────> │
  │                      │                      │                    │
  │                      │                      │  stop_watchdog()   │
  │                      │                      ├────────────────────┤
  │                      │                      │                    │
  │                      │                      │  start_watchdog()  │
  │                      │                      │  (recursive=False) │
  │                      │                      ├────────────────────┤
  │                      │                      │                    │
  │                      │                      │  update_settings() │
  │                      │                      ├───────────────────>│
  │                      │                      │                    │
  │                      │                      │  로그: camera 모드 │
  │                      │<─────────────────────┤                    │
  │  설정 저장 완료      │                      │                    │
  │<─────────────────────┤                      │                    │
```

### 6.2 폴더 열기 버튼 클릭 시퀀스

```
사용자          NIRMonitorApp/SettingDialog       OS API
  │                      │                          │
  │  폴더열기 버튼 클릭  │                          │
  ├────────────────────>│                          │
  │                      │  경로 검증               │
  │                      ├──────────────────────────┤
  │                      │  (경로 유효성 확인)      │
  │                      │                          │
  │                      │  플랫폼 감지             │
  │                      ├──────────────────────────┤
  │                      │  (Windows/macOS/Linux)   │
  │                      │                          │
  │                      │  os.startfile(path)      │
  │                      │  or subprocess.run()     │
  │                      ├────────────────────────> │
  │                      │                          │
  │                      │  탐색기 열림             │
  │                      │<────────────────────────┤
  │                      │                          │
  │  탐색기 표시         │  로그: 폴더 열기 성공    │
  │<─────────────────────┤                          │
```

---

## 7. 성능 고려사항

### 7.1 Watchdog 성능 최적화

**문제**:
- 기존: 재귀 감시로 인한 불필요한 이벤트 처리
- 예: `/path/camera/seaweed_001/sub1/sub2/file.txt` 변경 시에도 이벤트 발생

**해결**:
- camera 하위폴더 모드에서 `recursive=False` 사용
- 이벤트 핸들러에서 깊이 검증 추가
- 예상 성능 향상: CPU 사용량 20~30% 감소

### 7.2 UI 응답성

**고려사항**:
- 폴더 열기는 비동기 작업이 아니므로 빠르게 완료되어야 함
- 경로 검증은 메인 스레드에서 수행 (간단한 작업)
- 탐색기 열기는 OS에 위임 (블로킹 최소화)

---

## 8. 보안 고려사항

### 8.1 경로 주입 방지

```python
def open_folder(self, edit_widget: QLineEdit):
    path = edit_widget.text().strip()

    # ✅ 경로 정규화 및 검증
    path = os.path.normpath(path)
    path = os.path.abspath(path)

    # ✅ 실제 디렉토리 존재 확인
    if not os.path.isdir(path):
        return

    # ✅ 플랫폼별 안전한 API 사용
    # (os.startfile, subprocess.run은 쉘 인젝션 방지)
```

### 8.2 설정 파일 검증

```python
class ConfigManager:
    def validate_settings(self, settings: dict) -> tuple[bool, str]:
        """설정 값 타입 검증"""
        # 경로는 문자열
        for key in ["normal", "normal2", "nir", ...]:
            if key in settings and not isinstance(settings[key], str):
                return False, f"Invalid type for {key}"

        # 불리언 플래그 검증
        for key in ["use_camera_subfolder_normal", "use_camera_subfolder_normal2"]:
            if key in settings and not isinstance(settings[key], bool):
                return False, f"Invalid type for {key}"

        return True, ""
```

---

## 9. 에러 처리

### 9.1 폴더 열기 실패

```python
def open_folder(self, edit_widget: QLineEdit):
    try:
        # ... 폴더 열기 로직 ...
    except FileNotFoundError:
        self.log("❌ 폴더가 존재하지 않습니다.")
    except PermissionError:
        self.log("❌ 폴더 접근 권한이 없습니다.")
    except Exception as e:
        self.log(f"❌ 폴더 열기 실패: {e}")
```

### 9.2 Watchdog 감시 실패

```python
def start_watchdog(self):
    try:
        # ... watchdog 시작 ...
    except OSError as e:
        self.log_to_box(f"[ERROR] 폴더 접근 실패: {e}")
    except Exception as e:
        self.log_to_box(f"[ERROR] Watchdog 시작 실패: {e}")
        import traceback
        traceback.print_exc()
```

---

## 10. 테스트 전략

### 10.1 단위 테스트

- `get_effective_normal_path()`: camera 하위폴더 옵션에 따른 경로 계산
- `should_use_recursive_watch()`: 재귀 감시 여부 결정
- `open_folder()`: 플랫폼별 폴더 열기

### 10.2 통합 테스트

- 설정 변경 후 watchdog 재시작
- camera 하위폴더 옵션 ON/OFF 전환
- 폴더 열기 버튼 동작 (cam1~cam6, NIR 경로)

### 10.3 회귀 테스트

- 기존 설정 파일 로드 (하위 호환성)
- camera 하위폴더 옵션 없이도 정상 동작
- 기존 watchdog 동작 유지

---

## 11. 배포 고려사항

### 11.1 마이그레이션

```python
class ConfigManager:
    def migrate_settings(self, settings: dict) -> dict:
        """기존 설정을 새 버전으로 마이그레이션"""
        # ✅ 새 필드 기본값 추가
        if "use_camera_subfolder_normal" not in settings:
            settings["use_camera_subfolder_normal"] = False

        if "use_camera_subfolder_normal2" not in settings:
            settings["use_camera_subfolder_normal2"] = False

        return settings
```

### 11.2 릴리스 노트

**v0.3.8 변경사항**:
1. NIR 모니터링에 "폴더 열기" 버튼 추가
2. 카메라 폴더 열기 기능 개선 (cam2, cam3 수정)
3. 일반카메라 "camera 하위폴더" 옵션 추가
4. Watchdog 성능 최적화 (딥스캔 제한)

---

## 12. 설계 요구사항 추적

| 설계 항목 | 관련 요구사항 | 구현 위치 |
|----------|-------------|----------|
| D1: NIR 폴더 열기 버튼 | R1 | nir_app.py |
| D2: 카메라 폴더 열기 수정 | R2 | ui_components.py |
| D3: camera 하위폴더 UI | R3 | ui_components.py |
| D4: camera 하위폴더 로직 | R3 | monitoring_app.py, file_matcher.py |
| D5: Watchdog 딥스캔 제한 | R4 | monitoring_app.py, file_matcher.py |

---

## 13. 변경 이력

| 버전 | 날짜 | 작성자 | 변경 내용 |
|-----|------|-------|----------|
| 1.0 | 2025-11-20 | Claude | 초기 작성 |
