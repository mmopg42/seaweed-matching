---
name: data-panel:export
description: "DataGrid를 JSON으로 내보냅니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 데이터 패널 내보내기 (data-panel:export)

## 설명
모든 DataGrid 데이터를 JSON 형식으로 내보냅니다.

## CLI 명령
```bash
ui_automation.exe datagrid export
```

## 반환 값
```json
{
  "success": true,
  "data": {
    "rowCount": 10,
    "exportedAt": "2026-01-27T12:34:56.789Z",
    "data": [...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/data-panel:export
```
