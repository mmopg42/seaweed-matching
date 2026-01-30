---
name: test:capabilities
description: "사용 가능한 자동화 기능을 나열합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 기능 확인 (test:capabilities)

## 설명
사용 가능한 자동화 기능을 나열합니다 (윈도우, 컨트롤러, 명령).

## CLI 명령
```bash
ui_automation.exe test capabilities $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "windows": [{"type": "MainWindow", "accessible": true}],
    "controllers": ["MainWindowController", "SettingsDialogController"],
    "commands": ["app launch", "toolbar start", ...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/test:capabilities
/test:capabilities --json
```
