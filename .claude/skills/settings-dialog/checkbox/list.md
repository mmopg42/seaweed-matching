---
name: settings-dialog:checkbox:list
description: "모든 체크박스를 나열합니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 체크박스 목록 (settings-dialog:checkbox:list)

## 설명
고급 탭의 모든 체크박스를 나열합니다.

## CLI 명령
```bash
ui_automation.exe settings-dialog checkbox list $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "SettingsDialog",
    "count": 5,
    "settings": {"use_folder_suffix": true, "use_disk_cache": false}
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/settings-dialog:checkbox:list
/settings-dialog:checkbox:list --json
```
