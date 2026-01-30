---
name: file-ops:select-group-id
description: "GroupId로 행을 선택합니다."
argument-hint: --group-id <id>
allowed-tools: Bash
disable-model-invocation: true
---

# GroupId로 선택 (file-ops:select-group-id)

## 설명
GroupId 값으로 행을 선택합니다.

## CLI 명령
```bash
ui_automation.exe file-ops select group-id --group-id $ARGUMENTS
```

## 인수
- `--group-id <id>`: 찾아서 선택할 GroupId 값

## 사용 예시
```bash
/file-ops:select-group-id --group-id line1_20250127_120000
```
