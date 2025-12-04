# ui_builder.py 문서

## 개요
MainWindow UI 생성 전담 클래스입니다. UI 위젯 생성, 레이아웃 구성, 위젯 참조 관리를 담당하며, MainWindow는 이벤트 핸들러만 담당하도록 분리되었습니다.

**파일 경로**: `script/ui/builders/ui_builder.py`  
**파일 크기**: 548 라인  
**총 클래스**: 1개 (`UIBuilder`)  
**총 메서드**: 13개 (public 3개 + private 10개)  
**업데이트**: 2025-12-04

---

## 설계 원칙

### 책임 분리
- **UIBuilder**: UI 생성 및 구성
- **MainWindow**: 이벤트 핸들링

### 위젯 참조 관리
- 모든 위젯을 딕셔너리로 반환
- MainWindow가 setattr()로 속성화
- 하위 호환성 유지

---

## 클래스

### `UIBuilder`

MainWindow UI 생성 전담 클래스

#### 초기화

```python
ui_builder = UIBuilder(parent, settings)
```

##### 매개변수
- `parent`: MainWindow 인스턴스
- `settings`: 설정 딕셔너리

##### 인스턴스 변수
- `parent`: MainWindow 참조
- `settings`: 설정 딕셔너리
- `widgets`: 생성된 위젯 딕셔너리 {name: widget}

---

## 메서드

### `build_ui()` → dict

전체 UI 구성 메인 진입점

#### 반환값
- `dict`: 위젯 참조 딕셔너리 {name: widget}

#### 동작
1. 중앙 위젯 및 메인 레이아웃 생성
2. 상단 툴바 생성 (`_build_toolbar()`)
3. 통계 바 생성 (`_build_stats_bar()`)
4. 탭 위젯 생성 (`_build_tabs()`)
5. 로그 패널 생성 (`_build_log_panel()`)
6. 이벤트 핸들러 연결 (`_connect_signals()`)
7. 위젯 참조 딕셔너리 반환

#### 예시
```python
ui_builder = UIBuilder(self, self.settings)
widgets = ui_builder.build_ui()

# 위젯 참조 복원 (하위 호환성)
for name, widget in widgets.items():
    setattr(self, name, widget)
```

---

### `_build_toolbar()`

상단 툴바 생성

#### 생성 위젯
- **설정 버튼**: `btn_setting`, `btn_open_folder`, `btn_output_folder`
- **날짜 입력**: `today_edit`, `lbl_today`
- **경로 자동 설정**: `btn_path_auto_setting`
- **시료명 입력**: `subject_folder_edit`, `subject_folder_edit2`, `lbl_subject`, `lbl_subject2`
- **시료 폴더 생성**: `btn_create_subject_folder`
- **행 제어**: `btn_toggle_select`, `btn_delete_rows`, `btn_refresh_rows`
- **감시 제어**: `btn_run`, `btn_stop`
- **파일 작업**: `btn_move`, `combo_mode`
- **작업 옵션**: `nir_count_edit`, `data_count_edit`

#### 레이아웃
- `FlowLayout_` 사용 (자동 줄바꿈)
- 각 위젯별 고정 폭 설정
- 컨테이너로 그룹화 (예: count_container)

---

### `_build_stats_bar()`

통계 바 생성 (파일 개수 + 매칭 현황)

#### 동작
1. 파일 개수 현황 바 생성
2. 매칭 현황 바 (통합 모드) 생성
3. 매칭 현황 바 (분리 모드) 생성
4. 컨테이너에 모두 추가

#### 저장 위젯
- `stats_container`: 통계 컨테이너
- `matching_frame_unified`: 통합 모드 프레임
- `matching_frame_separated`: 분리 모드 프레임

---

### `_build_file_count_bar()`

파일 개수 현황 바 생성

#### 생성 칩
- **라인1**: `chip_nir_count`, `chip_normal_count`, `chip_cam1_count`, `chip_cam2_count`, `chip_cam3_count`
- **라인2**: `chip_nir2_count`, `chip_normal2_count`, `chip_cam4_count`, `chip_cam5_count`, `chip_cam6_count`

#### 각 칩 구성
- `chip_xxx_count`: 칩 위젯  
- `lbl_xxx_count`: 카운트 라벨 (setText로 업데이트)

#### 레이아웃
```
📊 파일 개수 현황: | NIR1 | 일반1 | Cam1 | Cam2 | Cam3 | NIR2 | 일반2 | Cam4 | Cam5 | Cam6 |
```

---

### `_build_matching_bar_unified()`

매칭 현황 - 통합 모드

#### 생성 칩
- `chip_total` / `lbl_total`: 총 매칭 개수
- `chip_with` / `lbl_with`: NIR 포함 개수
- `chip_without` / `lbl_without`: NIR 제외 개수
- `chip_fail` / `lbl_fail`: 실패 개수

#### 레이아웃
```
🔗 매칭 현황: | 총 매칭 | with NIR | without NIR | 실패 |
```

---

### `_build_matching_bar_separated()`

매칭 현황 - 분리 모드 (라인1, 라인2)

#### 생성 칩 (라인1)
- `chip_total_line1` / `lbl_total_line1`
- `chip_with_line1` / `lbl_with_line1`
- `chip_without_line1` / `lbl_without_line1`
- `chip_fail_line1` / `lbl_fail_line1`

#### 생성 칩 (라인2)
- `chip_total_line2` / `lbl_total_line2`
- `chip_with_line2` / `lbl_with_line2`
- `chip_without_line2` / `lbl_without_line2`
- `chip_fail_line2` / `lbl_fail_line2`

