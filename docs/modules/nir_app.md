# nir_app.py 문서

## 개요
NIR 스펙트럼 모니터링 애플리케이션으로, NIR 파일의 실시간 감시 및 자동 이동 기능을 제공합니다.

**파일 경로**: `script/apps/nir_app.py` (모듈화 후 이동)  
**파일 크기**: 약 495 라인  
**주요 클래스**: `NIRMonitorThread`, `NIRMonitorApp`  
**업데이트**: 2025-12-04

---

## 🔥 최신 변경사항 (2025-12-03 ~ 2025-12-04)

### 1. NIR 상태 모니터링 통합 (신규)
- **NIRStatusManager**: 파일 기반 IPC로 메인 앱에 상태 전달
- **상태 업데이트 타이머**: 2초마다 상태 파일 갱신
- **처리 파일 개수 추적**: `processed_file_count` 변수 추가
- **종료 시 상태 정리**: `clear_status()` 호출

### 2. QTimer 추가
- `status_update_timer`: 2초마다 상태 파일 갱신
- `update_status_file()` 메서드: 타이머 콜백 함수

### 3. 로그 개선
- 파일 처리 로그 시 자동으로 `processed_file_count` 증가
- 마지막 파일명 추출 및 상태 업데이트

---

## 클래스: NIRMonitorThread (신규 추가)

### 개요
NIR 모니터링을 별도 QThread에서 실행하는 워커 스레드

### 시그널
- `log_signal`: 로그 메시지 전달 (Signal(str))
- `error_signal`: 에러 메시지 전달 (Signal(str))

### 메서드

#### `__init__(monitor_path, move_path)`
- **설명**: 스레드 초기화
- **매개변수**:
  - `monitor_path`: 감시 폴더 경로
  - `move_path`: 이동 폴더 경로
- **초기화 변수**:
  - `monitor`: NIRSpectrumMonitor 인스턴스
  - `running`: 실행 상태 플래그

#### `run()`
- **설명**: 스레드 메인 루프 (오버라이드)
- **동작**:
  1. NIRSpectrumMonitor 인스턴스 생성
  2. `running = True` 설정
  3. `monitor.start()` 호출 (무한 루프)
  4. 예외 발생 시 `error_signal` 발생
  5. finally: `running = False`, 종료 로그

#### `stop()`
- **설명**: 모니터링 중지
- **동작**:
  1. `monitor.stop()` 호출
  2. `running = False` 설정

---

## 클래스: NIRMonitorApp

### 개요
NIR 스펙트럼 파일(.spc)을 실시간으로 감시하고 자동으로 지정된 폴더로 이동하는 애플리케이션

### 주요 기능
1. **NIR 파일 실시간 감시**: watchdog를 사용한 파일 시스템 감시
2. **자동 파일 이동**: 설정된 조건에 따라 NIR 파일 자동 이동
3. **경로 자동 날짜 설정**: 프로그램 시작 시 경로의 날짜 부분을 오늘 날짜로 자동 업데이트
4. **로그 출력**: 실시간 작업 로그 표시

---

## 주요 메서드

### 초기화
```python
__init__(self, config_manager, log_emitter_func)
```
- NIR 모니터 애플리케이션 초기화
- 설정 로드 및 UI 구성
- **자동 날짜 업데이트**: 설정 로드 시 경로의 날짜 부분을 오늘 날짜로 자동 변경

### 경로 자동 날짜 설정
```python
auto_update_date_in_path(path: str) -> tuple[str, bool]
```
**기능**: 경로의 마지막 부분이 `\MM\DD` 형식이면 오늘 날짜로 자동 변경

