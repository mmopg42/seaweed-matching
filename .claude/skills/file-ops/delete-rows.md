---
name: file-ops:delete-rows
description: "행 인덱스로 선택하여 삭제합니다."
argument-hint: --rows <indices>
allowed-tools: Bash
disable-model-invocation: true
---

# 행 삭제 (file-ops:delete-rows)

## 설명
인덱스로 행을 선택하고 삭제 버튼을 클릭합니다.

## CLI 명령
```bash
ui_automation.exe file-ops delete rows --rows $ARGUMENTS
```

## 인수
- `--rows <indices>`: 삭제할 행 인덱스 (쉼표로 구분)

## 사용 예시
```bash
/file-ops:delete-rows --rows 0,1,2
```
