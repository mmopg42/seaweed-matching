---
name: console-logs:search
description: "콘솔 로그에서 텍스트를 검색합니다."
argument-hint: <text> [--file <path>] [--latest] [--max <n>] [--json]
allowed-tools: Bash
---

# 콘솔 로그 검색 (console-logs:search)

## 설명
콘솔 로그에서 특정 텍스트를 검색합니다.

## CLI 명령
```bash
ui_automation.exe console-logs search $ARGUMENTS
```

## 인수
- `text`: 검색 텍스트 (필수)
- `--file <path>`: 특정 로그 파일 경로
- `--latest`: 최신 로그 파일 사용
- `--max <n>`: 최대 결과 수 (기본값: 50)
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "ConsoleLogs",
    "file": "C:\\Users\\...\\log.txt",
    "search": "ERROR",
    "maxResults": 50,
    "count": 5,
    "logs": ["line with ERROR", ...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/console-logs:search ERROR
/console-logs:search "Exception" --latest --max 100
/console-logs:search WARNING --file path/to/log.txt --json
```
