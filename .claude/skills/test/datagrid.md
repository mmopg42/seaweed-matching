---
name: test:datagrid
description: "DataGrid 접근성을 확인합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# DataGrid 확인 (test:datagrid)

## 설명
DataGrid에 접근 가능한지 확인하고 행/열 정보를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe test datagrid $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "accessible": true,
    "rowCount": 42,
    "headers": ["GroupId", "NIR1", "Normal1", ...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/test:datagrid
/test:datagrid --json
```
