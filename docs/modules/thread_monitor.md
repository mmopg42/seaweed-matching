# thread_monitor.py 문서

## 개요
워커 스레드의 예외를 캐치하고 로깅하는 모니터링 모듈입니다.

**파일 경로**: `script/debug/thread_monitor.py`  
**파일 크기**: 76 라인  
**총 함수**: 1개 (`monitor_thread` 데코레이터)  
**총 클래스**: 1개 (`ThreadMonitor`)  
**생성일**: 2025-12-03  
**의존성**: logging, functools

---

## 핵심 기능

### 1. 스레드 예외 캐치
- QThread의 `run()` 메서드를 데코레이터로 감싸서 예외 자동 로깅
- `sys.excepthook`이 캡처하지 못하는 스레드 예외 처리

### 2. 스레드 그룹 모니터링
- 여러 스레드를 등록하고 상태 확인
- 실행 중/중지 상태 추적

### 3. 시작/종료 로깅
- 스레드 시작 및 종료 시점 자동 기록

---

## 데코레이터

### `@monitor_thread(thread_name=None)`

스레드 `run()` 메서드를 감싸서 예외 로깅

**매개변수:**
- `thread_name` (str, 선택): 스레드 이름 (기본값: 함수명)

**동작:**
1. 스레드 시작 로그: "🚀 스레드 시작: {name}"
2. `run()` 메서드 실행
3. 예외 발생 시:
   - 로그 기록 (CRITICAL 레벨, 스택 트레이스 포함)
   - `error_occurred` 시그널 발생 (있으면)
   - 예외 재발생
4. 스레드 종료 로그: "🛑 스레드 종료: {name}"

**사용 예시:**
```python
from PySide6.QtCore import QThread
from debug import monitor_thread

class ImageLoaderWorker(QThread):
    @monitor_thread("ImageLoader")
    def run(self):
        # 작업 수행
        while self.running:
            # ...
            pass
```

---

## 클래스

### `ThreadMonitor`

스레드 그룹 모니터링 클래스

#### 초기화

```python
ThreadMonitor()
```

**인스턴스 변수:**
- `threads`: 스레드 딕셔너리 `{name: thread_object}`
- `_last_check`: 마지막 체크 시간

---

### 메서드

#### `register(name: str, thread)`

스레드 등록

**매개변수:**
- `name` (str): 스레드 이름
- `thread`: 스레드 객체 (QThread 인스턴스)

**동작:**
- 스레드를 딕셔너리에 추가
- 로그: "📝 스레드 등록: {name}"

**사용 예시:**
```python
monitor = ThreadMonitor()
monitor.register("ImageLoader", self.image_loader)
monitor.register("FileOperation", self.file_operation_worker)
```

---

#### `check_all()`

모든 스레드 상태 확인

**반환값:**
- `dict`: 스레드 이름을 키로, 실행 상태를 값으로 하는 딕셔너리

**반환 예시:**
```python
{
    'ImageLoader': True,      # 실행 중
    'FileOperation': False,   # 중지됨
    'FileMatcher': True       # 실행 중
}
```

**동작:**
- 모든 등록된 스레드의 `isRunning()` 상태 확인
- 비활성 스레드는 경고 로그 기록

**사용 예시:**
```python
status = monitor.check_all()
for name, is_running in status.items():
    print(f"{name}: {'실행 중' if is_running else '중지됨'}")
```

---

#### `get_summary()`

스레드 상태 요약

**반환값:**
- `str`: 요약 문자열 (예: "2/3 running")

**사용 예시:**
```python
summary = monitor.get_summary()
print(f"스레드 상태: {summary}")  # "2/3 running"
```

---

## 사용 예시

### 데코레이터 사용

```python
from PySide6.QtCore import QThread, Signal
from debug import monitor_thread

class ImageLoaderWorker(QThread):
    error_occurred = Signal(str)
    
    @monitor_thread("ImageLoader")
    def run(self):
        try:
            while self.running:
                # 이미지 로딩 작업
                self.load_image()
        except Exception as e:
            # 예외는 자동으로 로깅됨
            raise
```

### ThreadMonitor 사용

```python
from debug import ThreadMonitor

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # ThreadMonitor 초기화
        self.thread_monitor = ThreadMonitor()
        
        # 워커 스레드 등록
        self.thread_monitor.register("ImageLoader", self.image_loader)
        self.thread_monitor.register("FileOperation", self.file_operation_worker)
        self.thread_monitor.register("FileMatcher", self.file_matcher)
        
        # 주기적 체크 (예: Heartbeat와 함께)
        self.heartbeat.add_status_collector(
            "threads",
            lambda: self.thread_monitor.get_summary()
        )
    
    def check_thread_health(self):
        """스레드 상태 확인"""
        status = self.thread_monitor.check_all()
        print(f"스레드 상태: {status}")
```

---

## 로그 예시

### 정상 동작

```
2025-12-03 15:30:00 [INFO] 🚀 스레드 시작: ImageLoader
2025-12-03 15:30:00 [DEBUG] 📝 스레드 등록: ImageLoader
2025-12-03 15:30:00 [INFO] 🚀 스레드 시작: FileOperation
2025-12-03 15:30:00 [DEBUG] 📝 스레드 등록: FileOperation
```

### 스레드 크래시

```
2025-12-03 15:35:00 [INFO] 🚀 스레드 시작: ImageLoader
2025-12-03 15:35:15 [CRITICAL] 💥 스레드 크래시: ImageLoader
Traceback (most recent call last):
  File "image_loader.py", line 123, in run
    pixmap = QPixmap(image_path)
  File ...
ValueError: Invalid image path
2025-12-03 15:35:15 [INFO] 🛑 스레드 종료: ImageLoader
```

### 스레드 비활성 경고

```
2025-12-03 15:40:00 [WARNING] ⚠️ 스레드 비활성: ImageLoader
```

---

## 설계 특징

### 1. 데코레이터 패턴
- 기존 코드 수정 최소화
- `@monitor_thread` 추가만으로 모니터링 활성화

### 2. 스택 트레이스 보존
- `exc_info=True`로 완전한 스택 트레이스 기록
- 크래시 원인 분석 용이

### 3. 시그널 통합
- `error_occurred` 시그널이 있으면 자동 발생
- UI에 에러 알림 가능

---

## QThread 예외 처리 문제

### 문제: sys.excepthook이 동작하지 않음

```python
class Worker(QThread):
    def run(self):
        # 예외 발생!
        result = 1 / 0  # ← sys.excepthook이 캡처하지 못함!
```

**이유:**
- QThread는 별도의 이벤트 루프에서 실행
- `sys.excepthook`은 메인 스레드의 예외만 캡처

### 해결: monitor_thread 데코레이터 사용

```python
class Worker(QThread):
    @monitor_thread("Worker")
    def run(self):
        result = 1 / 0  # ← 이제 캡처됨!
```

---

## 주의사항

### 1. 데코레이터는 run()에만
- `run()` 메서드에만 `@monitor_thread` 사용
- 다른 메서드는 일반 `try-except` 사용

### 2. 예외 재발생
- 데코레이터는 예외를 재발생시킴 (`raise`)
- 스레드는 여전히 종료됨

### 3. 시그널 발생
- `error_occurred` 시그널이 없으면 시그널 발생 생략
- 에러 처리 로직에 따라 시그널 정의

### 4. ThreadMonitor vs. 데코레이터
- **데코레이터**: 예외 로깅
- **ThreadMonitor**: 상태 모니터링
- 두 가지를 함께 사용 권장

---

**작성일:** 2025-12-04  
**관련 모듈:** `crash_logger.py`, `heartbeat.py`



