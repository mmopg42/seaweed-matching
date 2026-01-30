---
name: logs:get
description: "모든 LogPanel 메시지를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 로그 가져오기 (logs:get)

## 설명
LogPanel에서 모든 로그 메시지를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe logs get $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "LogPanel",
    "count": 42,
    "logs": [
      {"Severity": "Info", "Time": "12:34:56", "Source": "FileWatcher", "Message": "..."},
      ...
    ]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/logs:get
/logs:get --json
```
