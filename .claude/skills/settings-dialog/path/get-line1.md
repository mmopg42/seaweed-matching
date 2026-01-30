---
name: settings-dialog:path:get-line1
description: "Line 1 경로를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# Line 1 경로 가져오기 (settings-dialog:path:get-line1)

## 설명
Line 1 경로만 가져옵니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog path get-line1 $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "line": "line1",
    "count": 5,
    "paths": {"nir1": "/path", "normal1": "/path"}
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:path:get-line1
/settings-dialog:path:get-line1 --json
```
