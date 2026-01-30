---
name: app:launch
description: "ChronoView 애플리케이션을 시작합니다."
argument-hint: [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 앱 시작 (app:launch)

## 설명
ChronoView 애플리케이션을 dotnet run으로 시작합니다. 프로세스 시작 후 즉시 반환됩니다.

## CLI 명령
```bash
ui_automation.exe app launch $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "launched": true,
    "processId": 12345
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/app:launch
/app:launch --json
```
