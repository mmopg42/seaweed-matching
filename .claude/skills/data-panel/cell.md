---
name: data-panel:cell
description: "특정 셀 값을 가져옵니다."
argument-hint: <row> <col> [--json]
allowed-tools: Bash
---

# 데이터 패널 셀 (data-panel:cell)

## 설명
행과 열 인덱스로 특정 셀 값을 가져옵니다.

## CLI 명령
```bash
ui_automation.exe datagrid cell $ARGUMENTS
```

## 인수
- `row`: 행 인덱스 (0 기반)
- `col`: 열 인덱스 (0 기반)
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "row": 0,
    "column": 1,
    "value": "/path/to/file.tif"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/data-panel:cell 0 1
/data-panel:cell 5 2 --json
```
