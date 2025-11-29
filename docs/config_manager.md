# config_manager.py 문서

## 개요
애플리케이션의 설정과 로그를 JSON 파일로 관리하는 클래스입니다. 플랫폼에 맞는 사용자 데이터 디렉토리를 사용하여 이식성을 높입니다.

**파일 크기**: 6KB (173 라인)  
**총 함수**: 17개

---

## ConfigManager 클래스

### 설명
애플리케이션 설정 및 로그 관리 클래스

### 속성
- `app_name`: 애플리케이션 이름
- `app_author`: 제작자
- `app_dir`: 설정 폴더 경로
- `config_path`: config.json 경로

### 메서드

#### 초기화
```python
__init__(self, app_name="MatchingTool_monitoring", app_author="prische")
```
- 플랫폼별 사용자 데이터 디렉토리 설정 (appdirs 사용)
- 폴더 자동 생성

#### 설정 파일 관리
```python
load(self) -> dict
```
config.json 파일에서 설정 로드
- **Returns**: 설정 딕셔너리 (실패 시 빈 딕셔너리)

```python
save(self, config: dict)
```
설정을 config.json에 저장
- UTF-8 인코딩
- 들여쓰기 2칸

#### 폴더 열기
```python
open_appdir_folder(self)
```
설정 폴더를 시스템 파일 탐색기에서 열기
- Windows: `os.startfile()`
- macOS: `open` 명령
- Linux: `xdg-open` 명령

```python
open_folder(self, path)
```
지정된 경로를 탐색기에서 열기

#### 로그 경로 관리
```python
get_daily_log_dir(self, date_str: str) -> str
```
일자별 로그 디렉토리 경로 반환
- 경로: `<app_dir>/<YYYYMMDD>`

```python
get_subject_log_dir(self, date_str: str, subject: str) -> str
```
시료별 로그 디렉토리 경로 반환
- 경로: `<app_dir>/<YYYYMMDD>/<subject>`

```python
get_move_plan_path(self, date_str: str, subject: str) -> str
```
move_plan.json 저장 경로 반환
- 경로: `<app_dir>/<YYYYMMDD>/<subject>/move_plan.json`

```python
get_move_log_path(self, date_str: str) -> str
```
이동 로그 파일 경로 반환
- 경로: `<app_dir>/<YYYYMMDD>/moved_subjects.json`

```python
get_log_file_path(self) -> str
```
오늘 날짜 기준 앱 로그 파일 경로 반환
- 경로: `<app_dir>/<YYMMDD>/app.log`

#### 이동 로그 관리
```python
load_move_log(self, date_str: str) -> dict
```
이동 로그 로드
- **Returns**:
  ```python
  {
      "meta": {"date": "20250129", "app": "..."},
      "subjects": {
          "sample1": [
              {"at": "2025-01-29T10:30:00", "mode": "이동"},
              ...
          ],
          ...
      }
  }
  ```

```python
save_move_log(self, date_str: str, data: dict)
```
이동 로그 저장

```python
was_subject_moved(self, date_str: str, subject: str) -> tuple
```
시료가 이미 이동되었는지 확인
- **Returns**: `(exists: bool, last_iso_timestamp: str)`

```python
record_subject_moved(
    self,
    date_str: str,
    subject: str,
    when_iso: str,
    mode: str = "이동",
    extra = None
)
```
시료 이동 기록 추가

---

## 파일 구조

### 설정 디렉토리
- **Windows**: `C:\Users\<username>\AppData\Local\prische\MatchingTool_monitoring\`
- **macOS**: `~/Library/Application Support/MatchingTool_monitoring/`
- **Linux**: `~/.local/share/MatchingTool_monitoring/`

### 파일 레이아웃
```
<app_dir>/
├─ config.json                    # 애플리케이션 설정
├─ 20250129/                      # 날짜별 폴더
│   ├─ app.log                    # 앱 로그
│   ├─ moved_subjects.json        # 이동 기록
│   ├─ sample1/                   # 시료별 폴더
│   │   └─ move_plan.json         # 이동 계획
│   └─ sample2/
│       └─ move_plan.json
└─ 20250130/
    └─ ...
```

---

## 데이터 구조

### config.json
```json
{
  "normal": "e:/data/normal",
  "nir": "e:/data/nir",
  "output": "e:/output",
  "img_width": 200,
  "img_height": 150,
  "subject": "sample1",
  "today": "250129",
  ...
}
```

### moved_subjects.json
```json
{
  "meta": {
    "date": "20250129",
    "app": "MatchingTool_monitoring"
  },
  "subjects": {
    "sample1": [
      {
        "at": "2025-01-29T10:30:00",
        "mode": "이동",
        "extra": {"count": 50}
      }
    ]
  }
}
```

### move_plan.json
```json
{
  "with_nir": {
    "Nir": {
      "dirs": ["path/to/nir"],
      "files": {"dirname": ["file1.spc"]}
    },
    ...
  },
  "without_nir": {...}
}
```

---

## 사용 예시

### 설정 로드/저장
```python
config_manager = ConfigManager()

# 로드
settings = config_manager.load()

# 저장
settings["subject"] = "new_sample"
config_manager.save(settings)
```

### 이동 기록 확인
```python
exists, last_time = config_manager.was_subject_moved("20250129", "sample1")
if exists:
    print(f"이전 이동: {last_time}")
```

### 이동 기록 추가
```python
config_manager.record_subject_moved(
    "20250129",
    "sample1",
    "2025-01-29T10:30:00",
    "이동",
    {"count": 50}
)
```

---

## 의존성
- `appdirs`: 플랫폼별 사용자 데이터 디렉토리
- `json`: JSON 파일 읽기/쓰기
- `os`: 파일 시스템 작업
- `sys`: 플랫폼 감지
- `subprocess`: 폴더 열기
- `datetime`: 날짜/시간 처리
