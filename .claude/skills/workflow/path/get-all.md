---
name: workflow:path:get-all
description: "모든 워크플로우 경로를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 모든 경로 가져오기 (workflow:path:get-all)

## 설명
모든 워크플로우 경로를 가져옵니다 (Line 1 및 Line 2).

## CLI 명령
```bash
ui_automation.exe workflow path get-all $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "count": 6,
    "paths": {
      "line1": {"sampleName": "/path1", ...},
      "line2": {"sampleName": "/path2", ...}
    }
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/workflow:path:get-all
/workflow:path:get-all --json
```
