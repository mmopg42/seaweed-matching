---
name: app:stop
description: "모든 ChronoView 프로세스를 중지합니다."
argument-hint: [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 앱 중지 (app:stop)

## 설명
실행 중인 모든 ChronoView 프로세스를 중지합니다. 종료된 프로세스 수를 반환합니다.

## CLI 명령
```bash
ui_automation.exe app stop $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "stopped": true,
    "processesStopped": 1
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/app:stop
/app:stop --json
```
