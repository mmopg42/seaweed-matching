# nir_status_widget.py 문서

## 개요
메인 모니터링 앱에서 NIR 모니터링 앱의 상태를 표시하는 UI 위젯입니다.

**파일 경로**: `script/ui/components/nir_status_widget.py`  
**파일 크기**: 133 라인  
**총 클래스**: 1개 (`NIRStatusWidget`)  
**생성일**: 2025-12-03 (최근 구현)  
**의존성**: PySide6, NIRStatusManager

---

## 핵심 기능

### 1. 실시간 상태 표시
- NIR 앱 실행 여부 (실행 중 / 중지됨)
- 감시 폴더 및 이동 폴더
- 처리된 파일 개수
- 마지막 처리 파일

### 2. 주기적 업데이트
- 2초마다 자동 업데이트 (기본값)
- QTimer 기반 자동 갱신

### 3. 시각적 인디케이터
- ● 녹색: 실행 중
- ● 회색: 중지됨
- 상태 텍스트 및 상세 정보 표시

---

## 클래스

### `NIRStatusWidget(QFrame)`

NIR 모니터링 상태 표시 위젯

#### 초기화

```python
NIRStatusWidget(parent=None, update_interval_ms: int = 2000)
```

**매개변수:**
- `parent` (QWidget, 선택): 부모 위젯 (일반적으로 `MainWindow`)
- `update_interval_ms` (int): 상태 업데이트 주기 (밀리초, 기본 2000)

**동작:**
1. `NIRStatusManager` 초기화
2. UI 초기화
3. 타이머 초기화 및 시작
4. 즉시 1회 상태 업데이트

**인스턴스 변수:**
- `status_manager`: NIRStatusManager 인스턴스
- `update_interval`: 업데이트 주기 (ms)
- `timer`: QTimer 인스턴스
- `status_indicator`: 상태 인디케이터 레이블 (●)
- `status_text`: 상태 텍스트 레이블
- `detail_label`: 상세 정보 레이블

---

### 메서드

#### `_init_ui()`

UI 초기화 (내부 메서드)

**구성 요소:**
1. **제목**: "📊 NIR 모니터링:"
2. **상태 인디케이터**: ● (녹색/회색)
3. **상태 텍스트**: "실행 중" / "중지됨" / "알 수 없음"
4. **구분선**: |
5. **상세 정보**: 폴더 경로 및 파일 개수

**스타일:**
- `QFrame`의 `ObjectName`을 "StatsBar"로 설정 (통계 바와 동일)
- 여백: 12px (좌/우), 8px (상/하)
- 간격: 12px

---

#### `_init_timer()`

타이머 초기화 (내부 메서드)

**동작:**
1. QTimer 생성
2. `update_status()` 연결
3. 타이머 시작 (update_interval_ms)
4. 즉시 1회 업데이트

---

#### `update_status()`

NIR 앱 상태 업데이트

**동작:**
1. `status_manager.read_status()` 호출
2. 상태 정보 추출:
   - `is_running`: 실행 여부
   - `monitor_path`: 감시 폴더
   - `move_path`: 이동 폴더
   - `last_file`: 마지막 파일
   - `file_count`: 처리 개수
3. UI 업데이트:
   - 실행 중: ● 녹색, "실행 중", 상세 정보 표시
   - 중지: ● 회색, "중지됨", 상세 정보 숨김

**UI 업데이트 예시:**

**실행 중:**
```
📊 NIR 모니터링: ● 실행 중 | 감시: nir → 이동: processed | 처리 파일: 25개 (최근: spectrum_001.txt)
```

**중지됨:**
```
📊 NIR 모니터링: ● 중지됨 | 경로: - | 처리 파일: 0개
```

---

#### `stop_timer()`

타이머 중지

**동작:**
- 타이머가 활성 상태이면 중지
- 로그: "[NIRStatusWidget] 타이머 중지됨"

**사용 시점:**
- 메인 앱 종료 시

---

#### `start_timer()`

타이머 시작

**동작:**
- 타이머가 비활성 상태이면 시작
- 로그: "[NIRStatusWidget] 타이머 시작됨"

---

## 사용 예시

### monitoring_app.py에서의 사용

