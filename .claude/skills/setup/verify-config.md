---
name: setup:verify-config
description: "시뮬레이터와 ChronoView 구성을 확인합니다."
argument-hint: [--config-path <path>] [--open-settings] [--strict] [--json]
allowed-tools: Bash
---

# 구성 확인 (setup:verify-config)

## 설명
시뮬레이터 구성이 ChronoView 구성과 일치하는지 확인합니다.

## CLI 명령
```bash
ui_automation.exe setup verify-config $ARGUMENTS
```

## 인수
- `--config-path <path>`: 시뮬레이터 구성 경로 (기본값: task_helper/data_test/dist/simulator_config.json)
- `--open-settings`: SettingsDialog가 열려 있지 않으면 열기
- `--strict`: 불일치 시 실패
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "success": true,
    "matched": ["nir1", "normal1"],
    "mismatches": [],
    "missing": []
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/setup:verify-config
/setup:verify-config --strict
/setup:verify-config --open-settings --json
```
