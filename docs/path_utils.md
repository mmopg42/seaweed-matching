# path_utils.py 문서

## 개요
경로 유틸리티 함수를 제공하는 모듈입니다. 일반카메라 썸네일 경로 추출, 경로에서 날짜 추출, 날짜 기반 경로 자동 업데이트, 날짜 검증, 폴더 생성 기능을 제공합니다.

**파일 크기**: 8.2KB (271 라인)  
**총 함수**: 6개  
**데이터 클래스**: 2개  
**상수**: 2개 (PATH_KEYS, KEY_LABELS)

---

## 상수 정의

### `PATH_KEYS`
경로 관련 설정 키 리스트:
```python
["normal", "normal2", "nir", "nir2", "cam1", "cam2", 
 "cam3", "cam4", "cam5", "cam6", "output", "delete"]
```

### `KEY_LABELS`
각 경로 키의 UI 표시용 라벨 딕셔너리:
```python
{
    "normal": "일반 폴더",
    "normal2": "일반2 폴더",
    "nir": "NIR 폴더",
    "nir2": "NIR2 폴더",
    "cam1": "Cam1 폴더",
    ...
}
```

---

## 데이터 클래스

### `PathChange`
경로 변경 정보를 담는 데이터 클래스

#### 필드
- `key` (str): 설정 키 (예: "normal", "nir")
- `label` (str): UI 표시용 라벨 (예: "일반 폴더", "NIR 폴더")
- `old_path` (str): 이전 경로
- `new_path` (str): 새로운 경로

#### 예시
```python
change = PathChange(
    key="normal",
    label="일반 폴더",
    old_path="/data/20250101/normal",
    new_path="/data/20250115/normal"
)
```

### `FolderCreationResult`
폴더 생성 결과를 담는 데이터 클래스

#### 필드
- `label` (str): 폴더 라벨
- `path` (str): 폴더 경로
- `error` (Optional[str]): 에러 메시지 (성공 시 None)

#### 예시
```python
# 성공
result = FolderCreationResult(
    label="일반 폴더",
    path="/data/20250115/normal"
)

# 실패
result = FolderCreationResult(
    label="NIR 폴더",
    path="/data/20250115/nir",
    error="접근 권한 없음: Permission denied"
)
```

---

## 함수 목록

### `validate_date_format(date_str: str) -> tuple[bool, str]`

날짜 형식 검증 (YYYYMMDD)

#### 매개변수
- `date_str`: 검증할 날짜 문자열

#### 반환값
- `(is_valid, error_message)` 튜플
  - `is_valid`: 유효하면 True, 아니면 False
  - `error_message`: 에러 메시지 (유효하면 빈 문자열)

#### 검증 규칙
1. 빈 문자열 체크
2. 길이 8자리 확인
3. 숫자만 포함 확인

#### 예시
```python
# 유효한 날짜
is_valid, msg = validate_date_format("20250115")
# (True, "")

# 길이 오류
is_valid, msg = validate_date_format("2025011")
# (False, "날짜는 8자리여야 합니다. (입력값: '2025011', 길이: 7)")

# 문자 포함
is_valid, msg = validate_date_format("2025-01-15")
# (False, "날짜는 숫자만 포함해야 합니다. (입력값: '2025-01-15')")
```

---

### `plan_date_based_path_changes(settings: dict, new_date: str) -> list[PathChange]`

설정 경로들에서 날짜 패턴(YYYYMMDD)을 찾아 변경 계획을 생성합니다.

#### 매개변수
- `settings`: 설정 딕셔너리
- `new_date`: 새로운 날짜 (YYYYMMDD 형식)

#### 반환값
- `list[PathChange]`: 변경할 경로 리스트 (변경할 경로가 없으면 빈 리스트)

#### 동작
1. 정규식 패턴 생성: `r'\d{8}'` (8자리 연속 숫자)
2. 모든 PATH_KEYS를 순회하며 경로 확인
3. 각 경로에서 8자리 날짜 패턴 검색
4. 날짜 패턴을 new_date로 교체한 새 경로 생성
5. 변경이 있는 경우 PathChange 객체 생성

