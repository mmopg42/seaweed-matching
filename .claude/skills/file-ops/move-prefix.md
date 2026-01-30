---
name: file-ops:move-prefix
description: "접두사로 선택하여 이동합니다."
argument-hint: --prefix <text>
allowed-tools: Bash
disable-model-invocation: true
---

# 접두사 이동 (file-ops:move-prefix)

## 설명
GroupId 접두사로 행을 선택하고 이동 버튼을 클릭합니다.

## CLI 명령
```bash
ui_automation.exe file-ops move prefix --prefix $ARGUMENTS
```

## 인수
- `--prefix <text>`: GroupId 접두사 필터 (예: 'line2_')

## 사용 예시
```bash
/file-ops:move-prefix --prefix line2_
/file-ops:move-prefix --prefix line1_20250127
```
