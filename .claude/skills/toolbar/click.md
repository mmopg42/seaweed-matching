---
name: toolbar:click
description: "텍스트로 버튼을 클릭합니다."
argument-hint: <text>
allowed-tools: Bash
disable-model-invocation: true
---

# 툴바 클릭 (toolbar:click)

## 설명
텍스트 내용으로 버튼을 클릭합니다.

## CLI 명령
```bash
ui_automation.exe toolbar click $ARGUMENTS
```

## 인수
- `text`: 버튼 텍스트 (예: 'Start', 'Stop', 'Settings')

## 사용 예시
```bash
/toolbar:click Start
/toolbar:click Stop
```
