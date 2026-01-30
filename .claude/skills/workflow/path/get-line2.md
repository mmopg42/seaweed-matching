---
name: workflow:path:get-line2
description: "Line 2 경로를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# Line 2 경로 가져오기 (workflow:path:get-line2)

## 설명
WorkflowPanel에서 Line 2 경로를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe workflow path get-line2 $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "line": "line2",
    "count": 3,
    "paths": {"sampleName": "/path", "moveNir": "/path", "moveAllData": "/path"}
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/workflow:path:get-line2
/workflow:path:get-line2 --json
```
