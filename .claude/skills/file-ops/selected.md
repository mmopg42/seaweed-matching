---
name: file-ops:selected
description: "선택된 행 인덱스를 가져옵니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 선택된 행 (file-ops:selected)

## 설명
현재 선택된 행의 인덱스를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe file-ops selected $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "count": 3,
    "selectedRows": [0, 1, 2]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/file-ops:selected
/file-ops:selected --json
```
