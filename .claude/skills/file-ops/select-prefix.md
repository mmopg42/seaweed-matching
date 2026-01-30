---
name: file-ops:select-prefix
description: "접두사로 행을 선택합니다."
argument-hint: --prefix <text>
allowed-tools: Bash
disable-model-invocation: true
---

# 접두사로 선택 (file-ops:select-prefix)

## 설명
지정된 접두사로 시작하는 GroupId를 가진 모든 행을 선택합니다.

## CLI 명령
```bash
ui_automation.exe file-ops select prefix --prefix $ARGUMENTS
```

## 인수
- `--prefix <text>`: GroupId 접두사 필터 (예: 'line2_')

## 사용 예시
```bash
/file-ops:select-prefix --prefix line2_
/file-ops:select-prefix --prefix line1_20250127
```
