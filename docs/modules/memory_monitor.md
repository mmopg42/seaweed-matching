# memory_monitor.py 문서

## 개요
메모리 사용량을 주기적으로 모니터링하고 임계값 초과 시 경고하는 모듈입니다.

**파일 경로**: `script/debug/memory_monitor.py`  
**파일 크기**: 93 라인  
**총 클래스**: 1개 (`MemoryMonitor`)  
**생성일**: 2025-12-03  
**의존성**: psutil, PySide6 (QTimer)

---

## 핵심 기능

### 1. 주기적 메모리 체크
- QTimer를 사용하여 일정 간격으로 메모리 사용량 확인
- 프로세스 메모리 (MB) 및 시스템 메모리 (%) 모니터링

### 2. 임계값 경고
- 시스템 메모리가 임계값 초과 시 경고 로그
- 프로세스 메모리가 2GB 초과 시 치명적 로그

### 3. Peak 메모리 추적
- 프로그램 실행 중 최대 메모리 사용량 기록

---

## 클래스

### `MemoryMonitor`

메모리 사용량 모니터 클래스

#### 초기화

```python
MemoryMonitor(interval_sec=60, threshold_percent=80)
```

**매개변수:**
- `interval_sec` (int): 체크 인터벌 (초, 기본 60초)
- `threshold_percent` (int): 경고 임계값 (%, 기본 80%)

**동작:**
1. psutil Process 객체 생성
2. QTimer 설정 및 시작
3. Peak 메모리 및 체크 횟수 초기화
4. 즉시 1회 메모리 체크 실행

**인스턴스 변수:**
- `interval`: 체크 인터벌 (초)
- `threshold`: 경고 임계값 (%)
- `process`: psutil Process 인스턴스
- `timer`: QTimer 인스턴스
- `peak_memory_mb`: Peak 메모리 (MB)
- `check_count`: 체크 실행 횟수

---

### 메서드

#### `_check_memory()`

메모리 사용량 체크 (내부 메서드)

**동작:**
1. 프로세스 메모리 확인 (MB)
2. 시스템 메모리 확인 (%)
3. Peak 메모리 업데이트
4. 5분마다 정상 로그 기록
5. 임계값 초과 시 경고 로그
6. 프로세스 메모리 2GB 초과 시 치명적 로그

**로그 예시:**
```
# 정상 (5분마다)
💾 메모리: 245.3 MB (Peak: 280.1 MB) | 시스템: 65.2%

# 경고 (시스템 메모리 > 80%)
⚠️ 시스템 메모리 부족! 85.3% > 80% (사용: 27.2 GB / 32.0 GB)

# 치명적 (프로세스 메모리 > 2GB)
🔴 프로세스 메모리 과다! 2150.3 MB > 2048 MB (Peak: 2150.3 MB)
```

---

#### `stop()`

모니터링 중지

**동작:**
1. QTimer 중지
2. 종료 로그 기록 (Peak 메모리, 총 체크 횟수)

**사용 예시:**
```python
monitor.stop()
```

**로그 예시:**
```
💾 MemoryMonitor 종료 (Peak: 280.1 MB, 총 체크: 15회)
```

---

#### `get_stats()`

현재 메모리 통계 반환

**반환값:**
- `dict`: 메모리 통계 딕셔너리

**반환 구조:**
```python
{
    'current_mb': 245.3,  # 현재 메모리 (MB)
    'peak_mb': 280.1,     # Peak 메모리 (MB)
    'check_count': 15     # 체크 횟수
}
```

**사용 예시:**
```python
stats = monitor.get_stats()
print(f"현재: {stats['current_mb']:.1f} MB, Peak: {stats['peak_mb']:.1f} MB")
```

---

## 사용 예시

### 기본 사용

```python
from debug import MemoryMonitor

# 60초마다 체크, 80% 임계값
monitor = MemoryMonitor(interval_sec=60, threshold_percent=80)

# 종료 시
monitor.stop()
```

