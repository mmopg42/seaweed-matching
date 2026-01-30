---
name: data-panel:data
description: "모든 DataGrid 데이터를 추출합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 데이터 패널 데이터 (data-panel:data)

## 설명
DataGrid의 모든 데이터를 추출합니다 (모든 행과 열).

## CLI 명령
```bash
ui_automation.exe datagrid data $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "rowCount": 10,
    "data": [
      {"GroupId": "line1_20250127_120000", "NIR1": "/path/to/nir1.tif", ...},
      ...
    ]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/data-panel:data
/data-panel:data --json
```
