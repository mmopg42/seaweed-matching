---
name: windows:all
description: "모든 ChronoView 윈도우를 나열합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 모든 윈도우 (windows:all)

## 설명
모든 ChronoView 윈도우를 나열합니다.

## CLI 명령
```bash
ui_automation.exe windows all $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "count": 2,
    "windows": [
      {"title": "ChronoView Pro", "className": "Window", "automationId": "MainWindow"},
      {"title": "Settings", "className": "Window", "automationId": "SettingsDialog"}
    ]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/windows:all
/windows:all --json
```
