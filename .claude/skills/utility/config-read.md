---
name: utility:config-read
description: "구성 파일 내용을 읽습니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 구성 읽기 (utility:config-read)

## 설명
구성 파일 전체 내용을 읽습니다.

## CLI 명령
```bash
ui_automation.exe config read $ARGUMENTS
```

## 인수
- `--json`: JSON 예쁘게 출력

## 사용 예시
```bash
/utility:config-read
/utility:config-read --json
```
