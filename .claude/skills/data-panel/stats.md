---
name: data-panel:stats
description: "StatisticsPanel 데이터를 읽습니다."
argument-hint: [--json]
allowed-tools: Bash
---

# 통계 패널 (data-panel:stats)

## 설명
StatisticsPanel에서 통계를 읽습니다 (파일 수, 매칭 상태 등).

## CLI 명령
```bash
ui_automation.exe stats $ARGUMENTS
```

## 인수
- `--json`: JSON 형식으로 출력

## 반환 값 (JSON 모드 시)
```json
{
  "success": true,
  "data": {
    "source": "StatisticsPanel",
    "statistics": {
      "NIR1": "10",
      "Normal1": "5",
      "Matched": "8"
    }
  },
  "timestamp": "..."
}
```

## 사용 예시
```bash
/data-panel:stats
/data-panel:stats --json
```
