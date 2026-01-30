---
name: batch:select-and-delete
description: "여러 행을 선택하여 삭제합니다."
argument-hint: [--rows <indices>] [--group-ids <ids>] [--start-index <n>] [--count <n>] [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 일괄 선택 및 삭제 (batch:select-and-delete)

## 설명
인덱스, GroupId 또는 범위로 여러 행을 선택한 후 삭제합니다. 확인 대화상자를 자동으로 처리합니다.

## CLI 명령
```bash
ui_automation.exe batch select-and-delete $ARGUMENTS
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
    "deleted": 3,
    "confirmed": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/batch:select-and-delete --rows 0,1,2
/batch:select-and-delete --group-ids line1_20250127_120000
/batch:select-and-delete --start-index 0 --count 3 --json
```
