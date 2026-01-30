---
name: windows:setup
description: "SetupWindow를 찾습니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 설정 윈도우 (windows:setup)

## 설명
SetupWindow를 찾습니다.

## CLI 명령
```bash
ui_automation.exe windows setup $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "found": true,
    "windowType": "SetupWindow",
    "title": "ChronoView Setup",
    "className": "Window",
    "automationId": "SetupWindow"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/windows:setup
/windows:setup --json
```
