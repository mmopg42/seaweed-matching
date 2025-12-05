# nir_status_monitor.py 문서

## 개요
NIR 모니터링 앱과 메인 모니터링 앱 간 상태 공유를 위한 IPC(Inter-Process Communication) 모듈입니다.

**파일 경로**: `script/infrastructure/nir_status_monitor.py`  
**파일 크기**: 137 라인  
**총 클래스**: 1개 (`NIRStatusManager`)  
**생성일**: 2025-12-03 (최근 구현)  
**의존성**: pathlib, json

---

## 핵심 기능

### 1. 파일 기반 IPC
- JSON 파일을 통한 프로세스 간 통신
- 임시 디렉토리에 상태 파일 저장
- 원자적 쓰기로 파일 손상 방지

### 2. 상태 정보 관리
- NIR 앱 실행 여부
- 감시 폴더 및 이동 폴더 경로
- 마지막 처리 파일 및 처리 개수
- 타임스탬프 (최신성 확인)

### 3. Stale 감지
- 10초 이상 업데이트가 없으면 "오래됨" 판정
- NIR 앱 크래시 감지

---

## 클래스

### `NIRStatusManager`

NIR 모니터링 상태 관리자

#### 초기화

```python
NIRStatusManager(status_file_path: str = None)
```

**매개변수:**
- `status_file_path` (str, 선택): 상태 파일 경로
  - 기본값: `{임시디렉토리}/seaweed_nir_monitor_status.json`

**동작:**
- 상태 파일 경로 설정
- 경로를 콘솔에 출력

---

### 메서드

#### `write_status(is_running, monitor_path="", move_path="", last_file="", file_count=0)`

NIR 모니터링 상태 기록 (NIR 앱에서 호출)

**매개변수:**
- `is_running` (bool): 모니터링 실행 여부
- `monitor_path` (str): 감시 폴더 경로
- `move_path` (str): 이동 폴더 경로
- `last_file` (str): 마지막 처리 파일명
- `file_count` (int): 처리된 파일 개수

**동작:**
1. 상태 딕셔너리 생성 (타임스탬프 포함)
2. 임시 파일에 JSON 쓰기
3. 임시 파일을 원본으로 이동 (원자적 연산)
4. 성공/실패 로그 출력

**JSON 구조:**
```json
{
  "is_running": true,
  "monitor_path": "E:/data/nir",
  "move_path": "E:/data/nir/processed",
  "last_file": "spectrum_001.txt",
  "file_count": 25,
  "last_updated": "2025-12-03T15:30:00.123456",
  "timestamp": 1701601800.123456
}
```

---

#### `read_status()`

NIR 모니터링 상태 읽기 (메인 모니터링 앱에서 호출)

**반환값:**
- `dict`: 상태 딕셔너리

**동작:**
1. 상태 파일 존재 확인
2. JSON 파싱
3. 타임스탬프 확인 (10초 이상 경과 시 `stale=True` 추가)
4. 실패 시 기본 상태 반환

**Stale 감지:**
```python
# 10초 이상 업데이트가 없으면
if time_diff > 10.0:
    status['is_running'] = False
    status['stale'] = True
```

---

#### `clear_status()`

상태 파일 삭제

**동작:**
- 상태 파일이 존재하면 삭제
- 성공/실패 로그 출력

**사용 시점:**
- NIR 앱 종료 시

---

#### `_default_status()`

기본 상태 딕셔너리 반환 (내부 메서드)

**반환값:**
```python
{
    "is_running": False,
    "monitor_path": "",
    "move_path": "",
    "last_file": "",
    "file_count": 0,
    "last_updated": None,
    "timestamp": 0
}
```

---

## 사용 예시

### NIR 앱에서 상태 쓰기

