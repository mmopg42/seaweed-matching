---
name: workflow:path:set-line1
description: "Line 1 경로를 설정합니다."
argument-hint: <type> <value>
allowed-tools: Bash
disable-model-invocation: true
---

# Line 1 경로 설정 (workflow:path:set-line1)

## 설명
유형별로 Line 1 경로 값을 설정합니다.

## CLI 명령
```bash
ui_automation.exe workflow path set-line1 $ARGUMENTS
```

## 인수
- `type`: 경로 유형: samplename, movenir, movealldata
- `value`: 설정할 경로 값

## 사용 예시
```bash
/workflow:path:set-line1 samplename /path/to/sample
/workflow:path:set-line1 movenir /path/to/nir
```
