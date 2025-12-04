# main.py 문서

## 개요
통합 모니터링 컨트롤러로, 메인 모니터링과 NIR 모니터링을 별도 창으로 실행 및 관리합니다.

**파일 크기**: 8KB (231 라인)  
**총 클래스/함수**: 16개

---

## 주요 클래스

### MonitorController
메인 모니터링과 NIR 모니터링을 제어하는 컨트롤러 윈도우

#### 속성
- `main_window`: 메인 모니터링 창 (monitoring_app.MainWindow)
- `nir_window`: NIR 모니터링 창 (nir_app.NirApp)
- `main_button`: 메인 모니터링 시작 버튼
- `nir_button`: NIR 모니터링 시작 버튼

#### 메서드

##### 초기화
```python
__init__(self)
```
- 윈도우 제목 설정: "AI 데이터 퓨전 및 통합 관제 솔루션 (Model 이비기술-MMS ver0.5.12)"
- 창 크기: 600x300
- 중앙 정렬
- UI 초기화

##### UI 구성
```python
init_ui(self)
```
메인 레이아웃 구성:
- 제목 라벨
- 제어 패널 (버튼 그룹)

```python
create_control_panel(self)
```
제어 패널 생성:
- 메인 모니터링 그룹
  - **시작 버튼**: `start_main_monitoring()` 호출
  - **중지 버튼**: `stop_main_monitoring()` 호출
  - 상태 라벨
- NIR 모니터링 그룹
  - **시작 버튼**: `start_nir_monitoring()` 호출
  - **중지 버튼**: `stop_nir_monitoring()` 호출
  - 상태 라벨

##### 메인 모니터링 제어
```python
start_main_monitoring(self)
```
- 이미 실행 중이면 창 활성화
- 새 MainWindow 인스턴스 생성
- 창이 닫힐 때 `on_main_closed()` 호출되도록 설정
- 버튼 상태 업데이트 (시작 비활성화, 중지 활성화)
- 창 표시
- 상태 라벨: "실행 중..."

```python
stop_main_monitoring(self)
```
- 창이 열려있으면 닫기
- 버튼 상태 복원

```python
on_main_closed(self)
```
- 창 닫힘 이벤트 처리
- 버튼 상태 복원
- 상태 라벨: "중지됨"

##### NIR 모니터링 제어
```python
start_nir_monitoring(self)
```
- 이미 실행 중이면 창 활성화
- 새 NirApp 인스턴스 생성
- 창이 닫힐 때 `on_nir_closed()` 호출되도록 설정
- 버튼 상태 업데이트
- 창 표시
- 상태 라벨: "실행 중..."

```python
stop_nir_monitoring(self)
```
- 창이 열려있으면 닫기
- 버튼 상태 복원

```python
on_nir_closed(self)
```
- 창 닫힘 이벤트 처리
- 버튼 상태 복원
- 상태 라벨: "중지됨"

##### 종료 처리
```python
closeEvent(self, event)
```
컨트롤러 종료 시:
- 메인 모니터링 창 정리
- NIR 모니터링 창 정리
- 이벤트 수락

---

## 실행 흐름

### 1. 프로그램 시작
```
main()
  → QApplication 생성
  → MonitorController 인스턴스 생성
  → 창 표시
  → 이벤트 루프 시작
```

### 2. 메인 모니터링 시작
```
사용자 [메인 시작 버튼 클릭]
  → start_main_monitoring()
  → MainWindow 생성 및 표시
  → 버튼 상태 업데이트
  → custom_close 이벤트 핸들러 등록
```

### 3. NIR 모니터링 시작
```
사용자 [NIR 시작 버튼 클릭]
  → start_nir_monitoring()
  → NirApp 생성 및 표시
  → 버튼 상태 업데이트
  → custom_close 이벤트 핸들러 등록
```

### 4. 창 종료
```
사용자 [창 닫기]
  → custom_close 이벤트
  → on_main_closed() 또는 on_nir_closed()
  → 버튼 상태 복원
  → 상태 라벨 업데이트
```

### 5. 컨트롤러 종료
```
사용자 [컨트롤러 창 닫기]
  → closeEvent()
  → 메인 창 정리
  → NIR 창 정리
  → 프로그램 종료
```

---

## UI 레이아웃

```
┌─────────────────────────────────────────┐
│  AI 데이터 퓨전 및 통합 관제 솔루션      │
│         (Model 이비기술-MMS)             │
├─────────────────────────────────────────┤
│  ┌─ 메인 모니터링 ─────────────────┐    │
│  │  [시작]  [중지]                   │    │
│  │  상태: 중지됨                     │    │
│  └───────────────────────────────────┘    │
│                                           │
│  ┌─ NIR 모니터링 ──────────────────┐    │
│  │  [시작]  [중지]                   │    │
│  │  상태: 중지됨                     │    │
│  └───────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

---

## 특징

1. **독립적인 창 관리**: 메인과 NIR을 별도 창으로 실행
2. **상태 관리**: 각 모니터링의 실행 상태를 버튼과 라벨로 표시
3. **중복 실행 방지**: 이미 실행 중이면 기존 창 활성화
4. **안전한 종료**: 컨트롤러 종료 시 모든 하위 창 정리
5. **EXE 호환**: 실행 파일 환경에서도 정상 작동

---

## 모듈 간 관계

```
MonitorController
    ├─ monitoring_app.MainWindow (메인 모니터링)
    │   ├─ 파일 매칭
    │   ├─ 이미지 프리뷰
    │   └─ 파일 이동/삭제
    │
    └─ nir_app.NirApp (NIR 모니터링)
        └─ NIR 스펙트럼 모니터링
```

---

## 의존성
- `PySide6.QtWidgets`: Qt 위젯
- `PySide6.QtCore`: Qt 코어 기능
- `monitoring_app.MainWindow`: 메인 모니터링 애플리케이션
- `nir_app.NirApp`: NIR 모니터링 애플리케이션

---

## 버전 정보
- **버전**: v0.5.12
- **제품명**: AI 데이터 퓨전 및 통합 관제 솔루션
- **모델**: 이비기술-MMS

---

## 사용 방법

### 직접 실행
```bash
python main.py
```

### EXE 실행
빌드된 실행 파일을 더블클릭하여 실행

### 프로그램 사용
1. 컨트롤러 창이 열림
2. "메인 시작" 버튼 클릭 → 메인 모니터링 창 열림
3. "NIR 시작" 버튼 클릭 → NIR 모니터링 창 열림
4. 각 창에서 독립적으로 작업 수행
5. 종료 시 모든 창 닫기
