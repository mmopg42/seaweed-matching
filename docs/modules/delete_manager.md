# delete_manager.py 문서

## 개요
파일/폴더 삭제를 관리하는 모듈입니다. 버킷 규칙에 따라 파일을 삭제 폴더로 이동하고, 안전한 삭제를 위한 여러 가드 로직을 제공합니다.

**파일 경로**: `script/services/delete_manager.py`  
**파일 크기**: 624 라인  
**총 함수**: 9개 (public 7개 + private 2개)  
**업데이트**: 2025-12-04

---

## 🔥 최신 변경사항 (2025-12-03)

### 1. Qt 프레임워크 변경
- **PyQt6 → PySide6**: QMessageBox import 변경

---

## 주요 함수

### 가드 함수

#### ensure_watching_off(main) -> bool
삭제 실행 전 감시 상태 확인 및 OFF 전환
```python
# 감시 중이면 확인 다이얼로그
if watching:
    reply = QMessageBox.question(...)
    if reply == Yes:
        stop_watching()
    else:
        return False  # 사용자 취소
return True  # 계속 진행
```
- **Returns**: True=계속, False=취소

#### ensure_delete_folder(main) -> Path
삭제 폴더 확인 및 존재 보장
```python
delete_path = settings.get("delete")
if not delete_path:
    QMessageBox.warning("삭제 폴더가 설정되지 않았습니다")
    return None
Path(delete_path).mkdir(parents=True, exist_ok=True)
return Path(delete_path)
```
- **Returns**: 삭제 폴더 경로 또는 None (취소)

#### ensure_subject_for_delete(main) -> str
시료명 확정
```python
subject = settings.get("subject", "").strip()
if not subject:
    # 'UnknownFolder' 사용 여부 확인
    reply = QMessageBox.question(...)
    if reply == Yes:
        return "UnknownFolder"
    else:
        return None  # 취소
return subject
```
- **Returns**: 시료명 또는 None (취소)

---

### 버킷 경로 생성

#### _build_delete_bucket_dir(main, *, group_has_nir: bool, role: str, subject: str) -> Path
버킷 규칙에 따른 삭제 경로 계산

**매개변수**:
- `group_has_nir`: 그룹이 NIR을 보유했는지
- `role`: "nir", "norm", "norm2", "cam1~6"
- `subject`: 시료명

**경로 구조**:
```
<delete>/<YYYYMMDD>/<subject>/<with NIR 또는 without NIR>/<세부폴더>
```

**with NIR**:
- `nir` → `Nir`
- `norm` → `일반`
- `norm2` → `일반2`
- `cam1~6` → `복합 카메라/cam1~6`

**without NIR**:
- `norm` → `일반 카메라`
- `norm2` → `일반2 카메라`
- `cam1~6` → `복합 카메라/cam1~6`

---

### 파일 이동

#### move_to_delete_bucket(main, source: Path, *, group_has_nir: bool, role: str, subject: str)
파일/폴더를 버킷 규칙에 따라 삭제 폴더로 이동

**처리 과정**:
1. 버킷 경로 계산
2. 목적지 폴더 생성
3. 충돌 확인 (이름 변경 or 덮어쓰기)
4. `shutil.move()` 실행

---

### 내부 수집 로직

#### _collect_paths_for_row(main, row_idx: int, ignore_checkboxes: bool = False, row_widget = None) -> tuple
지정된 행에서 삭제 대상과 역할 수집

**Returns**:
```python
(
    [
        {"path": Path(...), "role": "nir"},
        {"path": Path(...), "role": "norm"},
        {"path": Path(...), "role": "cam1"},
        ...
    ],
    group_has_nir,  # bool
    group_name      # str
)
```

**체크박스 무시**:
- `ignore_checkboxes=True`: 모든 항목 수집
- `ignore_checkboxes=False`: 체크된 항목만 수집

---

## 퍼블릭 API

