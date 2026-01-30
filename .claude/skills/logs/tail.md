---
name: logs:tail
description: "최근 N개 로그 메시지를 가져옵니다."
argument-hint: [count] [--json]
allowed-tools: Bash
---

# 로그 꼬리부분 (logs:tail)

## 설명
LogPanel에서 최근 N개 로그 메시지를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe logs tail $ARGUMENTS
```

## 인수
- `count`: 메시지 수 (기본값: 10)
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "LogPanel",
    "requested": 20,
    "returned": 20,
    "logs": [...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/logs:tail 50
/logs:tail 20 --json
```
