---
name: file-ops:confirm
description: "삭제 확인 대화상자를 처리합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 확인 (file-ops:confirm)

## 설명
삭제 작업을 위한 확인 대화상자 버튼을 찾아 클릭합니다.

## CLI 명령
```bash
ui_automation.exe file-ops confirm $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "action": "confirm",
    "confirmed": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/file-ops:confirm
/file-ops:confirm --json
```
