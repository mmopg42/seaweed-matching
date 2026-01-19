# ChronoView Logging Guide

## 로그 시스템 개요

ChronoView는 두 가지 종류의 로그를 사용합니다.

| 로그 타입 | 대상 | 용도 | 위치 |
|-----------|------|------|------|
| **UI 패널 로그** | 사용자 | 사용자에게 필요한 운영 정보 | ChronoView 내부 LogPanel |
| **콘솔 로그** | 개발자 | 디버깅 및 개발 정보 | `%APPDATA%\ChronoView\Logs\` |

---

## 1. UI 패널 로그 (LogPanel)

### 목적
- **사용자**에게 필요한 정보 제공
- 앱 운영 중 발생하는 이벤트, 오류, 경고 표시
- 사용자가 직접 확인하고 대응할 수 있는 정보

### 표시 형식
```
[심각도] [시간] [소스] [메시지]
```

### 예시
```
[INFO] [14:30:25] [FileWatcher] 새 파일 감지: sample_001.png
[WARN] [14:31:10] [FileMatcher] 타임스탬프 불일치: ±3초 초과
[ERROR] [14:32:00] [CameraLauncher] NIR 카메라 시작 실패
```

### CLI 접근
```bash
# 모든 로그 가져오기
ui_automation logs get

# 최근 10개 로그
ui_automation logs tail 10

# ERROR만 필터링
ui_automation logs filter --level ERROR

# 텍스트 검색
ui_automation logs search "카메라"
```

---

## 2. 콘솔 로그 (Debug Console)

### 목적
- **개발자**를 위한 디버깅 정보
- 상세 스택 트레이스, 내부 상태, 개발용 메시지
- UI Automation 테스트 및 문제 해결

### 로그 레벨 (Microsoft.Extensions.Logging)
- `Trace` → 가장 상세한 개발 정보
- `Debug` → 디버깅용 정보
- `Information` → 일반 informational 메시지
- `Warning` → 비정상 상태 but 복구 가능
- `Error` → 오류 발생 but 앱 계속 실행
- `Critical` → 심각한 오류, 앱 종료 가능

### 파일 위치
```
%APPDATA%\ChronoView\Logs\{YYYYMMDD}\ChronoView_Debug_{YYYYMMDD}_{HHmmss}.log
예: C:\Users\{User}\AppData\Local\prische\ChronoView\Logs\20260118\ChronoView_Debug_20260118_161207.log
```

### 예시
```
info: ChronoView.App[0]
      ChronoView application starting...
info: ChronoView.App[0]
      Creating MainWindow from DI container...
dbug: ChronoView.UI.ViewModels.MainWindowViewModel[100]
      Initializing FileGroups ObservableCollection
warn: ChronoView.Services.FileWatcherService[10]
      File change detected but matching timeout: file_001.dat
fail: ChronoView.Services.NirCameraLauncher[50]
      Failed to launch NIR camera: Port already in use
```

### CLI 접근 (현재 미구현)
```bash
# TODO: 콘솔 로그 읽기 CLI 명령 추가 필요
ui_automation console-logs tail
ui_automation console-logs search "Exception"
```

---

## 3. UI Automation에서의 활용

### 디버깅 시나리오

| 상황 | 확인 방법 | CLI 명령 |
|------|-----------|----------|
| 모니터링 시작 안 됨 | LogPanel 확인 | `logs search "시작"` |
| 파일 매칭 실패 | 콘솔 로그 확인 | 로그 파일 직접 확인 |
| 카메라 연결 문제 | LogPanel ERROR만 보고 | `logs filter --level ERROR` |
| UI 클릭 반응 없음 | 콘솔 로그에서 이벤트 확인 | 로그 파일에서 "Click" 검색 |

### 우선순위
1. **사용자 문제** → LogPanel 먼저 확인 (사용자 친화적 정보)
2. **개발자 문제** → 콘솔 로그 확인 (기술적 디테일)

---

## 4. 향후 개선 사항

### 콘솔 로그 CLI 지원
- [ ] `console-logs tail <n>` - 최근 콘솔 로그 n줄
- [ ] `console-logs search <text>` - 텍스트 검색
- [ ] `console-logs follow` - 실시간 모니터링 (tail -f)
- [ ] `console-logs export` - 로그 파일 추출

### LogPanel 개선
- [ ] 로그 레벨별 색상 구분
- [ ] 로그 필터 UI
- [ ] 로그 export 기능

---

*작성일: 2026-01-18*
*목적: UI Automation 테스트 및 디버깅 워크플로우 정의*
