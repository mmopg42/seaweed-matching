# FileOperationManager 문서

## 개요
`FileOperationManager`는 파일 이동/복사 작업의 **비즈니스 로직**을 담당하는 클래스입니다. UI(`MainWindow`)와 비즈니스 로직을 분리하여 테스트 용이성과 유지보수성을 높이기 위해 도입되었습니다.

**파일 경로**: `script/domain/file_operation_manager.py`  
**파일 크기**: 약 400 라인  
**총 클래스**: 1개 (`FileOperationManager`)  
**총 메서드**: 9개 (public 5개 + private 4개)  
**업데이트**: 2025-12-04

---

## 주요 책임
1. **입력 검증 (`validate`)**: 사용자 입력(시료명, 경로 등)의 유효성 검사
2. **그룹 선택 (`select_groups`)**: 탭/라인 모드에 따라 작업 대상 그룹 필터링
3. **NIR 정리 (`prune_nir`)**: 설정된 개수만큼 NIR 파일을 유지하고 나머지 정리
4. **데이터 구성 (`build_processed_data`)**: `FileOperationWorker`가 처리할 수 있는 데이터 구조 생성

---

## 클래스 구조

### Public 메서드

#### `__init__(self, settings, groups, log_callback)`
초기화 메서드
- **매개변수**:
  - `settings` (dict): 애플리케이션 설정 딕셔너리
  - `groups` (list): 현재 로드된 전체 그룹 리스트
  - `log_callback` (callable): 로그 메시지를 출력할 콜백 함수 (예: `MainWindow.log_to_box`)
- **초기화 내용**:
  - `self.settings`: 설정 저장
  - `self.groups`: 그룹 리스트 저장
  - `self.log`: 로그 콜백 저장

---

#### `validate(self, tab_index, line_mode, subject, subject2)`
입력값의 유효성을 검사합니다.
- **Returns**: `(is_valid, errors)` 튜플
  - `is_valid` (bool): 유효성 여부
  - `errors` (list): 오류 메시지 튜플 리스트 `[(title, message), ...]`

### `select_groups(self, tab_index, line_mode, data_count_limit)`
작업 대상 그룹을 선택하고 필터링합니다.
- **Returns**: 선택 결과 딕셔너리
  - `line1`: 라인1 이동 대상 그룹 리스트
  - `line2`: 라인2 이동 대상 그룹 리스트
  - `line1_skipped`: 라인1 제외된 그룹 리스트
  - `line2_skipped`: 라인2 제외된 그룹 리스트
  - `limit_triggered`: 데이터 개수 제한 적용 여부

### `prune_nir(self, selection, keep_n, subject, subject2)`
NIR 파일을 정리(삭제 폴더로 이동)합니다.
- **Returns**: 결과 딕셔너리
  - `pruned_count`: 정리된 NIR 파일 수
  - `affected_groups`: 영향을 받은 그룹 리스트

#### `build_processed_data(self, tab_index, is_separated, subject, subject2, groups_line1, groups_line2)`
워커 스레드에 전달할 데이터를 구성합니다.
- **매개변수**:
  - `tab_index` (int): 현재 탭 인덱스
  - `is_separated` (bool): 분리 모드 여부
  - `subject` (str): 라인1 시료명
  - `subject2` (str): 라인2 시료명 (분리 모드일 때)
  - `groups_line1` (list): 라인1 그룹 리스트
  - `groups_line2` (list): 라인2 그룹 리스트
- **반환값**: `processed_data` 딕셔너리 `{date: {subject: {groups: [...]}}}`

---

### Private 메서드

#### `_move_to_delete_bucket(self, src_path, subject, is_nir=True)`
파일/폴더를 삭제 버킷으로 이동
- **매개변수**:
  - `src_path` (str): 원본 경로
  - `subject` (str): 시료명
  - `is_nir` (bool): NIR 파일 여부
- **동작**:
  - 삭제 폴더 경로 계산
  - `shutil.move()` 사용하여 이동
  - 로그 메시지 출력

---

#### `_filter_fully_matched_groups(self, groups)`
완전히 매칭된 그룹만 필터링
- **매개변수**:
  - `groups` (list): 필터링할 그룹 리스트
- **반환값**: 완전히 매칭된 그룹 리스트
- **동작**:
  - 각 그룹에 대해 `_is_group_fully_matched()` 호출
  - 완전히 매칭된 그룹만 포함

---

#### `_is_group_fully_matched(self, group)`
그룹이 완전히 매칭되었는지 확인
- **매개변수**:
  - `group` (dict): 확인할 그룹
- **반환값**: True (완전 매칭), False (불완전)
- **판정 기준**:
  - 모든 폴더 타입(카메라, NIR)에 유효한 파일 존재
  - 빈 폴더 또는 None 값은 불완전으로 간주

---

#### `_has_valid_file_entry(self, entry)`
폴더 엔트리에 유효한 파일이 있는지 확인
- **매개변수**:
  - `entry`: 폴더 엔트리 (dict, str, None)
- **반환값**: True (유효한 파일 존재), False (없음)
- **판정 기준**:
  - `None` → False
  - 문자열 (경로) → True
  - 딕셔너리 → `files` 키가 있고 비어있지 않으면 True

---

## 사용 예시 (MainWindow)

```python
# 1. Manager 생성
mgr = FileOperationManager(self.settings, self.groups, self.log_to_box)

# 2. 입력 검증
is_valid, errors = mgr.validate(tab_index, line_mode, subject, subject2)
if not is_valid:
    # 에러 처리
    return

# 3. 그룹 선택
selection = mgr.select_groups(tab_index, line_mode, limit)

# 4. NIR 정리 (옵션)
if nir_keep > 0:
    mgr.prune_nir(selection, nir_keep, subject, subject2)

# 5. 데이터 구성
data = mgr.build_processed_data(...)

# 6. 워커 실행
worker = FileOperationWorker(data, ...)
worker.start()
```
