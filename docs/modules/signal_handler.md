# signal_handler.py 문서

## 개요
OS 시그널(SIGTERM, SIGINT 등)을 감지하여 로깅하고 처리하는 모듈입니다.

**파일 경로**: `script/debug/signal_handler.py`  
**파일 크기**: 52 라인  
**총 함수**: 1개 (`setup_signal_handlers`)  
**생성일**: 2025-12-03  
**의존성**: signal, logging

---

## 핵심 기능

### 1. OS 레벨 종료 신호 감지
- SIGTERM: 정상 종료 요청
- SIGINT: Ctrl+C
- SIGBREAK: Ctrl+Break (Windows only)

### 2. 시그널 로깅
- 시그널 이름 및 코드 기록
- 발생 위치 (파일명, 라인 번호) 기록

### 3. 긴급 정리 작업
- 시그널 수신 시 정리 작업 수행 가능
- 중요 데이터 저장 (TODO)

---

## 함수

### `setup_signal_handlers()`

OS 시그널 핸들러 설치

**동작:**
1. 시그널 핸들러 함수 정의
2. SIGTERM, SIGINT 핸들러 등록
3. Windows에서는 SIGBREAK도 등록

**시그널 핸들러 동작:**
1. 시그널 이름 확인 (예: "SIGTERM")
2. 로그 기록 (CRITICAL 레벨)
3. 발생 위치 기록 (파일명, 라인 번호)
4. 긴급 정리 작업 (현재는 로그만)
5. `sys.exit(1)`로 프로그램 종료

**사용 예시:**
```python
from debug import setup_signal_handlers

# 애플리케이션 시작 시
setup_signal_handlers()
```

---

## 사용 예시

### main.py에서의 사용

```python
import sys
import logging
from PySide6.QtWidgets import QApplication
from debug import setup_crash_logger, setup_signal_handlers

# 크래시 로거 및 시그널 핸들러 설치
log_file = setup_crash_logger(log_dir="logs", app_name="monitoring")
setup_signal_handlers()

logging.info("="*80)
logging.info(f"📝 Crash log: {log_file}")
logging.info("🚀 애플리케이션 시작")
logging.info("="*80)

app = QApplication(sys.argv)
# ...
sys.exit(app.exec())
```

---

## 로그 예시

### 정상 설치

```
2025-12-03 15:30:00 [INFO] ✅ 시그널 핸들러 설치 완료 (SIGTERM, SIGINT)
2025-12-03 15:30:00 [INFO] ✅ Windows 시그널 핸들러 추가 (SIGBREAK)
```

### SIGTERM 수신 (정상 종료 요청)

```
2025-12-03 15:35:00 [CRITICAL] 💥 시그널 수신: SIGTERM (코드: 15)
2025-12-03 15:35:00 [CRITICAL] 📍 프레임: monitoring_app.py:1234
2025-12-03 15:35:00 [INFO] 🧹 긴급 정리 작업 시작...
2025-12-03 15:35:00 [INFO] ❌ 프로그램 강제 종료됨
```

### SIGINT 수신 (Ctrl+C)

```
2025-12-03 15:40:00 [CRITICAL] 💥 시그널 수신: SIGINT (코드: 2)
2025-12-03 15:40:00 [CRITICAL] 📍 프레임: monitoring_app.py:567
2025-12-03 15:40:00 [INFO] 🧹 긴급 정리 작업 시작...
2025-12-03 15:40:00 [INFO] ❌ 프로그램 강제 종료됨
```

---

## 지원 시그널

### SIGTERM (15)
- **의미**: 정상 종료 요청
- **발생 시점**: 
  - 작업 관리자에서 프로세스 종료
  - `kill` 명령어
  - 시스템 종료 시

### SIGINT (2)
- **의미**: 인터럽트 요청
- **발생 시점**:
  - Ctrl+C 입력
  - 터미널 종료

### SIGBREAK (21, Windows only)
- **의미**: Break 요청
- **발생 시점**:
  - Ctrl+Break 입력 (Windows)

---

## 설계 특징

### 1. 플랫폼 독립적
- Windows, Linux, macOS 모두 지원
- Windows 전용 시그널(SIGBREAK)은 조건부 등록

### 2. 로깅 우선
- 시그널 수신 즉시 로그 기록
- 프로그램 종료 전 마지막 정보 확보

### 3. 정리 작업 확장 가능
- TODO 주석으로 확장 지점 표시
- 중요 데이터 저장, 스레드 중지 등 추가 가능

---

## 긴급 정리 작업 확장 예시

```python
def signal_handler(signum, frame):
    # ... (기존 로깅 코드)
    
    # 긴급 정리 작업
    logger.info("🧹 긴급 정리 작업 시작...")
    
    # 현재 작업 상태 저장
    try:
        if hasattr(main_window, 'save_state'):
            main_window.save_state()
            logger.info("✅ 작업 상태 저장 완료")
    except Exception as e:
        logger.error(f"❌ 작업 상태 저장 실패: {e}")
    
    # 메모리 통계 저장
    try:
        if hasattr(memory_monitor, 'get_stats'):
            stats = memory_monitor.get_stats()
            logger.info(f"📊 메모리 통계: {stats}")
    except Exception as e:
        logger.error(f"❌ 메모리 통계 수집 실패: {e}")
    
    # 스레드 상태 기록
    try:
        if hasattr(thread_monitor, 'check_all'):
            status = thread_monitor.check_all()
            logger.info(f"🧵 스레드 상태: {status}")
    except Exception as e:
        logger.error(f"❌ 스레드 상태 확인 실패: {e}")
    
    logger.info("❌ 프로그램 강제 종료됨")
    sys.exit(1)
```

---

## 주의사항

### 1. 시그널 제한
- 모든 시그널을 캡처할 수 없음
- SIGKILL (강제 종료)은 캡처 불가

### 2. Qt 애플리케이션
- Qt 이벤트 루프 실행 중 시그널 처리 지연 가능
- `QApplication.quit()` 대신 `sys.exit()` 사용

### 3. Windows vs. Unix
- Windows와 Unix의 시그널 동작 차이 존재
- SIGBREAK는 Windows 전용

### 4. 정리 작업 시간
- 시그널 핸들러는 빠르게 실행되어야 함
- 긴 작업은 타임아웃 설정 필요

---

## 시그널 vs. 예외

| 구분 | 시그널 | 예외 |
|------|--------|------|
| 발생 위치 | OS 레벨 | Python 코드 |
| 처리 방법 | signal.signal() | try-except |
| 예시 | SIGTERM, SIGINT | ZeroDivisionError |
| 핸들러 | setup_signal_handlers | crash_logger |

---

**작성일:** 2025-12-04  
**관련 모듈:** `crash_logger.py`, `heartbeat.py`



