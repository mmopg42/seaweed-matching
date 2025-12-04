# operation_validator.py

## 📋 개요

**파일 경로:** `script/services/operation_validator.py`  
**파일 크기:** 106 라인  
**생성:** Phase 2 (2025-11-29)  
**업데이트:** 2025-12-04  
**목적:** 파일 작업(이동/복사) 전 입력 검증 전담 모듈

## 🎯 책임

- 작업 실행 전 기본 조건 검증
- 경로 유효성 확인
- 중복 작업 방지
- 검증 오류 메시지 생성

## 📦 주요 클래스

### `OperationValidator`

파일 작업의 사전 조건을 검증하는 Validator 클래스.

**초기화:**
```python
OperationValidator(settings: dict, groups_ref)
```

**매개변수:**
- `settings`: ConfigManager의 settings 딕셔너리
- `groups_ref`: groups 리스트에 대한 참조 (callable 또는 직접 참조)

---

## 주요 메서드

### `validate_basic_inputs(is_file_operation_running=False) -> tuple[bool, list]`
- 파일 작업 실행을 위한 기본 조건 검증
- 반환: `(is_valid: bool, errors: list[(title, message)])`

**검증 항목:**
1. 작업 중복 확인 (is_file_operation_running)
2. 출력 경로 존재 확인
3. 그룹 데이터 존재 확인

**반환 예시:**
```python
# 성공
(True, [])

# 실패
(False, [
    ("진행 중", "이미 이동 작업이 진행 중입니다.\n작업이 완료될 때까지 기다려 주세요."),
    ("경로 오류", "이동 대상 경로가 설정되지 않았습니다.")
])
```

---

### `filter_valid_groups(groups: list) -> dict`

이동 가능한 유효 그룹만 필터링합니다. 완전 매칭 여부를 확인합니다.

**매개변수:**
- `groups`: 검증할 그룹 리스트

**반환 구조:**
```python
{
    "valid": [완전한 그룹들],
    "skipped": [(불완전한 그룹, 누락 파일 리스트), ...]
}
```

**예시:**
```python
result = validator.filter_valid_groups(all_groups)
# {
#     "valid": [group1, group2],
#     "skipped": [(group3, ["cam1", "cam2"]), ...]
# }
```

---

### `_is_group_fully_matched(group: dict) -> tuple[bool, list]`

그룹이 완전한지(모든 필수 파일이 있는지) 검사합니다.

**매개변수:**
- `group`: 검사할 그룹 딕셔너리

**반환값:**
- `(is_complete: bool, missing: list)`
  - `is_complete`: 완전한 그룹 여부
  - `missing`: 누락된 파일 키 리스트

**검증 항목:**
1. 일반 카메라 ("카메라") 확인
2. 라인별 cam 확인:
   - 라인1: cam1, cam2, cam3
   - 라인2: cam4, cam5, cam6

---

### `_has_valid_file_entry(entry) -> bool`

파일 엔트리가 유효한지 확인합니다.

**매개변수:**
- `entry`: 검사할 엔트리 (dict 타입 예상)

**반환값:**
- `True`: 유효한 파일 엔트리 (dict 타입이고 absolute_path 존재)
- `False`: 유효하지 않음

**검증 로직:**
```python
isinstance(entry, dict) and 
any(isinstance(v, dict) and "absolute_path" in v for v in entry.values())
```

## 🔄 Phase 2 이전과의 차이

### 이전 (monitoring_app.py 내부)
```python
# execute_file_operation 내부에 검증 로직 산재
def execute_file_operation(self):
    if self.is_file_operation_running:
        QMessageBox.warning(...)
        return
    output_dir = self.settings.get("output")
    if not output_dir:
        QMessageBox.warning(...)
        return
    # ...
```

### 이후 (OperationValidator 사용)
```python
# 깔끔한 검증 위임
is_valid, errors = self.operation_validator.validate_basic_inputs(...)
if not is_valid:
    for title, msg in errors:
        # 오류 처리
    return
```

## 🔗 의존성

**Import:** 없음 (순수 Python)

**사용처:**
- `monitoring_app.py` (execute_file_operation 메서드)

## 📊 통계

- **라인 수:** 106줄
- **총 메서드:** 5개 (초기화 + 공개 메서드 2개 + 내부 메서드 2개)
- **주요 메서드:** 4개
  - `validate_basic_inputs` - 기본 입력 검증
  - `filter_valid_groups` - 유효 그룹 필터링
  - `_is_group_fully_matched` - 그룹 완전성 검사 (내부)
  - `_has_valid_file_entry` - 파일 엔트리 유효성 검사 (내부)
- **검증 항목:** 3가지 (작업 중복, 경로 유효성, 그룹 존재)

## 💡 설계 특징

1. **단일 책임:** 검증 로직만 담당
2. **일관된 반환:** 모든 오류를 리스트로 반환
3. **UI 독립:** UI 코드 없이 순수 검증 로직만

---

**작성일:** 2025-12-01  
**관련 Phase:** Phase 2 (파일 작업 로직 분리)
