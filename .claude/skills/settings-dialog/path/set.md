---
name: settings-dialog:path:set
description: "경로 값을 설정합니다."
argument-hint: <key> <value>
allowed-tools: Bash
disable-model-invocation: true
---

# 경로 설정 (settings-dialog:path:set)

## 설명
키로 경로 값을 설정합니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog path set $ARGUMENTS
```

## 인수
- `key`: 경로 키 (예: nir1, normal1, nir2, normal2, cam1-6)
- `value`: 설정할 경로 값

## 사용 예시
```bash
/settings-dialog:path:set nir1 /path/to/nir1
/settings-dialog:path:set normal1 /path/to/normal1
```
