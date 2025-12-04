# operation_planner.py

## 📋 개요

**파일 경로:** `script/services/operation_planner.py`  
**파일 크기:** 118 라인  
**생성:** Phase 2 (2025-11-29)  
**업데이트:** 2025-12-04  
**목적:** 파일 작업 데이터 구성 및 계획 전담 모듈

## 🎯 책임

- 탭/모드에 따른 작업 대상 그룹 결정
- 파일 작업 워커에 전달할 데이터 구성
- 라인별 데이터 분리 및 매핑

## 📦 주요 클래스

### `OperationPlanner`

파일 작업의 실행 계획을 수립하는 Planner 클래스.

**초기화:**
```python
OperationPlanner(config_manager)
```

**매개변수:**
- `config_manager`: ConfigManager 인스턴스

---

## 주요 메서드

### `build_file_operation_data(tab_index, is_separated, subject, subject2, groups_line1, groups_line2) -> dict`
- 파일 작업 워커에 전달할 데이터 구성
- 탭 인덱스와 라인 모드에 따라 적절한 그룹 선택

**매개변수:**
- `tab_index`: 0=라인1, 1=라인2, 2=통합
- `is_separated`: 분리 모드 여부
- `subject`, `subject2`: 시료명
- `groups_line1`, `groups_line2`: 이동할 그룹 리스트

**반환 구조:**
```python
{
    "YYMMDD": {
        "SubjectName": {
            "groups": [그룹 리스트]
        }
    }
}
```

---

### `create_operation_plan(groups_line1, groups_line2, subject, subject2, date) -> dict`

이동할 파일들의 계획을 생성합니다.

**매개변수:**
- `groups_line1`: 라인1 그룹 리스트
- `groups_line2`: 라인2 그룹 리스트
- `subject`: 시료명
- `subject2`: 시료명2
- `date`: 날짜 문자열

**반환 구조:**
```python
{
    "created_at": "2025-12-04T10:30:00.000000",
    "date": "251204",
    "line1": {
        "subject": "시료명",
        "groups": [...]
    },
    "line2": {
        "subject": "시료명2",
        "groups": [...]
    }
}
```

---

### `_build_group_data(groups) -> list`

그룹 리스트를 저장용 데이터로 변환합니다.

**매개변수:**
- `groups`: 그룹 리스트

**반환값:**
- 변환된 그룹 데이터 리스트 (name, timestamp, files 포함)

---

### `save_plan_to_file(plan, filename, subject_folder) -> bool`

계획을 JSON 파일로 저장합니다.

**매개변수:**
- `plan`: 저장할 계획 딕셔너리
- `filename`: 파일명 (예: "move_plan.json")
- `subject_folder`: 시료 폴더명

**반환값:**
- `True`: 저장 성공
- `False`: 저장 실패

## 🔄 Phase 2 이전과의 차이

### 이전 (monitoring_app.py 내부)
```python
# 복잡한 if-else 체인
if current_tab_index == 0:
    line1_data = {"subject": subject, "groups": groups_to_move_line1}
    line2_data = {"subject": "", "groups": []}
elif current_tab_index == 1:
    # ...
# 수십 줄의 조건문
```

### 이후 (OperationPlanner 사용)
```python
# 간결한 위임
processed_data = self.operation_planner.build_file_operation_data(
    current_tab_index, is_separated, subject, subject2,
    groups_to_move_line1, groups_to_move_line2
)
```

## 🔗 의존성

**Import:** 없음 (순수 Python)

**사용처:**
- `monitoring_app.py` (execute_file_operation 메서드)

## 📊 통계

- **라인 수:** 118줄
- **총 메서드:** 5개 (초기화 + 공개 메서드 3개 + 내부 메서드 1개)
- **주요 메서드:** 4개
  - `build_file_operation_data` - 파일 작업 데이터 구성
  - `create_operation_plan` - 작업 계획 생성
  - `save_plan_to_file` - 계획 저장
  - `_build_group_data` - 그룹 데이터 변환 (내부)
- **처리 경우의 수:** 6가지 (3탭 × 2모드)

## 💡 설계 특징

1. **데이터 중심:** 비즈니스 로직 없이 데이터 구성에만 집중
2. **명확한 구조:** 항상 동일한 구조의 딕셔너리 반환
3. **조건 분리:** 복잡한 if-else를 명확한 케이스별로 분리

---

**작성일:** 2025-12-01  
**관련 Phase:** Phase 2 (파일 작업 로직 분리)