**처리 로직**:
1. 경로의 마지막 2개 디렉토리가 `\숫자2개\숫자2개` 패턴인지 확인
2. 패턴 일치 시 오늘 날짜(`\월\일`)로 교체
3. OS에 맞는 경로 구분자(`\` 또는 `/`) 사용

**예시**:
```python
# 입력: "D:\NIR\01\25"
# 오늘 날짜가 2025-01-26이라면
# 출력: ("D:\NIR\01\26", True)

# 입력: "D:\NIR\data"
# 출력: ("D:\NIR\data", False)  # 패턴 불일치
```

**반환값**:
- `(변경된 경로, 변경 여부)`

**용도**:
- 매일 날짜가 바뀔 때마다 수동으로 경로를 수정할 필요 없이, 프로그램 시작 시 자동으로 오늘 날짜 경로로 설정

---

### 설정 로드
```python
load_settings_to_ui(self)
```
**기능**: 설정 파일에서 UI로 경로 로드 + 날짜 자동 업데이트

**처리 순서**:
1. config에서 `nir_monitor_path`, `nir_move_path` 로드
2. 각 경로에 대해 `auto_update_date_in_path()` 호출
3. 업데이트된 경로를 UI에 표시
4. **변경 사항이 있으면 즉시 config에 저장**
5. 로그 메시지 출력: `"✓ NIR 경로가 오늘 날짜로 자동 업데이트되었습니다."`

**특징**:
- 프로그램 시작 시 자동 호출 (`__init__`에서 실행)
- 변경된 경로는 즉시 config.json에 저장되어 영구 반영

---

### 폴더 열기

#### `open_settings_folder(self)`

설정 폴더를 시스템 탐색기에서 열기

**동작**:
1. `config_manager.open_appdir_folder()` 호출
2. 로그 출력: `"📁 설정 폴더 열기: {경로}"`
3. 예외 발생 시 에러 로그 출력

**예외 처리**:
- 폴더 열기 실패 시: `"❌ 폴더 열기 실패: {에러}"`

---

#### `open_folder(self, edit_widget: QLineEdit)`

경로 입력란의 폴더를 시스템 탐색기에서 열기

**매개변수**:
- `edit_widget`: 폴더 경로가 입력된 QLineEdit 위젯

**동작**:
1. 경로 입력란에서 경로 가져오기 (`.strip()`)
2. 빈 경로 검증
3. **경로 정규화**: `os.path.normpath()`를 사용하여 OS에 맞게 경로 변환
   - 혼합된 슬래시(`/`, `\`)를 OS 표준 구분자로 통일
   - 중복된 슬래시 제거
   - 상대 경로 해석 (`.`, `..` 처리)
4. 폴더 존재 여부 확인 (`os.path.isdir()`)
5. 플랫폼별 폴더 열기:
   - **Windows**: `os.startfile(path)`
   - **macOS**: `subprocess.run(["open", path])`
   - **Linux**: `subprocess.run(["xdg-open", path])`
6. 성공 시 로그: `"📁 폴더 열기: {경로}"`

**예외 처리**:
- 빈 경로: `"❌ 폴더 경로가 비어있습니다."`
- 폴더 미존재: `"❌ 폴더가 존재하지 않습니다: {경로}"`
- FileNotFoundError: `"❌ 폴더가 존재하지 않습니다: {경로}"`
- PermissionError: `"❌ 폴더 접근 권한이 없습니다: {경로}"`
- 기타 예외: `"❌ 폴더 열기 실패: {에러}"`

**정규화 예시**:
```python
# 입력: "D:/NIR//folder\\data"
# 정규화: "D:\NIR\folder\data" (Windows)

# 입력: "./data/../output"
# 정규화: "output"
```

---

### 파일 감시
```python
start_monitoring(self)
stop_monitoring(self)
```
- NIR 파일 감시 시작/중지
- watchdog를 사용한 실시간 파일 이벤트 감지

---

## 설정 항목

### 경로 설정
- `nir_monitor_path`: NIR 파일 감시 경로
  - 예: `D:\NIR\01\26`
  - 프로그램 시작 시 날짜 부분 자동 업데이트
- `nir_move_path`: NIR 파일 이동 대상 경로
  - 예: `D:\NIR_Archive\01\26`
  - 프로그램 시작 시 날짜 부분 자동 업데이트

### 작업 설정
- `auto_move`: 자동 이동 활성화 여부
- `move_delay`: 이동 전 대기 시간 (초)

---

## 워크플로우

### 프로그램 시작
```
사용자 [프로그램 실행]
  → __init__()
  → load_settings_to_ui()
  → auto_update_date_in_path(monitor_path)
  → auto_update_date_in_path(move_path)
  → 변경사항 있으면 config.json에 즉시 저장
  → UI 표시
  → 로그 출력
```

### NIR 파일 감시
```
NIR 파일 생성 (watchdog 감지)
  → on_created 이벤트
  → 조건 확인 (확장자, 크기 등)
  → 대기 시간 경과
  → 이동 대상 경로로 파일 이동
  → 로그 출력
```

---

## 경로 날짜 패턴 인식

### 패턴 매칭
정규표현식: `r'(.*)[\\/](\d{2})[\\/](\d{2})$'`

**매칭 예시** (✓):
- `D:\NIR\01\26`
- `C:\Data\12\31`
- `/home/user/data/03/15`

**매칭 실패** (✗):
- `D:\NIR\2025\01\26` (월/일이 각각 2자리가 아님)
- `D:\NIR\data` (숫자 디렉토리 없음)
- `D:\NIR\001\026` (3자리 숫자)

---

## 크로스 플랫폼 지원

### 경로 구분자
- **Windows**: `\` (백슬래시)
- **Unix/Linux**: `/` (슬래시)

**자동 감지**: 원본 경로에 사용된 구분자를 그대로 사용
```python
separator = '\\' if '\\' in path else '/'
```

---

## 의존성
- `PySide6`: Qt GUI 프레임워크
- `watchdog`: 파일 시스템 감시
- `config_manager`: 설정 관리
- `datetime`: 날짜 처리
- `re`: 정규표현식 (경로 패턴 매칭)
- `pathlib`: 경로 처리

---

## 데이터 구조

### 설정 (config.json)
```python
{
    "nir_monitor_path": "D:\\NIR\\01\\26",  # 자동 업데이트됨
    "nir_move_path": "D:\\NIR_Archive\\01\\26",  # 자동 업데이트됨
    "auto_move": true,
    "move_delay": 3.0
}
```

---

## 주요 특징

1. **자동 날짜 업데이트**: 매일 수동으로 경로 변경 불필요
2. **즉시 저장**: 변경된 경로를 즉시 config에 저장하여 영구 반영
3. **패턴 기반 인식**: 경로의 마지막 2개 디렉토리가 `\숫자2개\숫자2개` 형식인 경우만 처리
4. **크로스 플랫폼**: Windows/Unix 경로 구분자 자동 처리
5. **안전한 처리**: 패턴 불일치 시 원본 경로 유지

---

## 사용 시나리오

### 일일 작업 흐름
```
2025-01-25 (금요일)
  → config.json에 저장된 경로: "D:\NIR\01\25"
  → 프로그램 종료

2025-01-26 (토요일)
  → 프로그램 시작
  → 자동으로 "D:\NIR\01\26"으로 업데이트
  → config.json 즉시 저장
  → 사용자는 별도 설정 변경 불필요
```

---

## 에러 처리

### 경로 패턴 불일치
- 마지막 디렉토리가 날짜 형식이 아닌 경우
- **동작**: 원본 경로 유지, 변경 없음
- **로그**: 출력 없음 (정상 동작)

### 파일 시스템 에러
- 경로가 존재하지 않는 경우
- **동작**: watchdog 시작 실패 시 에러 로그 출력

---

## 향후 개선 사항

1. **다양한 날짜 형식 지원**: `\YYYY\MM\DD`, `\YYYYMMDD` 등
2. **수동 날짜 변경 방지**: 사용자가 수동으로 날짜를 변경해도 다음 시작 시 오늘 날짜로 복원
3. **날짜 범위 검증**: 유효한 월(01-12), 일(01-31)인지 검증
