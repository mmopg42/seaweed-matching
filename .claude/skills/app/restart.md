---
name: app:restart
description: "ChronoView를 재시작합니다 (중지 + 시작)."
argument-hint: [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 앱 재시작 (app:restart)

## 설명
기존 ChronoView 프로세스를 중지하고 새 인스턴스를 시작합니다.

## CLI 명령
```bash
ui_automation.exe app restart $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "restarted": true,
    "processesStopped": 1,
    "newProcessId": 12345
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/app:restart
/app:restart --json
```
