---
name: file-ops:verify-deleted
description: "그룹이 삭제되었는지 확인합니다."
argument-hint: <groupId> [--json]
allowed-tools: Bash
---

# 삭제 확인 (file-ops:verify-deleted)

## 설명
DataGrid에서 GroupId가 더 이상 존재하지 않는지 확인합니다.

## CLI 명령
```bash
ui_automation.exe file-ops verify deleted $ARGUMENTS
```

## 인수
- `groupId`: 확인할 GroupId
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "groupId": "line1_20250127_120000",
    "verified": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/file-ops:verify-deleted line1_20250127_120000
/file-ops:verify-deleted line1_20250127_120000 --json
```
