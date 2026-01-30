---
name: settings-dialog:path:get-output
description: "출력 경로를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 출력 경로 가져오기 (settings-dialog:path:get-output)

## 설명
출력 경로를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog path get-output $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "pathType": "output",
    "path": "/output/path"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:path:get-output
/settings-dialog:path:get-output --json
```
