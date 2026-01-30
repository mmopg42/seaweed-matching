---
name: windows:preview
description: "ImagePreviewWindow를 찾습니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 미리보기 윈도우 (windows:preview)

## 설명
ImagePreviewWindow를 찾습니다.

## CLI 명령
```bash
ui_automation.exe windows preview $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "found": true,
    "windowType": "ImagePreviewWindow",
    "title": "Image Preview",
    "className": "Window",
    "automationId": "ImagePreviewWindow"
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/windows:preview
/windows:preview --json
```
