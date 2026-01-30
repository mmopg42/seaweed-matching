---
name: windows:setup-complete
description: "SetupWindow를 완료하고 MainWindow로 이동합니다."
argument-hint: [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 설정 완료 (windows:setup-complete)

## 설명
SetupWindow를 완료하고 MainWindow로 이동합니다 (시작 버튼 클릭 + MainWindow 대기).

## CLI 명령
```bash
ui_automation.exe windows setup-complete $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "completed": true,
    "mainWindowFound": true,
    "mainWindowTitle": "ChronoView Pro"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/windows:setup-complete
/windows:setup-complete --json
```
