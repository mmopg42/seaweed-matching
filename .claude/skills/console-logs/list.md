---
name: console-logs:list
description: "사용 가능한 콘솔 로그 파일을 나열합니다."
argument-hint: [--date <YYYYMMDD>] [--latest] [--json]
allowed-tools: Bash
---

# 콘솔 로그 목록 (console-logs:list)

## 설명
사용 가능한 콘솔 로그 파일을 나열합니다. 날짜 필터링을 지원합니다.

## CLI 명령
```bash
ui_automation.exe console-logs list $ARGUMENTS
```

## 인수
- `--date <YYYYMMDD>`: 날짜 필터 (YYYYMMDD 형식)
- `--latest`: 최신 날짜 폴더 사용
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "ConsoleLogs",
    "logDirectory": "C:\\Users\\...\\Logs\\20260127",
    "dateFilter": "20260127",
    "count": 3,
    "files": ["log1.txt", "log2.txt", "log3.txt"]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/console-logs:list
/console-logs:list --date 20260127
/console-logs:list --latest --json
```