#### 예시
```python
settings = {
    "normal": "/data/20250101/normal",
    "nir": "/data/20250101/nir",
    "cam1": "/data/camera1",  # 날짜 없음, 건너뜀
}

changes = plan_date_based_path_changes(settings, "20250115")
# [
#     PathChange(key="normal", label="일반 폴더", 
#                old_path="/data/20250101/normal", 
#                new_path="/data/20250115/normal"),
#     PathChange(key="nir", label="NIR 폴더",
#                old_path="/data/20250101/nir",
#                new_path="/data/20250115/nir")
# ]
```

---

### `create_folders_if_needed(path_changes: list[PathChange]) -> tuple[list[FolderCreationResult], list[FolderCreationResult]]`

경로 변경 리스트에서 존재하지 않는 폴더들을 생성합니다.

#### 매개변수
- `path_changes`: PathChange 객체 리스트

#### 반환값
- `(created_list, failed_list)` 튜플
  - `created_list`: 성공적으로 생성된 폴더 정보
  - `failed_list`: 생성 실패한 폴더 정보 (에러 메시지 포함)

#### 동작
1. 각 PathChange의 new_path 확인
2. 이미 존재하는 폴더는 건너뜀
3. 존재하지 않는 폴더는 `os.makedirs()` 시도
4. 성공/실패 결과를 FolderCreationResult로 수집

#### 예외 처리
- `PermissionError`: "접근 권한 없음" 메시지
- `OSError`: "OS 오류" 메시지
- `Exception`: "예상치 못한 오류" 메시지

#### 예시
```python
changes = [
    PathChange("normal", "일반 폴더", 
               "/data/old/normal", "/data/new/normal"),
    PathChange("nir", "NIR 폴더",
               "/data/old/nir", "/readonly/nir")  # 권한 없음
]

created, failed = create_folders_if_needed(changes)

# created:
# [FolderCreationResult(label="일반 폴더", path="/data/new/normal")]

# failed:
# [FolderCreationResult(label="NIR 폴더", path="/readonly/nir",
#                       error="접근 권한 없음: Permission denied")]
```

---

## 함수 목록 (기존)

### `get_normal_thumbnail_path(folder_key: str, data_folder_name: str, settings: dict) -> str`

일반카메라 데이터 폴더의 `stitched_original.png` 경로 반환

#### 매개변수
- `folder_key`: "normal" 또는 "normal2"
- `data_folder_name`: 데이터 폴더명 (예: "C_20250101_120000")  
- `settings`: 설정 딕셔너리

#### 반환값
- `str`: stitched_original.png 절대 경로
- `None`: 경로를 찾을 수 없는 경우

#### 동작
1. 기본 경로 가져오기 (`settings[folder_key]`)
2. camera 하위폴더 옵션 체크 (`use_camera_subfolder`)
3. 실제 검색 경로 계산
   - camera 하위폴더 사용: `base_path/camera`
   - 미사용: `base_path`
4. 데이터 폴더 경로 구성: `search_path/data_folder_name`
5. 썸네일 파일 존재 확인: `data_folder_path/stitched_original.png`

#### 예시
```python
settings = {
    "normal": "/data/normal",
    "use_camera_subfolder_normal": True
}

# /data/normal/camera/C_20250101_120000/stitched_original.png
path = get_normal_thumbnail_path("normal", "C_20250101_120000", settings)
```

---

### `extract_date_from_paths(settings: dict) -> str`

설정된 경로들에서 8자리 날짜 패턴(YYYYMMDD)을 추출합니다.

#### 매개변수
- `settings`: 설정 딕셔너리

#### 반환값
- `str`: 추출된 날짜 문자열 (YYYYMMDD)
- `None`: 날짜 패턴을 찾을 수 없는 경우

#### 동작
1. 정규식 패턴 생성: `r'\d{8}'` (8자리 연속 숫자)
2. 모든 경로 키 확인:
   - `["normal", "normal2", "nir", "nir2", "cam1~6", "output", "delete"]`
3. 각 경로에서 8자리 날짜 패턴 검색
4. 발견된 날짜들의 출현 빈도 계산
5. 가장 많이 나타나는 날짜 반환

