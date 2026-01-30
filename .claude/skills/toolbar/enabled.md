---
name: toolbar:enabled
description: "버튼 활성화 상태를 확인합니다."
argument-hint: <text>
allowed-tools: Bash
---

# 툴바 활성화 (toolbar:enabled)

## 설명
버튼이 활성화되어 있는지 확인합니다.

## CLI 명령
```bash
ui_automation.exe toolbar enabled $ARGUMENTS
```

## 인수
- `text`: 버튼 텍스트

## 사용 예시
```bash
/toolbar:enabled Start
/toolbar:enabled Stop
```
