# nir_pruning_service.py

## 📋 개요

**파일 경로:** `script/services/nir_pruning_service.py`  
**파일 크기:** 102 라인  
**생성:** Phase 2 (2025-11-29)  
**업데이트:** 2025-12-04  
**목적:** NIR 파일 정리(pruning) 전담 서비스

## 🎯 책임

- 이동할 그룹에서 유지할 NIR 파일 선택
- 제거 대상 NIR 파일 결정
- 파일 시스템에서 NIR 파일 삭제 실행

## 📦 주요 클래스

### `NirPruningService`

NIR 파일 정리를 담당하는 서비스 클래스.

**초기화:**
```python
NirPruningService(log_callback: Callable = None)
```

**주요 메서드:**

#### `prune_nir_files(groups, keep_n, subject_name="")`
- NIR 파일을 keep_n개만 남기고 나머지 삭제
- 시간순 정렬 후 최신 파일부터 유지

**매개변수:**
- `groups`: 그룹 리스트
- `keep_n`: 유지할 NIR 파일 개수 (0이면 정리하지 않음)
- `subject_name`: 로그용 시료명

**동작:**
1. 그룹에서 모든 NIR 파일 경로 추출
2. 파일 수정 시간(mtime) 기준 정렬
3. 최신 `keep_n`개 선택
4. 나머지 파일 삭제
5. 로그 출력

**예시:**
```python
# 5개만 유지
service.prune_nir_files(groups, keep_n=5, subject_name="Sample1")
# → 5개 초과 NIR 파일이 삭제됨
```

## 🔄 Phase 2 이전과의 차이

### 이전 (monitoring_app.py 내부)
```python
# execute_file_operation 내부에 NIR 정리 로직
if keep_n > 0:
    all_nir_paths = []
    for g in groups_to_move:
        # NIR 파일 수집 로직
    # 정렬 및 삭제 로직
```

### 이후 (NirPruningService 사용)
```python
# 간결한 호출
if keep_n > 0:
    self.nir_pruning_service.prune_nir_files(
        groups_to_move, keep_n, subject
    )
```

## 🔗 의존성

**Import:**
- `os`: 파일 삭제 및 경로 확인

**사용처:**
- `monitoring_app.py` (execute_file_operation 메서드)

## 📊 통계

- **라인 수:** 138줄
- **주요 메서드:** 1개 (prune_nir_files)
- **파일 작업:** 삭제 (os.remove)

## 💡 설계 특징

1. **안전 삭제:** 파일 존재 확인 후 삭제
2. **시간 기반 정렬:** 최신 파일 우선 보존
3. **로그 통합:** log_callback으로 일관된 로깅

## ⚠️ 주의사항

- **불가역 작업:** 삭제된 NIR 파일은 복구 불가
- **keep_n=0:** 정리하지 않음 (전체 유지)
- **파일 잠금:** 삭제 실패 시 예외 발생

---

**작성일:** 2025-12-01  
**관련 Phase:** Phase 2 (파일 작업 로직 분리)