#### 예시
```python
settings = {
    "normal": "/data/20250101/normal",
    "nir": "/data/20250101/nir",
    "cam1": "/data/20250102/cam1",  # 소수
    "output": "/output/20250101"
}

# "20250101" (3번 출현)
date = extract_date_from_paths(settings)
```

---

### `auto_update_paths_with_date(settings: dict, new_date: str) -> dict`

설정 경로들의 날짜 부분을 새로운 날짜로 자동 교체합니다.

#### 매개변수
- `settings`: 설정 딕셔너리
- `new_date`: 새로운 날짜 (YYYYMMDD 형식)

#### 반환값
- `dict`: 업데이트된 설정 딕셔너리

#### 동작
1. 날짜 형식 검증 (8자리 숫자)
2. 기존 날짜 패턴 추출 (`extract_date_from_paths()`)
3. 모든 경로 키에서 기존 날짜를 새 날짜로 교체
4. 업데이트된 설정 반환

#### 예시
```python
settings = {
    "normal": "/data/20250101/normal",
    "nir": "/data/20250101/nir",
    "output": "/output/20250101"
}

updated = auto_update_paths_with_date(settings, "20250115")
# {
#     "normal": "/data/20250115/normal",
#     "nir": "/data/20250115/nir",
#     "output": "/output/20250115"
# }
```

---

## camera 하위폴더 옵션

### 설정 키
각 폴더 타입마다 별도의 camera 하위폴더 옵션이 있습니다:
- `use_camera_subfolder_normal`: 일반 폴더 옵션
- `use_camera_subfolder_normal2`: 일반2 폴더 옵션

### 동작 방식

#### 옵션 활성화 (True)
```
base_path: /data/normal
실제 경로: /data/normal/camera
검색 대상:
  /data/normal/camera/C_20250101_120000/
  /data/normal/camera/C_20250101_120001/
```

#### 옵션 비활성화 (False)
```
base_path: /data/normal  
실제 경로: /data/normal
검색 대상:
  /data/normal/C_20250101_120000/
  /data/normal/C_20250101_120001/
```

---

## 날짜 패턴 인식

### 정규식 패턴
```python
r'\d{8}'  # 8자리 연속 숫자
```

### 유효한 패턴
- ✅ `20250101` (YYYYMMDD)
- ✅ `/data/20250101/normal`
- ✅ `/normal_20250101_test`

### 무시되는 패턴
- ❌ `2025-01-01` (하이픈 포함)
- ❌ `2025/01/01` (슬래시 포함)
- ❌ `20250101abc` (문자 포함, 숫자만 추출됨)

---

## MainWindow와의 통합

### 썸네일 경로 조회
```python
def _update_row_widget(self, row_widget, group):
    folder_key = "normal" if group.get("line") == 1 else "normal2"
    data_folder_name = group.get("norm")
    
    thumbnail_path = get_normal_thumbnail_path(
        folder_key,
        data_folder_name,
        self.settings
    )
    
    if thumbnail_path:
        pixmap = self.get_cached_pixmap(thumbnail_path)
        row_widget.set_thumbnail(pixmap)
```

### 경로 자동 업데이트
```python
def path_auto_setting_edit_config(self):
    new_date = self.today_edit.text().strip()
    
    # 경로 자동 업데이트
    updated_settings = auto_update_paths_with_date(
        self.settings,
        new_date
    )
    
    # 설정 저장
    self.config_manager.save(updated_settings)
```

---

## 의존성
- `os`: 파일 시스템 작업
- `re`: 정규식 패턴 매칭

---

## 주의사항

1. **경로 존재 확인**: 함수는 경로가 실제로 존재하는지 확인합니다
2. **None 반환**: 경로를 찾을 수 없거나 파일이 없으면 None 반환
3. **대소문자 구분**: 파일명은 대소문자를 구분합니다 (`stitched_original.png`)
4. **날짜 형식**: 반드시 YYYYMMDD 형식이어야 합니다 (8자리 숫자)

---

## 향후 개선 사항

1. **유연한 썸네일**: 다른 이름의 썸네일 파일도 지원
2. **날짜 검증**: YYYYMMDD 형식 유효성 검증 강화
3. **다중 썸네일**: 여러 이미지 중 최선 선택
