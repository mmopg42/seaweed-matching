---
name: setup:camera-states
description: "카메라 버튼 상태를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 카메라 상태 (setup:camera-states)

## 설명
SetupWindow에서 카메라 버튼 상태를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe setup camera-states $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "general": true,
    "nir1": false,
    "nir2": false
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/setup:camera-states
/setup:camera-states --json
```
