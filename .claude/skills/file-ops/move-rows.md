---
name: file-ops:move-rows
description: "행 인덱스로 선택하여 이동합니다."
argument-hint: --rows <indices>
allowed-tools: Bash
disable-model-invocation: true
---

# 행 이동 (file-ops:move-rows)

## 설명
인덱스로 행을 선택하고 이동 버튼을 클릭합니다.

## CLI 명령
```bash
ui_automation.exe file-ops move rows --rows $ARGUMENTS
```

## 인수
- `--rows <indices>`: 이동할 행 인덱스 (쉼표로 구분)

## 사용 예시
```bash
/file-ops:move-rows --rows 0,1,2
/file-ops:move-rows --rows 5,6,7,8
```
