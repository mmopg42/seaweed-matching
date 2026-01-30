---
name: workflow:camera-states
description: "모든 카메라 상태를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 카메라 상태 (workflow:camera-states)

## 설명
WorkflowPanel에서 모든 카메라 버튼 상태를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe workflow camera-states $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "WorkflowPanel",
    "count": 3,
    "states": {"General": true, "Nir1": false, "Nir2": false}
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/workflow:camera-states
/workflow:camera-states --json
```
