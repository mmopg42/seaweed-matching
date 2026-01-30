---
name: workflow:select-tab
description: "MainWindow에서 탭을 선택합니다."
argument-hint: <tab>
allowed-tools: Bash
disable-model-invocation: true
---

# 탭 선택 (workflow:select-tab)

## 설명
MainWindow TabControl에서 탭을 선택합니다.

## CLI 명령
```bash
ui_automation.exe workflow select-tab $ARGUMENTS
```

## 인수
- `tab`: 탭 이름: 'Line 1', 'Line 2', 또는 'Combined'

## 사용 예시
```bash
/workflow:select-tab "Line 1"
/workflow:select-tab "Line 2"
/workflow:select-tab Combined
```
