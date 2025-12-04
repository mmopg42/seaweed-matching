# window_state_manager.py 문서

## 개요
윈도우 상태(위치, 크기)를 저장하고 복원하는 관리 클래스입니다. DPI 스케일링을 고려하며, fallback 메커니즘을 제공합니다.

**파일 경로**: `script/ui/utils/window_state_manager.py`  
**파일 크기**: 62 라인  
**총 클래스**: 1개 (`WindowStateManager`)  
**총 메서드**: 2개  
**작성일**: 2025-12-04

---

## 사용 목적

**문제**: 애플리케이션 재시작 시 윈도우 위치/크기가 초기화됨  
**해결**: 윈도우 상태를 설정 파일에 저장하고 복원

---

## 클래스: WindowStateManager

윈도우 상태 저장 및 복원 관리 클래스

### 메서드

#### `save_window_bounds(self, window, config_manager)`
- **설명**: 윈도우 위치/크기를 설정 파일에 저장
- **매개변수**:
  - `window`: QMainWindow 객체
  - `config_manager`: ConfigManager 인스턴스
- **저장 데이터**:
  - `geometry` (str): QByteArray의 hex 문자열 (DPI 스케일링 고려)
  - `x` (int): 윈도우 X 좌표 (fallback용)
  - `y` (int): 윈도우 Y 좌표 (fallback용)
  - `w` (int): 윈도우 너비 (fallback용)
  - `h` (int): 윈도우 높이 (fallback용)
- **동작**:
  1. 현재 설정 로드
  2. `window.saveGeometry()`로 QByteArray 획득
  3. QByteArray를 hex 문자열로 변환
  4. 추가로 x, y, w, h 값도 저장 (fallback용)
  5. 설정 파일에 저장

**저장 구조**:
```json
{
  "window": {
    "geometry": "01d9d0cb00030000...",
    "x": 100,
    "y": 50,
    "w": 1200,
    "h": 800
  }
}
```

---

#### `restore_window_bounds(self, window, config_manager)`
- **설명**: 설정 파일에서 윈도우 위치/크기를 복원
- **매개변수**:
  - `window`: QMainWindow 객체
  - `config_manager`: ConfigManager 인스턴스
- **동작**:
  1. 설정 파일 로드
  2. **Primary**: `geometry` hex 문자열로 복원 시도
     - QByteArray로 변환
     - `window.restoreGeometry()` 호출
     - 성공 여부 리턴값 확인
  3. **Fallback**: geometry 복원 실패 시 x, y, w, h 사용
     - `window.setGeometry(x, y, w, h)` 직접 호출
- **특징**:
  - **DPI 스케일링 대응**: `saveGeometry()`는 DPI를 자동으로 고려
  - **Fallback 메커니즘**: geometry 복원 실패 시 x, y, w, h로 복원
  - **예외 처리**: 모든 에러는 무시하고 기본값 사용

---

## 사용 예시 (MainWindow)

```python
from ui.utils.window_state_manager import WindowStateManager
from infrastructure.config_manager import ConfigManager

class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        
        # 설정 관리자
        self.config_manager = ConfigManager()
        
        # 윈도우 상태 관리자
        self.window_state_manager = WindowStateManager()
        
        # UI 초기화
        self.init_ui()
        
        # 윈도우 상태 복원
        self.window_state_manager.restore_window_bounds(
            self, 
            self.config_manager
        )
    
    def closeEvent(self, event):
        """윈도우 닫기 이벤트 처리"""
        # 윈도우 상태 저장
        self.window_state_manager.save_window_bounds(
            self, 
            self.config_manager
        )
        
        event.accept()
```

---

## DPI 스케일링 처리

### QByteArray를 사용하는 이유

**문제**: 단순한 x, y, w, h 값은 DPI 스케일링 정보를 포함하지 않음
- 4K 모니터(200% DPI)에서 저장
- Full HD 모니터(100% DPI)에서 복원
- 윈도우 크기가 2배로 나타남

**해결**: `saveGeometry()` / `restoreGeometry()` 사용
- Qt가 DPI 정보를 QByteArray에 포함
- 자동으로 DPI 스케일링 조정
- 다양한 모니터 환경에서 일관된 크기

### Fallback 메커니즘

**Primary (geometry)**:
```python
geo_ba = window.saveGeometry()
geo_hex = geo_ba.toHex().data().decode("ascii")
# 저장: "01d9d0cb00030000..."
```

**Fallback (x, y, w, h)**:
```python
geom = window.geometry()
x, y, w, h = geom.x(), geom.y(), geom.width(), geom.height()
# 저장: x=100, y=50, w=1200, h=800
```

**복원 우선순위**:
1. `restoreGeometry(QByteArray)` 시도 (DPI 고려)
2. 실패 시 `setGeometry(x, y, w, h)` 사용 (DPI 무시)

---

## 주의사항

1. **ConfigManager 필수**: 설정 파일 저장/로드용
2. **closeEvent에서 호출**: 윈도우 닫기 시 상태 저장
3. **__init__에서 호출**: UI 초기화 후 상태 복원
4. **예외 무시**: 복원 실패해도 프로그램은 정상 동작

---

## 상태 저장 시점

### 저장 (closeEvent)
- 윈도우 닫기 버튼 클릭
- Alt+F4 누름
- 애플리케이션 종료

### 복원 (__init__)
- 애플리케이션 시작
- UI 초기화 완료 후
- 이벤트 루프 시작 전

---

## Phase 5 추가 (2025-11-30)

### 이전
- 윈도우 상태 저장 없음
- 매번 기본 위치/크기로 시작

### 이후
- 윈도우 상태 자동 저장/복원
- 사용자 경험 개선
- DPI 스케일링 대응
