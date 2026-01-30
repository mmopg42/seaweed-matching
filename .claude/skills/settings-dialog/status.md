---
name: settings-dialog:status
description: "대화상자 열림 상태를 확인합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 설정 대화상자 상태 (settings-dialog:status)

## 설명
SettingsDialog가 현재 열려 있는지 확인합니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog status $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "dialogType": "SettingsDialog",
    "isOpen": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:status
/settings-dialog:status --json
```
