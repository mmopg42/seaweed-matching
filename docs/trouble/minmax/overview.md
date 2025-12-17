# MinDelay/MaxDelay Migration - 개요

## 문제점
현재 `ExpectedDelay ± Tolerance` 방식은 대칭 범위만 표현 가능:
- Camera1 = Normal + 4~6초를 표현할 수 없음
- Delay=5, Tol=±1로 설정하면 4~6초 뿐만 아니라 0~10초도 매칭됨

## 해결책
`MinDelay` / `MaxDelay` 비대칭 범위 도입:
- Camera1: MinDelay=4, MaxDelay=6
- 정확히 "Normal + 4초 ~ Normal + 6초" 범위만 매칭

## 변경 범위
- ✅ Models: DataSequenceItem, DataSequenceSettings
- ⏳ ViewModels: DataSequenceItemViewModel, SettingsDialogViewModel  
- ⏳ Views: SettingsDialog.xaml
- ⏳ Presets: DataSequencePresets.cs
- ⏳ Core: MonitoringOrchestrator.cs (Match 3)
- ⏳ Tests: DataSequenceSettingsValidation.cs
- ⏳ Config: appsettings.json

## 파일 목록
- `tasks.md` - 상세 체크리스트 ← **여기서 작업 진행**
- `overview.md` - 이 파일
- `notes.md` - 구현 노트

## 다음 단계
`tasks.md`를 열어서 Phase 2부터 시작
