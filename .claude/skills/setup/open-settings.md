---
name: setup:open-settings
description: "SetupWindow에서 SettingsDialog를 엽니다."
argument-hint: [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 설정 열기 (setup:open-settings)

## 설명
SetupWindow에서 설정 버튼을 클릭하여 SettingsDialog를 엽니다.

## CLI 명령
```bash
ui_automation.exe setup open-settings $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "settingsOpened": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/setup:open-settings
/setup:open-settings --json
```
