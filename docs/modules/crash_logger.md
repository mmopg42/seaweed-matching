# crash_logger.py 문서

## 개요
전역 크래시 로거 모듈입니다. 처리되지 않은 예외를 파일로 기록하고 분석 가능한 형식으로 저장합니다.

**파일 경로**: `script/debug/crash_logger.py`  
**파일 크기**: 118 라인  
**총 클래스**: 1개 (`CrashLogger`)  
**총 함수**: 2개 (`setup_crash_logger`, `get_crash_logger`)  
**생성일**: 2025-12-03  
**의존성**: logging, psutil (선택)

---

## 핵심 기능

### 1. 전역 예외 처리
- `sys.excepthook`을 사용하여 처리되지 않은 예외 자동 캡처
- 예외 발생 시 로그 파일에 상세 정보 기록
- 시스템 정보 및 메모리 사용량 포함

### 2. 로그 파일 관리
- 타임스탬프가 포함된 로그 파일명 자동 생성
- 예: `crash_monitoring_app_20251203_152310.txt`
- 로그 디렉토리 자동 생성

### 3. 상세 크래시 리포트
- 스택 트레이스 완전 기록
- Python 버전 정보
- 메모리 사용량 (psutil 사용 시)

---

## 클래스

### `CrashLogger`

애플리케이션 크래시를 로깅하는 클래스

#### 초기화

```python
CrashLogger(log_dir="logs", app_name="monitoring_app")
```

**매개변수:**
- `log_dir` (str): 로그 파일 저장 디렉토리 (기본: "logs")
- `app_name` (str): 애플리케이션 이름, 로그 파일명에 사용

**동작:**
1. 로그 디렉토리 생성
2. 타임스탬프 기반 로그 파일명 생성
3. 로깅 설정
4. 전역 예외 핸들러 설치

---

### 메서드

#### `_setup_logging()`
- **설명**: 로깅 시스템 초기화
- **동작**:
  - 파일 핸들러와 스트림 핸들러 설정
  - UTF-8 인코딩
  - INFO 레벨 로깅

#### `_install_exception_hook()`
- **설명**: 전역 예외 핸들러를 `sys.excepthook`에 설치
- **동작**:
  1. 기존 excepthook 백업
  2. 커스텀 예외 핸들러 정의
  3. KeyboardInterrupt는 정상 처리
  4. 크래시 리포트 작성 후 원래 핸들러 호출

#### `_write_crash_report(exc_type, exc_value, exc_traceback)`
- **설명**: 상세 크래시 리포트 작성
- **매개변수**:
  - `exc_type`: 예외 타입
  - `exc_value`: 예외 값
  - `exc_traceback`: 스택 트레이스
- **기록 내용**:
  - 타임스탬프
  - 완전한 스택 트레이스
  - Python 버전
  - 메모리 사용량 (psutil 있으면)

#### `get_log_path()`
- **설명**: 로그 파일 경로 반환
- **반환값**: 로그 파일의 절대 경로 문자열

---

## 전역 함수

### `setup_crash_logger(log_dir="logs", app_name="monitoring")`

크래시 로거 초기화 (싱글톤 패턴)

**매개변수:**
- `log_dir`: 로그 디렉토리
- `app_name`: 애플리케이션 이름

**반환값:**
- 로그 파일 경로 (str)

**사용 예시:**
```python
from debug import setup_crash_logger

log_file = setup_crash_logger(log_dir="logs", app_name="monitoring")
print(f"Log: {log_file}")
```

### `get_crash_logger()`

현재 크래시 로거 인스턴스 반환

**반환값:**
- `CrashLogger` 인스턴스 또는 `None`

---

## 사용 예시

### 기본 사용

```python
from debug import setup_crash_logger

# 애플리케이션 시작 시
log_file = setup_crash_logger(log_dir="logs", app_name="monitoring")
print(f"Crash log: {log_file}")

# 이후 모든 처리되지 않은 예외는 자동으로 로그됨
```

### main.py에서의 사용

```python
import sys
import logging
from PySide6.QtWidgets import QApplication
from debug import setup_crash_logger, setup_signal_handlers

# 크래시 로거 설치
log_file = setup_crash_logger(log_dir="logs", app_name="monitoring")
setup_signal_handlers()

logging.info("="*80)
logging.info(f"📝 Crash log: {log_file}")
logging.info("🚀 애플리케이션 시작")
logging.info("="*80)

app = QApplication(sys.argv)
# ...
```

---

## 로그 파일 예시

```
2025-12-03 15:23:10 [INFO] __main__: 🔧 CrashLogger 초기화: logs/crash_monitoring_20251203_152310.txt
2025-12-03 15:23:10 [INFO] __main__: ✅ 전역 예외 핸들러 설치 완료
2025-12-03 15:23:15 [CRITICAL] __main__: 💥 처리되지 않은 예외 발생!
Traceback (most recent call last):
  File "monitoring_app.py", line 123, in process_data
    result = 1 / 0
ZeroDivisionError: division by zero

================================================================================
💥 CRASH REPORT
Time: 2025-12-03 15:23:15.123456
================================================================================

[스택 트레이스...]

--------------------------------------------------------------------------------
System Information:
Python: 3.11.5 (main, Sep 11 2023, 13:54:46) [MSC v.1936 64 bit (AMD64)]
Memory: 245.3 MB
```

---

## 설계 특징

### 1. 싱글톤 패턴
- 애플리케이션 당 하나의 크래시 로거만 존재
- `setup_crash_logger()` 재호출 시 기존 인스턴스 반환

### 2. 비침습적
- 기존 코드 변경 없이 사용 가능
- `sys.excepthook`만 설치하면 자동 동작

### 3. 상세한 정보
- 스택 트레이스 완전 기록
- 시스템 환경 정보
- 메모리 사용량 (선택)

---

## 주의사항

### 1. QThread 예외
- Qt의 워커 스레드(`QThread`)에서 발생한 예외는 `sys.excepthook`으로 캡처되지 않음
- 워커 스레드는 `try-except`로 명시적 처리 필요

### 2. KeyboardInterrupt
- Ctrl+C는 정상 종료로 처리하여 로그하지 않음

### 3. 로그 파일 위치
- 로그 디렉토리가 없으면 자동 생성
- 쓰기 권한 확인 필요

---

**작성일:** 2025-12-04  
**관련 모듈:** `signal_handler.py`, `thread_monitor.py`