```python
from ui.components.nir_status_widget import NIRStatusWidget

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # ... UI 초기화 ...
        
        # NIR 상태 위젯 초기화 (2초마다 업데이트)
        self.nir_status_widget = NIRStatusWidget(
            self,
            update_interval_ms=2000
        )
        
        # 레이아웃에 추가 (파일개수현황과 버튼 사이)
        main_layout.addWidget(self.nir_status_widget)
    
    def closeEvent(self, event):
        # 종료 시 타이머 정리
        if hasattr(self, 'nir_status_widget'):
            self.nir_status_widget.stop_timer()
        
        event.accept()
```

### 커스텀 업데이트 간격

```python
# 더 빠른 업데이트 (1초마다)
nir_widget = NIRStatusWidget(self, update_interval_ms=1000)

# 더 느린 업데이트 (5초마다)
nir_widget = NIRStatusWidget(self, update_interval_ms=5000)
```

---

## UI 배치

### monitoring_app.py에서의 위치

```
┌────────────────────────────────────────────┐
│  📊 파일 개수 현황                         │ ← stats_container
├────────────────────────────────────────────┤
│  📊 NIR 모니터링: ● 실행 중 | ...        │ ← nir_status_widget
├────────────────────────────────────────────┤
│  [감시 시작]  [중지]  [설정]  ...        │ ← buttons
└────────────────────────────────────────────┘
```

---

## 설계 특징

### 1. IPC 기반
- `NIRStatusManager`를 통한 프로세스 간 통신
- 파일 기반 상태 공유

### 2. 자동 갱신
- QTimer로 주기적 업데이트
- NIR 앱 실행 여부 자동 감지

### 3. Stale 감지
- 10초 이상 업데이트가 없으면 자동으로 "중지됨" 표시
- NIR 앱 크래시 자동 감지

### 4. 통계 바 스타일
- 기존 통계 바와 동일한 스타일
- 일관된 UI/UX

---

## 상태 전환 다이어그램

```
초기 상태 (회색, "알 수 없음")
    │
    ↓ (NIR 앱 시작)
실행 중 (녹색, "실행 중")
    │
    ├─ 정상 업데이트 (2초마다)
    │
    ├─ (NIR 앱 중지 버튼)
    ↓
중지됨 (회색, "중지됨")
    │
    ├─ (10초 이상 업데이트 없음)
    ↓
중지됨 (회색, "중지됨", stale)
```

---

## 주의사항

### 1. 타이머 정리
- 메인 앱 종료 시 `stop_timer()` 호출 필수
- 메모리 누수 방지

### 2. 업데이트 간격
- 너무 짧은 간격 (< 1초)은 부하 증가
- 권장: 1~3초

### 3. NIRStatusManager 의존성
- NIR 앱이 상태 파일을 쓰지 않으면 "알 수 없음" 표시
- NIR 앱이 정상 실행 중이어야 정확한 상태 표시

### 4. Stale 감지
- 10초 제한은 `NIRStatusManager`에서 설정
- 필요 시 조정 가능

---

## GUI 체크 주기와 부하

### 2초마다 체크 시 부하

```python
# 1회 체크 동작:
# 1. JSON 파일 읽기 (< 1ms)
# 2. 파싱 (< 1ms)
# 3. UI 업데이트 (< 1ms)
# 총: < 3ms

# 2초마다 체크 시:
# - CPU 사용률 증가: < 0.1%
# - 메모리 증가: 무시 가능
# - GUI 부하: 없음 (매우 경량)
```

**결론**: 2초 간격은 GUI에 무리 없음

---

## 확장 가능성

### 더 많은 정보 표시

```python
def update_status(self):
    status = self.status_manager.read_status()
    
    # 추가 정보
    error_count = status.get('error_count', 0)
    uptime = status.get('uptime_seconds', 0)
    
    # UI 업데이트
    if error_count > 0:
        self.error_label.setText(f"⚠️ 에러: {error_count}회")
```

---

**작성일:** 2025-12-04  
**관련 모듈:** `nir_status_monitor.py`, `nir_app.py`, `monitoring_app.py`  
**관련 Phase:** Phase 7~8 (NIR 상태 표시 기능 추가)


