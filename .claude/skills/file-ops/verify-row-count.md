---
name: file-ops:verify-row-count
description: "행 수가 변경되었는지 확인합니다."
argument-hint: <originalCount> [--timeout <ms>] [--json]
allowed-tools: Bash
---

# 행 수 확인 (file-ops:verify-row-count)

## 설명
행 수가 원래 값에서 변경되었는지 대기하고 확인합니다.

## CLI 명령
```bash
ui_automation.exe file-ops verify row-count $ARGUMENTS
```

## 인수
- `originalCount`: 작업 전 행 수
- `--timeout <ms>`: 타임아웃 (밀리초, 기본값: 30000)
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "originalCount": 10,
    "currentCount": 7,
    "changed": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/file-ops:verify-row-count 10
/file-ops:verify-row-count 10 --timeout 60000 --json
```
