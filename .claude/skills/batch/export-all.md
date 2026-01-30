---
name: batch:export-all
description: "모든 데이터를 내보냅니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 일괄 내보내기 (batch:export-all)

## 설명
ChronoView에서 사용 가능한 모든 데이터를 내보냅니다 (통계, DataGrid 행, 카메라 상태).

## CLI 명령
```bash
ui_automation.exe batch export-all $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "timestamp": "2026-01-27T12:34:56.789Z",
    "statistics": {"NIR1": "10", "Normal1": "5"},
    "dataGrid": {
      "rowCount": 10,
      "rows": [...]
    },
    "cameraStates": {"General": true, "Nir1": false}
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/batch:export-all
/batch:export-all --json
```
