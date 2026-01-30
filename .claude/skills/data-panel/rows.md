---
name: data-panel:rows
description: "DataGrid 행 수를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 데이터 패널 행 (data-panel:rows)

## 설명
DataGrid의 행 수를 가져옵니다 (헤더 제외).

## CLI 명령
```bash
ui_automation.exe datagrid rows $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "rowCount": 42
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/data-panel:rows
/data-panel:rows --json
```
