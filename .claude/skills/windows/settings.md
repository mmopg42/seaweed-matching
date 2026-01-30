---
name: windows:settings
description: "SettingsDialog를 찾습니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 설정 대화상자 (windows:settings)

## 설명
SettingsDialog를 찾습니다.

## CLI 명령
```bash
ui_automation.exe windows settings $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "found": true,
    "windowType": "SettingsDialog",
    "title": "Settings",
    "className": "Window",
    "automationId": "SettingsDialog"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/windows:settings
/windows:settings --json
```
