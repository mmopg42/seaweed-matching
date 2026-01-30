---
name: settings-dialog:path:get-quarantine
description: "검역 경로를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 검역 경로 가져오기 (settings-dialog:path:get-quarantine)

## 설명
검역 경로를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog path get-quarantine $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "pathType": "quarantine",
    "path": "/quarantine/path"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:path:get-quarantine
/settings-dialog:path:get-quarantine --json
```
