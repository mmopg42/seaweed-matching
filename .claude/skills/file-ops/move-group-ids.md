---
name: file-ops:move-group-ids
description: "GroupId로 선택하여 이동합니다."
argument-hint: --group-ids <ids>
allowed-tools: Bash
disable-model-invocation: true
---

# GroupId 이동 (file-ops:move-group-ids)

## 설명
GroupId로 행을 선택하고 이동 버튼을 클릭합니다.

## CLI 명령
```bash
ui_automation.exe file-ops move group-ids --group-ids $ARGUMENTS
```

## 인수
- `--group-ids <ids>`: 이동할 GroupId (쉼표로 구분)

## 사용 예시
```bash
/file-ops:move-group-ids --group-ids line1_20250127_120000,line1_20250127_120001
```
