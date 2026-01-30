---
name: data-panel:info
description: "DataGrid 요약 정보를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 데이터 패널 정보 (data-panel:info)

## 설명
DataGrid의 요약 정보를 가져옵니다 (열 헤더 + 행 수).

## CLI 명령
```bash
ui_automation.exe datagrid info $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "columnCount": 10,
    "rowCount": 42,
    "columns": ["GroupId", "NIR1", "Normal1", ...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/data-panel:info
/data-panel:info --json
```
