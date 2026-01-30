---
name: settings-dialog:path:get-line2
description: "Line 2 경로를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# Line 2 경로 가져오기 (settings-dialog:path:get-line2)

## 설명
Line 2 경로만 가져옵니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog path get-line2 $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "line": "line2",
    "count": 5,
    "paths": {"nir2": "/path", "normal2": "/path"}
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:path:get-line2
/settings-dialog:path:get-line2 --json
```
