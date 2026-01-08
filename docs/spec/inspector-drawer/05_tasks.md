---
Task: inspector_drawer
Created: 2024-12-19
Status: Draft
Depends On: 04_design.md
---

# Inspector Drawer - Implementation Tasks

## Task Checklist

### Phase 1: ViewModel 구현

- [ ] **Task 1.1**: InspectorDrawerViewModel 클래스 생성
  - [ ] `ChronoView/UI/ViewModels/InspectorDrawerViewModel.cs` 파일 생성
  - [ ] ViewModelBase 상속
  - [ ] 기본 속성 구현 (SelectedGroup, IsPinned, IsExpanded, DrawerHeight)
  - [ ] 이미지 속성 구현 (NirImage, GeneralCameraImage, Camera1-3Image)
  - [ ] 파일명 속성 구현 (NirFileName, GeneralCameraFileName, etc.)
  - [ ] PinCommand, CloseCommand 구현
  - [ ] LoadImagesAsync 메서드 구현
  - [ ] ClearAllImages 메서드 구현

- [ ] **Task 1.2**: 이미지 로딩 로직 구현
  - [ ] ImageManager 서비스 주입
  - [ ] 비동기 이미지 로딩 구현
  - [ ] 에러 핸들링 (파일 없음, 로딩 실패)
  - [ ] 이미지 크기 계산 로직 (GeneralCameraImageSize)

- [ ] **Task 1.3**: 상태 관리 로직 구현
  - [ ] SelectedGroup setter에서 IsExpanded 자동 관리
  - [ ] Pin 상태에 따른 그룹 업데이트 로직
  - [ ] 높이 제한 로직 (160~260px)

### Phase 2: UI 구현