### delete_one_row(main, row_idx: int, *, skip_confirm: bool = False, subject: str = None, ignore_checkboxes: bool = False)
한 행의 선택된 항목들을 삭제 폴더로 이동

**처리 순서**:
1. 삭제 폴더 확인 (`ensure_delete_folder()`)
2. 감시 OFF 확인 (`ensure_watching_off()`)
3. 시료명 확인 (`ensure_subject_for_delete()` 또는 매개변수)
4. 사용자 확인 (skip_confirm=False일 때)
5. 항목 수집 (`_collect_paths_for_row()`)
6. 병렬 이동 (ThreadPoolExecutor)
7. 성공/실패 로그

**병렬 처리**:
```python
with ThreadPoolExecutor(max_workers=4) as executor:
    futures = [executor.submit(_move_single_item, item) for item in items]
    for future in as_completed(futures):
        result = future.result()
        # 성공/실패 카운트
```

### delete_selected_rows(main)
선택된 행들을 일괄 삭제

**처리 순서**:
1. 삭제 폴더 확인
2. 감시 OFF 확인
3. 선택된 행 확인
4. 사용자 확인
5. 시료명 확인
6. 모든 항목 수집
7. 병렬 일괄 이동
8. 성공/실패 로그

**선택된 행 확인**:
```python
selected_rows = [i for i, item in enumerate(display_items) if item.get("_selected", False)]
```

### set_select_all(main, state: bool)
모든 행의 선택 체크박스 상태 변경
- `state=True`: 전체 선택
- `state=False`: 전체 해제

---

## 버킷 규칙

### 날짜 자동 추출
```python
# settings에서 날짜 추출 (YYYYMMDD 패턴)
# 예: e:/data/20250129/normal → "20250129"
```

### with NIR 구조
```
<delete>/20250129/<subject>/with NIR/
    ├─ Nir/                         # NIR 파일
    ├─ 일반/                         # 일반 카메라
    ├─ 일반2/                        # 일반2 카메라
    └─ 복합 카메라/
        ├─ cam1/
        ├─ cam2/
        ├─ cam3/
        ├─ cam4/
        ├─ cam5/
        └─ cam6/
```

### without NIR 구조
```
<delete>/20250129/<subject>/without NIR/
    ├─ 일반 카메라/
    ├─ 일반2 카메라/
    └─ 복합 카메라/
        ├─ cam1/
        ├─ cam2/
        ├─ cam3/
        ├─ cam4/
        ├─ cam5/
        └─ cam6/
```

---

## 안전 장치

1. **감시 중지 확인**: 삭제 전 감시 OFF 확인 (충돌 방지)
2. **시료명 확인**: 시료명 미설정 시 UnknownFolder 사용 여부 확인
3. **사용자 확인**: 삭제 전 확인 다이얼로그 (항목 개수 표시)
4. **이름 충돌 처리**: 중복 시 `_1`, `_2` 등 자동 추가
5. **병렬 처리 에러 핸들링**: 개별 오류가 전체 작업 중단하지 않음

---

## 작업 흐름

### 1. 단일 행 삭제
```
사용자 [행의 Delete 버튼 클릭]
  → delete_one_row(row_idx)
  → 가드 함수들 실행
  → 사용자 확인
  → 항목 수집
  → 병렬 이동
  → 로그 출력
```

### 2. 선택 행 일괄 삭제
```
사용자 [선택 후 Delete Selected 버튼]
  → delete_selected_rows()
  → 가드 함수들 실행
  → 선택된 행 확인
  → 사용자 확인
  → 모든 항목 수집
  → 병렬 일괄 이동
  → 로그 출력
```

---

## 의존성
- `PySide6.QtWidgets`: QMessageBox (확인 다이얼로그)
- `pathlib.Path`: 경로 처리
- `shutil`: 파일 이동
- `concurrent.futures.ThreadPoolExecutor`: 병렬 처리
- `datetime`: 타임스탬프 생성