### 커스텀 설정

```python
from debug import MemoryMonitor

# 30초마다 체크, 70% 임계값 (더 민감하게)
monitor = MemoryMonitor(interval_sec=30, threshold_percent=70)

# 통계 확인
stats = monitor.get_stats()
print(f"현재 메모리: {stats['current_mb']:.1f} MB")
print(f"Peak 메모리: {stats['peak_mb']:.1f} MB")
print(f"체크 횟수: {stats['check_count']}회")

# 종료
monitor.stop()
```

### monitoring_app.py에서의 사용

```python
from debug import MemoryMonitor

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # MemoryMonitor 초기화
        self.memory_monitor = MemoryMonitor(
            interval_sec=60,      # 1분마다
            threshold_percent=80  # 80% 초과 시 경고
        )
    
    def closeEvent(self, event):
        # 종료 시 정리
        self.memory_monitor.stop()
        event.accept()
```

---

## 로그 예시

### 정상 동작

```
2025-12-03 15:30:00 [INFO] 💾 MemoryMonitor 시작 (interval: 60초, threshold: 80%)
2025-12-03 15:30:00 [INFO] 💾 메모리: 215.3 MB (Peak: 215.3 MB) | 시스템: 65.2%
2025-12-03 15:35:00 [INFO] 💾 메모리: 245.3 MB (Peak: 250.1 MB) | 시스템: 68.1%
2025-12-03 15:40:00 [INFO] 💾 메모리: 260.7 MB (Peak: 260.7 MB) | 시스템: 70.5%
```

### 경고 발생

```
2025-12-03 15:45:00 [WARNING] ⚠️ 시스템 메모리 부족! 85.3% > 80% (사용: 27.2 GB / 32.0 GB)
```

### 치명적 상황

```
2025-12-03 16:00:00 [CRITICAL] 🔴 프로세스 메모리 과다! 2150.3 MB > 2048 MB (Peak: 2150.3 MB)
```

---

## 설계 특징

### 1. psutil 활용
- 정확한 프로세스 메모리 측정
- 시스템 전체 메모리 상태 확인

### 2. 다단계 경고
- **정상**: 5분마다 로그 (정보 제공)
- **경고**: 시스템 메모리 > 임계값
- **치명적**: 프로세스 메모리 > 2GB

### 3. 자동 Peak 추적
- 프로그램 실행 중 최대 메모리 자동 기록
- 메모리 누수 감지에 유용

---

## 메모리 누수 감지

### 시나리오: 메모리가 계속 증가

```
15:30:00 [INFO] 💾 메모리: 215.3 MB (Peak: 215.3 MB)
15:35:00 [INFO] 💾 메모리: 245.3 MB (Peak: 250.1 MB)
15:40:00 [INFO] 💾 메모리: 280.7 MB (Peak: 285.2 MB)
15:45:00 [INFO] 💾 메모리: 320.5 MB (Peak: 325.8 MB)
15:50:00 [INFO] 💾 메모리: 365.2 MB (Peak: 370.1 MB)
```

**분석:**
- 5분마다 약 40~50MB 증가
- 메모리 누수 의심
- `ImageRegistry`, `LruPixmapCache` 등 확인 필요

---

## 주의사항

### 1. psutil 필수
- psutil이 설치되지 않으면 동작하지 않음
- `pip install psutil` 필요

### 2. 임계값 설정
- 시스템에 따라 적절한 임계값 설정
- 일반적으로 70~80% 권장

### 3. 프로세스 메모리 2GB 제한
- 32비트 프로세스는 2GB 제한
- 64비트 프로세스는 제한 없지만 과다 사용 시 시스템 불안정

### 4. 로그 빈도
- 정상 로그는 5분마다 (과도한 로그 방지)
- 경고/치명적 로그는 매번

---

**작성일:** 2025-12-04  
**관련 모듈:** `heartbeat.py`, `crash_logger.py`



