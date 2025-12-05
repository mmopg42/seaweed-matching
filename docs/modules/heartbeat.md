# heartbeat.py 문서

## 개요
주기적으로 "살아있음"을 로깅하고 상태 정보를 기록하는 Heartbeat 모니터 모듈입니다.

**파일 경로**: `script/debug/heartbeat.py`  
**파일 크기**: 76 라인  
**총 클래스**: 1개 (`Heartbeat`)  
**생성일**: 2025-12-03  
**의존성**: PySide6 (QTimer, QObject)

---

## 핵심 기능

### 1. 주기적 상태 로깅
- QTimer를 사용하여 일정 간격으로 로그 기록
- 애플리케이션이 살아있음을 확인

### 2. 상태 수집
- 커스텀 상태 수집 함수 등록 가능
- 메모리, 스레드, 그룹 개수 등 수집

### 3. 크래시 시점 파악
- Heartbeat 로그를 통해 크래시 직전 시점 확인 가능

---

## 클래스

### `Heartbeat(QObject)`

주기적 상태 로깅 클래스

#### 초기화

```python
Heartbeat(interval_sec=60, parent=None)
```

**매개변수:**
- `interval_sec` (int): Heartbeat 간격 (초, 기본 60초)
- `parent`: Qt 부모 객체 (선택)

**동작:**
1. 간격을 밀리초로 변환
2. QTimer 생성 및 연결
3. 상태 수집 콜백 리스트 초기화
4. 로그 기록

**인스턴스 변수:**
- `interval_ms`: Heartbeat 간격 (밀리초)
- `beat_count`: Heartbeat 실행 횟수
- `status_collectors`: 상태 수집 함수 리스트
- `timer`: QTimer 인스턴스

---

### 메서드

#### `start()`

Heartbeat 시작

**동작:**
- QTimer 시작
- 로그: "💓 Heartbeat 시작"

**사용 예시:**
```python
heartbeat = Heartbeat(interval_sec=30)
heartbeat.start()
```

---

#### `stop()`

Heartbeat 중지

**동작:**
- QTimer 중지
- 로그: "💓 Heartbeat 중지"

**사용 예시:**
```python
heartbeat.stop()
```

---

#### `add_status_collector(name: str, func: callable)`

상태 수집 함수 등록

**매개변수:**
- `name` (str): 상태 이름 (예: "memory", "groups_count")
- `func` (callable): 상태 값을 반환하는 함수

**사용 예시:**
```python
def get_memory():
    import psutil
    return psutil.Process().memory_info().rss / 1024 / 1024

heartbeat.add_status_collector("memory_mb", get_memory)
```

---

#### `_beat()`

Heartbeat 실행 (내부 메서드)

**동작:**
1. `beat_count` 증가
2. 타임스탬프 생성
3. 모든 상태 수집 함수 실행
4. 로그 기록

**로그 형식:**
```
💓 Heartbeat #1: beat=1, timestamp=2025-12-03T15:30:00.000000, memory_mb=245.3, groups_count=12
```

**에러 처리:**
- 상태 수집 함수에서 예외 발생 시 "ERROR: ..." 기록
- Heartbeat는 계속 동작

---

#### `force_beat()`

즉시 Heartbeat 실행 (테스트용)

**사용 예시:**
```python
heartbeat.force_beat()  # 타이머 대기 없이 즉시 실행
```

---

## 사용 예시

### 기본 사용

```python
from debug import Heartbeat

# 30초마다 로깅
heartbeat = Heartbeat(interval_sec=30)
heartbeat.start()

# 종료 시
heartbeat.stop()
```

### 상태 수집 함수 등록

```python
from debug import Heartbeat

heartbeat = Heartbeat(interval_sec=60)

# 메모리 수집
def get_memory():
    import psutil
    process = psutil.Process()
    return f"{process.memory_info().rss / 1024 / 1024:.1f} MB"

heartbeat.add_status_collector("memory", get_memory)

# 그룹 개수 수집
def get_groups_count():
    return len(self.groups)

heartbeat.add_status_collector("groups", get_groups_count)

heartbeat.start()
```

### monitoring_app.py에서의 사용

```python
from debug import Heartbeat

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # Heartbeat 초기화 (60초 간격)
        self.heartbeat = Heartbeat(interval_sec=60, parent=self)
        
        # 상태 수집 함수 등록
        self.heartbeat.add_status_collector("groups_count", lambda: len(self.groups))
        self.heartbeat.add_status_collector("is_watching", lambda: self.is_watching)
        
        # 시작
        self.heartbeat.start()
    
    def closeEvent(self, event):
        # 종료 시 정리
        self.heartbeat.stop()
        event.accept()
```

---

## 로그 예시

```
2025-12-03 15:30:00 [INFO] debug.heartbeat: 💓 Heartbeat 초기화: 60초 (60000ms) 간격
2025-12-03 15:30:00 [INFO] debug.heartbeat: 💓 Heartbeat 시작
2025-12-03 15:31:00 [INFO] debug.heartbeat: 💓 Heartbeat #1: beat=1, timestamp=2025-12-03T15:31:00.000000, memory=245.3 MB, groups=12
2025-12-03 15:32:00 [INFO] debug.heartbeat: 💓 Heartbeat #2: beat=2, timestamp=2025-12-03T15:32:00.000000, memory=247.1 MB, groups=15
2025-12-03 15:33:00 [INFO] debug.heartbeat: 💓 Heartbeat #3: beat=3, timestamp=2025-12-03T15:33:00.000000, memory=248.5 MB, groups=18
```

---

## 설계 특징

### 1. QObject 상속
- Qt의 이벤트 루프와 통합
- QTimer로 정확한 타이밍 보장

### 2. 확장 가능
- `add_status_collector`로 커스텀 상태 추가
- 애플리케이션별 모니터링 항목 구성

### 3. 크래시 분석 지원
- 로그를 통해 크래시 직전 상태 파악
- 타임스탬프로 이벤트 타임라인 구성

---

## 크래시 분석 활용

### 시나리오: 크래시 발생 시점 파악

```
2025-12-03 15:30:00 [INFO] 💓 Heartbeat #1: beat=1, groups=12
2025-12-03 15:31:00 [INFO] 💓 Heartbeat #2: beat=2, groups=15
2025-12-03 15:32:00 [INFO] 💓 Heartbeat #3: beat=3, groups=18
# 이 시점에 크래시 발생!
2025-12-03 15:32:15 [CRITICAL] 💥 처리되지 않은 예외 발생!
```

**분석:**
- 마지막 Heartbeat: 15:32:00
- 크래시 시점: 15:32:15
- 크래시 직전 그룹 개수: 18개
- 크래시는 Heartbeat 15초 후 발생

---

## 주의사항

### 1. 간격 설정
- 너무 짧은 간격(< 10초)은 로그 과다 생성
- 권장: 30~60초

### 2. 상태 수집 함수
- 빠르게 실행되는 함수만 등록 (< 100ms)
- 예외 발생 시 Heartbeat는 계속 동작

### 3. 메모리
- Heartbeat 자체는 메모리 사용량 매우 적음
- 상태 수집 함수가 메모리를 많이 사용할 수 있음

---

**작성일:** 2025-12-04  
**관련 모듈:** `crash_logger.py`, `memory_monitor.py`



