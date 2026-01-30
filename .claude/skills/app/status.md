---
name: app:status
description: "ChronoView 실행 상태를 확인합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 앱 상태 (app:status)

## 설명
ChronoView가 실행 중인지 확인하고 프로세스 정보를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe app status $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "isRunning": true,
    "processCount": 1,
    "processIds": [12345],
    "mainWindowTitles": ["ChronoView Pro"]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/app:status
/app:status --json
```
