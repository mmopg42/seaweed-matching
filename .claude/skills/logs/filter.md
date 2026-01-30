---
name: logs:filter
description: "심각도 수준으로 로그를 필터링합니다."
argument-hint: [--level <level>] [--json]
allowed-tools: Bash
---

# 로그 필터 (logs:filter)

## 설명
심각도 수준으로 로그 메시지를 필터링합니다.

## CLI 명령
```bash
ui_automation.exe logs filter $ARGUMENTS
```

## 인수
- `--level <level>`: 심각도 수준: Debug, Info, Warning, Error (null = 전체)
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "LogPanel",
    "filter": {"level": "Error"},
    "count": 5,
    "logs": [...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/logs:filter
/logs:filter --level Error
/logs:filter --level Warning --json
```
