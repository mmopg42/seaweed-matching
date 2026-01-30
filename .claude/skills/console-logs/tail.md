---
name: console-logs:tail
description: "콘솔 로그에서 최근 N줄을 읽습니다."
argument-hint: [count] [--file <path>] [--latest] [--json]
allowed-tools: Bash
---

# 콘솔 로그 꼬리부분 (console-logs:tail)

## 설명
콘솔 로그 파일에서 최근 N줄을 읽습니다.

## CLI 명령
```bash
ui_automation.exe console-logs tail $ARGUMENTS
```

## 인수
- `count`: 읽을 줄 수 (기본값: 20)
- `--file <path>`: 특정 로그 파일 경로
- `--latest`: 최신 로그 파일 사용
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "ConsoleLogs",
    "file": "C:\\Users\\...\\log.txt",
    "requested": 20,
    "returned": 20,
    "logs": ["line1", "line2", ...]
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/console-logs:tail 50
/console-logs:tail 100 --latest
/console-logs:tail 30 --file path/to/log.txt --json
```
