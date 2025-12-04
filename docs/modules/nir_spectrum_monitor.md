# nir_spectrum_monitor.py 문서

## 개요
NIR(근적외선) 스펙트럼 파일을 분석하여 김 검출 여부를 판정하는 모듈입니다. pandas 기반 고속 분석과 watchdog 기반 실시간 감시를 제공합니다.

**파일 경로**: `script/apps/nir_spectrum_monitor.py` (모듈화 후 이동)  
**파일 크기**: 203 라인  
**총 함수**: 3개  
**총 클래스**: 1개 (NIRSpectrumMonitor) + 1개 중첩 클래스 (SpectrumHandler)  
**업데이트**: 2025-12-04

---

## 함수

### `load_spectrum(file_path, encoding='cp949') -> DataFrame`

스펙트럼 파일 로드 (최적화 버전)

**매개변수**:
- `file_path`: NIR 스펙트럼 파일 경로 (.txt)
- `encoding`: 파일 인코딩 (기본: cp949)

**반환값**: pandas DataFrame (컬럼: x, y)

**동작**:
1. pandas read_csv로 한 번에 읽기 (Flask 버전보다 5-10배 빠름)
2. 공백 구분자, # 주석 무시
3. 숫자가 아닌 값 자동 제거
4. 실패 시 수동 파싱으로 fallback

---

### `find_y_variation_in_x_window(df, x_window=800, stride=50) -> list`

Y 변화 구간 찾기 (김 검출 알고리즘)

**매개변수**:
- `df`: 스펙트럼 DataFrame
- `x_window`: X 윈도우 크기 (기본: 800)
- `stride`: 슬라이딩 간격 (기본: 50)

**반환값**: 검출된 구간 리스트
```python
[
    {
        'x_start': 5000.0,
        'x_end': 5800.0,
        'y_range': 0.078
    },
    ...
]
```

**검출 기준**:
- X 범위: 4500 ~ 6500
- Y 변화량: 0.05 ~ 0.1
- 슬라이딩 윈도우 방식

---

### `process_file(file_path, dst_dir, log_callback=None)`

파일 처리 (분석 + 판정 + 이동/삭제)

**매개변수**:
- `file_path`: .txt 파일 경로
- `dst_dir`: 이동 대상 폴더
- `log_callback`: 로그 출력 콜백 (선택)

**동작**:
1. 스펙트럼 분석 (`load_spectrum` + `find_y_variation_in_x_window`)
2. **김 검출 → 파일 이동** (txt + 관련 .spc)
3. **김 미검출 → 파일 삭제** (txt + 관련 .spc)

**관련 파일**:
- `filename_A.txt` → `filename.spc` 자동 찾기
- .spc 파일이 있으면 함께 이동/삭제

---

## 클래스: NIRSpectrumMonitor

NIR 스펙트럼 실시간 감시 클래스

### 초기화

#### `__init__(monitor_path, move_path, log_callback=None)`
- `monitor_path`: 감시할 폴더
- `move_path`: 김 검출 파일 이동 폴더
- `log_callback`: 로그 콜백 (선택)

### 메서드

#### `log(msg)`
- **기능**: 로그 출력 (콜백 또는 print)

#### `start()`
- **기능**: watchdog 감시 시작
- **동작**:
  1. SpectrumHandler 생성
  2. Observer 시작 (재귀 감시 OFF)
  3. .txt 파일 생성 감지
  4. 1초 대기 후 `process_file()` 호출
- **블로킹**: 무한 루프 (self.running=True 동안)

#### `stop()`
- **기능**: watchdog 감시 중지
- **동작**: Observer 정지 및 종료

---

## 중첩 클래스: SpectrumHandler

**위치**: NIRSpectrumMonitor 클래스 내부에 정의  
**상속**: `FileSystemEventHandler` (watchdog.events)

### 개요
.txt 파일 생성 이벤트를 감지하여 자동으로 처리하는 이벤트 핸들러

### 초기화

#### `__init__(dst_dir, log_callback, parent_log_callback)`
- **매개변수**:
  - `dst_dir`: 이동 대상 폴더
  - `log_callback`: 로그 콜백 함수
  - `parent_log_callback`: 부모 로그 콜백 함수

### 메서드

#### `on_created(event)`
- **설명**: 파일 생성 이벤트 처리 (오버라이드)
- **동작**:
  1. 디렉토리 이벤트 무시
  2. .txt 파일인지 확인 (소문자 변환 후 체크)
  3. 파일 발견 로그 출력: `📥 새로운 파일 발견: {path}`
  4. 1초 대기 (파일 쓰기 완료 대기)
  5. `process_file()` 호출 (분석 및 이동/삭제)

### 특징
- **자동 처리**: .txt 파일이 생성되면 즉시 분석 시작
- **파일 쓰기 완료 대기**: `time.sleep(1)` 추가
- **로그 이중화**: parent_log_callback과 log_callback 모두 지원

---

## 사용 예시

### 기본 사용 (nir_app.py에서)

```python
from nir_spectrum_monitor import NIRSpectrumMonitor

# 모니터 생성
monitor = NIRSpectrumMonitor(
    monitor_path="E:/NIR/input",
    move_path="E:/NIR/detected",
    log_callback=self.log  # GUI 로그 함수
)

# 별도 스레드에서 시작
import threading
thread = threading.Thread(target=monitor.start)
thread.start()

# 중지
monitor.stop()
thread.join()
```

### 단독 함수 사용

```python
from nir_spectrum_monitor import load_spectrum, find_y_variation_in_x_window

# 스펙트럼 로드
df = load_spectrum("sample_A.txt")

# 김 구간 찾기
regions = find_y_variation_in_x_window(df)

if regions:
    print(f"김 검출! {len(regions)}개 구간")
else:
    print("김 미검출")
```

---

## 김 검출 알고리즘

### 원리
1. **X 범위 필터링**: 4500 ~ 6500 구간만 분석
2. **슬라이딩 윈도우**: 800 크기, 50 간격으로 이동
3. **Y 변화량 체크**: 0.05 ~ 0.1 범위면 김으로 판정

### 성능 최적화
- pandas read_csv: Flask 버전보다 **5-10배 빠름**
- numpy 벡터화: 윈도우 필터링 **2-3배 빠름**
- C 엔진 사용

---

## 파일 처리 흐름

```
.txt 파일 생성 감지
  ↓
watchdog SpectrumHandler.on_created()
  ↓
1초 대기 (파일 쓰기 완료)
  ↓
process_file()
  ├─ load_spectrum() - 파일 로드
  ├─ find_y_variation_in_x_window() - 분석
  └─ 판정
      ├─ 김 검출 → txt + .spc 이동
      └─ 미검출 → txt + .spc 삭제
```

---

## 의존성

- `pandas`: 스펙트럼 데이터 처리
- `watchdog`: 파일 시스템 감시
- `os`, `shutil`: 파일 조작
- `datetime`: 타임스탬프

---

## 주의사항

1. **직접 실행 불가**: nir_app.py를 통해 사용
2. **.spc 쌍**: filename_A.txt → filename.spc 자동 처리
3. **블로킹**: start() 메서드는 블로킹이므로 별도 스레드 필요
4. **인코딩**: 기본 cp949 (한글 Windows)

---

## 연관 모듈

- **[nir_app.md](nir_app.md)**: NIR 모니터링 GUI
- **[config_manager.md](config_manager.md)**: NIR 설정 저장

---

## 최적화 이력

- pandas read_csv: **5-10배 빠름** (vs. 수동 파싱)
- numpy 벡터화: **2-3배 빠름** (vs. pandas 필터링)
- C 엔진 사용
