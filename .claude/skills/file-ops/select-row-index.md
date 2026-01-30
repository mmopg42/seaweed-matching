---
name: file-ops:select-row-index
description: "인덱스로 행을 선택합니다."
argument-hint: --row-index <n>
allowed-tools: Bash
disable-model-invocation: true
---

# 행 인덱스로 선택 (file-ops:select-row-index)

## 설명
인덱스로 행을 선택합니다.

## CLI 명령
```bash
ui_automation.exe file-ops select row-index --row-index $ARGUMENTS
```

## 인수
- `--row-index <n>`: 선택할 행 인덱스 (0 기반)

## 사용 예시
```bash
/file-ops:select-row-index --row-index 0
/file-ops:select-row-index --row-index 5
```
