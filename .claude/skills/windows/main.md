---
name: windows:main
description: "MainWindow를 찾습니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 메인 윈도우 (windows:main)

## 설명
ChronoView MainWindow를 찾습니다.

## CLI 명령
```bash
ui_automation.exe windows main $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "found": true,
    "windowType": "MainWindow",
    "title": "ChronoView Pro",
    "className": "Window",
    "automationId": "MainWindow"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/windows:main
/windows:main --json
```
