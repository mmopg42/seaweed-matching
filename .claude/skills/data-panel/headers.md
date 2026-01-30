---
name: data-panel:headers
description: "DataGrid 열 헤더를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 데이터 패널 헤더 (data-panel:headers)

## 설명
DataGrid의 열 헤더를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe datagrid headers $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "columnCount": 10,
    "columns": ["GroupId", "NIR1", "Normal1", "Cam1", ...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/data-panel:headers
/data-panel:headers --json
```
