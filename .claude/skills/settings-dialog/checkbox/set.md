---
name: settings-dialog:checkbox:set
description: "체크박스 상태를 설정합니다."
argument-hint: <name> <value>
allowed-tools: Bash
disable-model-invocation: true
---

# 체크박스 설정 (settings-dialog:checkbox:set)

## 설명
체크박스 상태를 설정합니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog checkbox set $ARGUMENTS
```

## 인수
- `name`: 체크박스 이름
- `value`: 체크박스 값 (true/false)

## 사용 예시
```bash
/settings-dialog:checkbox:set use_folder_suffix true
/settings-dialog:checkbox:set use_disk_cache false
```
