---
name: file-ops:wait-move
description: "이동 작업 완료를 대기합니다."
argument-hint: [--timeout <ms>]
allowed-tools: Bash
---

# 이동 대기 (file-ops:wait-move)

## 설명
이동 작업이 완료될 때까지 대기합니다 (행 수 변화 모니터링).

## CLI 명령
```bash
ui_automation.exe file-ops wait move $ARGUMENTS
```

## 인수
- `--timeout <ms>`: 타임아웃 (밀리초, 기본값: 30000)

## 사용 예시
```bash
/file-ops:wait-move
/file-ops:wait-move --timeout 60000
```