#### 레이아웃
```
🔗 라인1: | 총 | NIR | NO-NIR | 실패 | 🔗 라인2: | 총 | NIR | NO-NIR | 실패 |
```

---

### `_build_tabs()`

탭 위젯 생성 (라인1, 라인2, 통합)

#### 탭1: 라인1
- `tab_line1`: 탭 위젯
- `scroll_area_line1`: 스크롤 영역
- `scroll_layout_line1`: 레이아웃 (MonitorRow 추가용)

#### 탭2: 라인2
- `tab_line2`: 탭 위젯
- `scroll_area_line2`: 스크롤 영역
- `scroll_layout_line2`: 레이아웃

#### 탭3: 통합 (좌우 분할)
- `tab_combined`: 탭 위젯
- **왼쪽 (라인1)**:
  - `scroll_area_combined_line1`: 스크롤 영역
  - `scroll_layout_combined_line1`: 레이아웃
- **오른쪽 (라인2)**:
  - `scroll_area_combined_line2`: 스크롤 영역
  - `scroll_layout_combined_line2`: 레이아웃

#### 기본 참조 (하위 호환성)
- `scroll_area`: `scroll_area_combined_line1`
- `scroll_layout`: `scroll_layout_combined_line1`

#### DragSelectWidget 통합
- 각 스크롤 영역의 scroll_content는 `DragSelectWidget`로 감싸짐
- 드래그로 여러 행 선택 가능

---

### `_build_log_panel()`

로그 패널 생성

#### 생성 위젯
- `log_panel`: LogPanel 인스턴스

#### 주의
- LogPanel은 `ui_components`가 아닌 `log_panel` 모듈에서 import

---

### `_connect_signals()`

이벤트 핸들러 연결

#### 버튼 클릭 이벤트
```python
btn_setting.clicked.connect(parent.show_setting_dialog)
btn_open_folder.clicked.connect(parent.config_manager.open_appdir_folder)
btn_output_folder.clicked.connect(parent.open_output_folder_clicked)
btn_path_auto_setting.clicked.connect(parent.path_auto_setting_edit_config)
btn_create_subject_folder.clicked.connect(parent.create_subject_folder)
btn_refresh_rows.clicked.connect(parent.refresh_rows_action)
btn_run.clicked.connect(parent.start_watch)
btn_stop.clicked.connect(parent.stop_watch)
btn_move.clicked.connect(parent.execute_file_operation)
btn_delete_rows.clicked.connect(lambda: delete_selected_rows(parent))
btn_toggle_select.clicked.connect(parent.toggle_select_all)
```

#### 입력 필드 변경 이벤트
```python
today_edit.textChanged.connect(parent.save_today_date)
subject_folder_edit.textChanged.connect(parent.save_subject_folder)
subject_folder_edit2.textChanged.connect(parent.save_subject_folder2)
nir_count_edit.textChanged.connect(parent.save_nir_count)
data_count_edit.textChanged.connect(parent.save_data_count)
```

#### 주의사항
- `update_line_mode_ui()` 및 `update_tooltips()`는 여기서 호출하지 않음
- MainWindow의 `init_ui()`에서 위젯 설정 후 호출

---

### `_create_chip(label_text: str)` → tuple

통계 칩 생성

#### 매개변수
- `label_text`: 칩 라벨 텍스트 (예: "NIR1", "총 매칭")

#### 반환값
- `(chip_widget, value_label)` 튜플
  - `chip_widget`: QWidget 컨테이너
  - `value_label`: QLabel (값 표시용)

#### 동작
1. QWidget 생성
2. QHBoxLayout 설정 (패딩: 10,6,10,6)
3. 라벨 생성 (property="muted")
4. 값 라벨 생성 (font-weight:700, 초기값 "0")
5. 레이아웃에 추가
6. 튜플 반환

#### 예시
```python
chip, lbl = self._create_chip("NIR1")
# lbl.setText("42")로 값 업데이트 가능
```

---

### `_store_widget(name: str, widget)`

위젯 참조 저장

#### 매개변수
- `name`: 위젯 이름 (속성명)
- `widget`: 위젯 객체

#### 동작
```python
self.widgets[name] = widget
```

---

## MainWindow와의 통합

### `init_ui()` 메서드

```python
def init_ui(self):
    """UI 초기화 (UIBuilder에 위임)"""
    from ui_builder import UIBuilder
    
    ui_builder = UIBuilder(self, self.settings)
    widgets = ui_builder.build_ui()
    
    # 위젯 참조 복원 (하위 호환성)
    for name, widget in widgets.items():
        setattr(self, name, widget)
    
    # 기본 초기화
    self.reset_monitor_rows()
    
    # UI 업데이트 (위젯이 설정된 후에 호출)
    self.update_line_mode_ui()
    self.update_tooltips()
```

### 위젯 접근

```python
# 기존 코드와 동일하게 사용 가능
self.btn_run.setEnabled(False)
self.lbl_nir_count.setText("42")
self.today_edit.text()
```

---

## 의존성

- `PySide6.QtWidgets`: Qt 위젯
- `PySide6.QtCore.Qt`: Qt 상수
- `ui_components`: FlowLayout_
- `log_panel`: LogPanel
- `delete_manager`: delete_selected_rows

---

## 주의사항

1. **순환 import 방지**: DragSelectWidget은 동적 import
2. **위젯 순서**: 위젯 설정 후 UI 업데이트 메서드 호출
3. **하위 호환성**: 기존 코드와 100% 호환 (setattr 사용)

---

## 향후 개선 사항

1. **타입 힌팅**: 반환 타입 명시 강화
2. **테마 지원**: 다크 모드 등
3. **설정 저장**: UI 레이아웃 상태 저장/복원
