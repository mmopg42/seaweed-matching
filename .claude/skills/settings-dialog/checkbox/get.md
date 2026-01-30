---
name: settings-dialog:checkbox:get
description: "체크박스 상태를 가져옵니다."
argument-hint: <name> [--json]
allowed-tools: Bash
---

# 체크박스 상태 가져오기 (settings-dialog:checkbox:get)

## 설명
체크박스 상태를 가져옵니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog checkbox get $ARGUMENTS
```

## 인수
- `name`: 체크박스 이름 (예: use_folder_suffix, use_disk_cache)
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "checkbox": "use_folder_suffix",
    "isChecked": true
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:checkbox:get use_folder_suffix
/settings-dialog:checkbox:get use_disk_cache --json
```
