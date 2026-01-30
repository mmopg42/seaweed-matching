---
name: logs:search
description: "로그 메시지에서 텍스트를 검색합니다."
argument-hint: <text> [--json]
allowed-tools: Bash
---

# 로그 검색 (logs:search)

## 설명
로그 메시지에서 특정 텍스트를 검색합니다.

## CLI 명령
```bash
ui_automation.exe logs search $ARGUMENTS
```

## 인수
- `text`: 검색 텍스트 (필수)
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "LogPanel",
    "search": "ERROR",
    "count": 3,
    "logs": [...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/logs:search ERROR
/logs:search "FileNotFound" --json
```
