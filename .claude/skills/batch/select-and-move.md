---
name: batch:select-and-move
description: "여러 행을 선택하여 이동합니다."
argument-hint: [--rows <indices>] [--group-ids <ids>] [--start-index <n>] [--count <n>] [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 일괄 선택 및 이동 (batch:select-and-move)

## 설명
인덱스, GroupId 또는 범위로 여러 행을 선택한 후 이동합니다.

## CLI 명령
```bash
ui_automation.exe batch select-and-move $ARGUMENTS
```

## 인수
- `--rows <indices>`: 행 인덱스 (쉼표로 구분)
- `--group-ids <ids>`: GroupId (쉼표로 구분)
- `--start-index <n>`: 범위 선택 시작 인덱스
- `--count <n>`: 범위 선택 개수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "selected": 3,
    "moved": 3,
    "duration": "completed"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/batch:select-and-move --rows 0,1,2
/batch:select-and-move --group-ids line1_20250127_120000,line1_20250127_120001
/batch:select-and-move --start-index 0 --count 5 --json
```
