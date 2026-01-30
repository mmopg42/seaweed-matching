---
name: settings-dialog:path:get-all
description: "모든 구성된 경로를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 모든 경로 가져오기 (settings-dialog:path:get-all)

## 설명
모든 구성된 경로를 가져옵니다 (Line 1, Line 2, Output, Quarantine).

## CLI 명령
```bash
ui_automation.exe settings-dialog path get-all $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "line1": {"nir1": "/path", "normal1": "/path"},
    "line2": {"nir2": "/path", "normal2": "/path"},
    "output": "/output/path",
    "quarantine": "/quarantine/path"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:path:get-all
/settings-dialog:path:get-all --json
```
