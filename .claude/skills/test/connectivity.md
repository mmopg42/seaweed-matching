---
name: test:connectivity
description: "ChronoView 연결을 확인합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 연결 확인 (test:connectivity)

## 설명
ChronoView가 실행 중이고 접근 가능한지 확인합니다.

## CLI 명령
```bash
ui_automation.exe test connectivity $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "connected": true,
    "windowFound": true,
    "appName": "ChronoView Pro",
    "timestamp": "2026-01-27T12:34:56.789Z"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/test:connectivity
/test:connectivity --json
```