- [ ] **Task 2.1**: InspectorDrawer UserControl 생성
  - [ ] `ChronoView/UI/Controls/InspectorDrawer.xaml` 파일 생성
  - [ ] `ChronoView/UI/Controls/InspectorDrawer.xaml.cs` 파일 생성
  - [ ] 기본 레이아웃 구조 (Grid with 3 rows)
  - [ ] 배경색 및 보더 스타일 적용 (#F5F5F5, 1px 보더)

- [ ] **Task 2.2**: 헤더 영역 구현
  - [ ] 리사이즈 핸들 UI (상단 5px 영역)
  - [ ] 제목 및 상태 표시 (▼ 상세 프리뷰, GroupId, GroupStatus)
  - [ ] [고정] 버튼 구현
  - [ ] [x] 닫기 버튼 구현
  - [ ] 폰트 크기 적용 (12~13px 헤더, 14~16px Bold GroupId)

- [ ] **Task 2.3**: 이미지 프리뷰 영역 구현
  - [ ] UniformGrid 또는 WrapPanel로 5개 이미지 가로 배치
  - [ ] 각 이미지 Border 스타일 (1px 보더, 흰색 배경)
  - [ ] 이미지 레이블 (NIR 그래프, 일반카메라, Cam1-3)
  - [ ] 이미지 크기 제약 (MinWidth 200px, MinHeight 120px)
  - [ ] 이미지 바인딩 (NirImage, GeneralCameraImage, Camera1-3Image)

- [ ] **Task 2.4**: 파일명 영역 구현
  - [ ] Grid로 5개 컬럼 배치
  - [ ] NIR 파일명 영역 (세로 배치: 파일명, spc, txt)
  - [ ] 일반카메라 파일명 영역 (파일명, 이미지 크기)
  - [ ] Cam1-3 파일명 영역
  - [ ] 폰트 크기 적용 (13~14px)

- [ ] **Task 2.5**: 리사이즈 기능 구현
  - [ ] ResizeHandle_MouseDown 이벤트 핸들러
  - [ ] ResizeHandle_MouseMove 이벤트 핸들러
  - [ ] ResizeHandle_MouseUp 이벤트 핸들러
  - [ ] 높이 제한 로직 (Math.Clamp 160~260px)
  - [ ] 커서 변경 (Cursors.SizeNS)

### Phase 3: MainWindow 통합

- [ ] **Task 3.1**: MainWindow.xaml 수정
  - [ ] Grid RowDefinitions 수정 (InspectorDrawer Row 추가)
  - [ ] GridSplitter 추가 (드로어 리사이즈용)
  - [ ] InspectorDrawer UserControl 추가
  - [ ] Visibility 바인딩 (IsExpanded)
  - [ ] Height 바인딩 (DrawerHeight)

- [ ] **Task 3.2**: MainWindowViewModel 수정
  - [ ] InspectorDrawerViewModel 인스턴스 추가
  - [ ] SelectedGroup 속성 추가
  - [ ] DataGrid SelectionChanged 이벤트 핸들러 수정
  - [ ] SelectedGroup과 InspectorDrawerViewModel 연결

- [ ] **Task 3.3**: DI 컨테이너 설정 (필요시)
  - [ ] InspectorDrawerViewModel 등록 (필요한 경우)
  - [ ] ImageManager 서비스 확인

### Phase 4: 테스트 및 검증

- [ ] **Task 4.1**: 기능 테스트
  - [ ] 그룹 행 클릭 시 드로어 열림 확인
  - [ ] 이미지 로딩 확인 (NIR, 일반카메라, Cam1-3)
  - [ ] 파일명 표시 확인
  - [ ] 리사이즈 기능 확인 (160~260px 범위)
  - [ ] 고정 기능 확인 (다른 행 클릭 시 유지)
  - [ ] 닫기 기능 확인

- [ ] **Task 4.2**: 에러 케이스 테스트
  - [ ] 이미지 파일 없을 때 처리 확인
  - [ ] 이미지 로딩 실패 시 처리 확인
  - [ ] null 그룹 선택 시 처리 확인

- [ ] **Task 4.3**: UI/UX 검증
  - [ ] 레이아웃이 요구사항과 일치하는지 확인
  - [ ] 폰트 크기가 지정된 범위인지 확인
  - [ ] 색상이 지정된 값인지 확인 (#F5F5F5 배경, #E0E0E0 보더)
  - [ ] 이미지 비율이 가로로 넓은지 확인
  - [ ] 드로어가 닫혀있을 때 공간을 차지하지 않는지 확인

- [ ] **Task 4.4**: 성능 검증
  - [ ] 이미지 로딩이 비동기로 처리되는지 확인
  - [ ] 드로어 열기/닫기가 빠른지 확인 (< 50ms)
  - [ ] 메모리 사용량 확인 (이미지 해제 확인)

### Phase 5: 문서화 및 정리

- [ ] **Task 5.1**: 코드 주석 추가
  - [ ] InspectorDrawerViewModel 주요 메서드 주석
  - [ ] InspectorDrawer.xaml.cs 이벤트 핸들러 주석
  - [ ] 복잡한 로직에 대한 설명 주석

- [ ] **Task 5.2**: README 업데이트 (필요시)
  - [ ] 새로운 기능 설명 추가
  - [ ] 사용 방법 안내

---

## Implementation Order

1. **Phase 1** (ViewModel): 핵심 로직 구현
2. **Phase 2** (UI): 사용자 인터페이스 구현
3. **Phase 3** (Integration): MainWindow 통합
4. **Phase 4** (Testing): 테스트 및 검증
5. **Phase 5** (Documentation): 문서화

---

## Estimated Effort

| Phase | Tasks | Estimated Time |
|-------|-------|----------------|
| Phase 1 | 3 tasks | 4-6 hours |
| Phase 2 | 5 tasks | 6-8 hours |
| Phase 3 | 3 tasks | 2-3 hours |
| Phase 4 | 4 tasks | 2-3 hours |
| Phase 5 | 2 tasks | 1 hour |
| **Total** | **17 tasks** | **15-21 hours** |

---

## Dependencies

- ImageManager 서비스가 정상 동작해야 함
- MainWindowViewModel의 FileGroups 컬렉션이 정상 동작해야 함
- 기존 로그 패널 기능에 영향 없어야 함

---

## Notes

- 이미지 로딩은 기존 ImageManager를 활용하여 일관성 유지
- 드로어 높이는 사용자 설정으로 저장할 수 있음 (향후 개선)
- 애니메이션 효과는 초기 버전에서는 제외 (성능 우선)

---

## Approval

- [ ] All tasks identified
- [ ] Dependencies clear
- [ ] Implementation order defined
- [ ] Estimated effort reasonable

**Next Step**: Implementation




