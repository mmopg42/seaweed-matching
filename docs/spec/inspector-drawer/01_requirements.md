---
Task: inspector_drawer
Created: 2024-12-19
Status: Draft
Summary: 로그 패널 위에 접히는 상세 프리뷰 드로어(Inspector Drawer) 구현
Research Required: No
---

# Inspector Drawer - Requirements

## 1. Goal

### 1.1 Primary Goal

사용자가 파일 그룹 행을 클릭하면 로그 패널 바로 위에 상세 프리뷰 패널이 펼쳐지고, 선택한 그룹의 이미지와 메타데이터를 크게 볼 수 있어야 합니다.

### 1.2 Success Criteria

- [ ] 파일 그룹 행 클릭 시 드로어가 펼쳐지고 선택한 그룹의 상세 정보가 표시됨
- [ ] 드로어 높이는 160~260px 범위에서 조절 가능하며, 기본값은 200px
- [ ] 드로어 상단 경계선을 드래그하여 높이를 리사이즈할 수 있음
- [ ] "고정(pin)" 버튼 클릭 시 다른 행을 클릭해도 현재 프리뷰가 유지됨
- [ ] "x" 버튼 클릭 시 드로어가 닫힘
- [ ] 드로어에 NIR 그래프, 일반카메라, Cam1~Cam3 이미지가 가로로 넓은 비율로 크게 표시됨
- [ ] 각 이미지 아래에 파일명이 표시됨
- [ ] NIR 파일명, spc, txt 정보가 세로로 배치되어 표시됨
- [ ] 일반카메라 이미지 크기 정보가 표시됨
- [ ] 드로어가 닫혀있을 때는 공간을 차지하지 않음
- [ ] 드로어 배경색은 #F5~F7 계열의 옅은 회색
- [ ] 드로어는 얇은 1px 보더로만 구획됨 (카드 느낌 과하지 않음)

## 2. Constraints

### 2.1 Technical Constraints

- WPF 기반 UI로 구현해야 함
- MainWindow.xaml의 기존 Grid 레이아웃 구조를 유지해야 함
- MainWindowViewModel과 통합되어야 함
- 기존 로그 패널(LogPanel) 기능에 영향을 주지 않아야 함
- 이미지 로딩은 기존 ImageManager/ImageLoader 시스템을 활용해야 함

### 2.2 Business Constraints

- 기존 UI 레이아웃의 큰 변경 없이 추가되어야 함
- 성능 저하 없이 동작해야 함 (이미지 로딩 최적화)

### 2.3 Non-Goals (Out of Scope)

- 드로어 내에서 이미지 편집 기능
- 드로어 내에서 파일 삭제/이동 기능
- 드로어 내에서 그룹 상태 변경 기능
- 드로어 애니메이션 효과 (단순 show/hide)
- 드로어 위치 변경 (항상 로그 패널 위 고정)
- 드로어 너비 조절 (전체 너비 고정)

## 3. Questions to Investigate

> Research Required = No이므로 이 섹션은 비워둡니다.

## 4. Assumptions

- 사용자는 마우스로 드래그하여 높이를 조절할 수 있음
- 선택된 그룹의 이미지 데이터는 MainWindowViewModel에서 제공됨
- 이미지 로딩은 비동기로 처리되어야 함
- 드로어가 열려있을 때도 로그 패널은 정상적으로 동작해야 함

## 5. Dependencies

### 5.1 Blocked By

| Dependency | Status | Owner |
|------------|--------|-------|
| MainWindow.xaml 레이아웃 구조 파악 | Done | - |
| MainWindowViewModel의 그룹 선택 메커니즘 | Done | - |
| 이미지 로딩 시스템 (ImageManager) | Done | - |

### 5.2 Blocks

| Dependent Task | Impact if Delayed |
|----------------|-------------------|
| 없음 | - |

---

## Approval

- [ ] Requirements reviewed and approved
- [ ] Success criteria are measurable
- [ ] Scope boundaries are clear
- [ ] All blocking dependencies identified

**Next Step**: 03_plan.md






