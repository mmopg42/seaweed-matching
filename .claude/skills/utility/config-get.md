---
name: utility:config-get
description: "특정 구성 값을 가져옵니다."
argument-hint: --key <path>
allowed-tools: Bash
---

# 구성 가져오기 (utility:config-get)

## 설명
JSON 경로로 특정 구성 값을 가져옵니다.

## CLI 명령
```bash
ui_automation.exe config get $ARGUMENTS
```

## 인수
- `--key <path>`: JSON 경로 (예: folderPaths.line1SampleName)

## 사용 예시
```bash
/utility:config-get --key folderPaths.line1SampleName
/utility:config-get --key imageSettings.jpegQuality
```
