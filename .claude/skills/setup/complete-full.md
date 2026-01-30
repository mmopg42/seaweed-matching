---
name: setup:complete-full
description: "전체 설정 워크플로우를 완료합니다."
argument-hint: [--verify-config] [--config-path <path>] [--strict] [--json]
allowed-tools: Bash
disable-model-invocation: true
---

# 전체 설정 완료 (setup:complete-full)

## 설명
전체 설정 워크플로우를 완료합니다 (카메라 시작, 시작 버튼 클릭, MainWindow 대기).

## CLI 명령
```bash
ui_automation.exe setup complete-full $ARGUMENTS
```

## 인수
- `--verify-config`: 구성 확인 먼저 실행
- `--config-path <path>`: 확인용 시뮬레이터 구성 경로
- `--strict`: 확인 실패 시 실패
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "completed": true,
    "configVerified": true,
    "mainWindowAppeared": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/setup:complete-full
/setup:complete-full --verify-config
/setup:complete-full --json
```