```python
from infrastructure.nir_status_monitor import NIRStatusManager

class NIRMonitorApp:
    def __init__(self):
        self.status_manager = NIRStatusManager()
        
        # 주기적 상태 업데이트 타이머 (2초마다)
        self.status_update_timer = QTimer(self)
        self.status_update_timer.timeout.connect(self.update_status_file)
        self.status_update_timer.setInterval(2000)
    
    def start_monitoring(self):
        # 모니터링 시작
        self.is_monitoring = True
        
        # 상태 기록
        self.status_manager.write_status(
            is_running=True,
            monitor_path=self.monitor_path,
            move_path=self.move_path,
            last_file="",
            file_count=0
        )
        
        # 주기적 업데이트 시작
        self.status_update_timer.start()
    
    def update_status_file(self):
        """2초마다 호출"""
        if self.is_monitoring:
            self.status_manager.write_status(
                is_running=True,
                monitor_path=self.monitor_path,
                move_path=self.move_path,
                last_file=self.last_processed_file,
                file_count=self.processed_file_count
            )
    
    def on_monitoring_stopped(self):
        # 모니터링 중지
        self.is_monitoring = False
        self.status_update_timer.stop()
        
        # 상태 업데이트
        self.status_manager.write_status(is_running=False)
    
    def closeEvent(self, event):
        # 종료 시 상태 파일 삭제
        self.status_update_timer.stop()
        self.status_manager.clear_status()
        event.accept()
```

### 메인 모니터링 앱에서 상태 읽기

```python
from infrastructure.nir_status_monitor import NIRStatusManager

class NIRStatusWidget(QFrame):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.status_manager = NIRStatusManager()
        
        # 2초마다 상태 확인
        self.timer = QTimer(self)
        self.timer.timeout.connect(self.update_status)
        self.timer.start(2000)
    
    def update_status(self):
        status = self.status_manager.read_status()
        
        is_running = status.get('is_running', False)
        monitor_path = status.get('monitor_path', "")
        file_count = status.get('file_count', 0)
        
        if is_running:
            self.status_label.setText("✅ 실행 중")
            self.detail_label.setText(
                f"감시: {monitor_path} | 처리: {file_count}개"
            )
        else:
            self.status_label.setText("⚪ 중지됨")
            self.detail_label.setText("")
```

---

## 설계 특징

### 1. 원자적 쓰기
```python
# 임시 파일에 쓰기
temp_file = self.status_file.with_suffix('.tmp')
with open(temp_file, 'w') as f:
    json.dump(status, f)

# 원자적 이동 (파일 손상 방지)
temp_file.replace(self.status_file)
```

### 2. Stale 감지
- 10초 이상 업데이트가 없으면 자동으로 `is_running=False` 처리
- NIR 앱 크래시를 메인 앱에서 자동 감지

### 3. 주기적 업데이트
- NIR 앱에서 2초마다 상태 파일 업데이트
- 파일 처리 없어도 "살아있음" 표시

---

## 상태 파일 위치

### Windows
```
C:\Users\{사용자}\AppData\Local\Temp\seaweed_nir_monitor_status.json
```

### Linux/macOS
```
/tmp/seaweed_nir_monitor_status.json
```

---

## 주의사항

### 1. 파일 권한
- 임시 디렉토리 쓰기 권한 필요
- 두 앱이 동일한 사용자로 실행되어야 함

### 2. 타임스탬프 정확도
- 시스템 시간이 정확해야 함
- 10초 제한은 `read_status()`에서 변경 가능

### 3. 파일 잠금
- 원자적 쓰기로 파일 잠금 문제 최소화
- Windows에서도 안전

### 4. JSON 파싱 실패
- 손상된 파일은 기본 상태 반환
- 에러 로그 출력

---

## 확장 가능성

### 더 많은 상태 정보 추가

```python
status = {
    # 기존 필드
    "is_running": is_running,
    "monitor_path": monitor_path,
    # ...
    
    # 추가 필드
    "error_count": error_count,
    "last_error": last_error_message,
    "uptime_seconds": uptime,
}
```

---

**작성일:** 2025-12-04  
**관련 모듈:** `nir_app.py`, `nir_status_widget.py`  
**관련 문서:** Phase 7~8에서 추가된 IPC 메커니즘



