---
name: toolbar:list
description: "모든 툴바 버튼을 나열합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 툴바 목록 (toolbar:list)

## 설명
사용 가능한 모든 툴바 버튼을 나열합니다.

## CLI 명령
```bash
ui_automation.exe toolbar list $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "count": 6,
    "buttons": ["Start", "Stop", "Settings", "Refresh", "Move", "Delete"]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/toolbar:list
/toolbar:list --json
```
