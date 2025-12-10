# Task 4.5: Combined 탭 구현 계획

## 개요

**작성일자**: 2025-12-11
**작업명**: Task 4.5 - Combined 탭 구현
**상태**: 📋 계획 단계 (피드백 반영 완료)
**우선순위**: 낮음 (Line 1/Line 2 독립 탭이 주 사용 시나리오)

---

## 1. 목적 및 배경

### 1.1 목적

Combined 탭은 Line 1과 Line 2의 파일 그룹을 동시에 화면에 표시하여, 사용자가 두 라인의 상태를 한 눈에 비교하고 모니터링할 수 있도록 합니다.

### 1.2 사용 시나리오

**시나리오 1: 생산 라인 동시 모니터링**
- 오퍼레이터가 Line 1과 Line 2의 파일 그룹 수집 상태를 동시에 확인
- 한 라인의 지연이나 오류 발생 시 빠른 대응 가능

**시나리오 2: 라인 간 비교**
- Line 1과 Line 2의 처리 속도 비교
- 각 라인의 매칭률, 실패율 동시 확인
- 불균형 발생 시 작업 재배분 판단

**시나리오 3: 다중 라인 작업**
- 사용자가 Line 1에서 일부 그룹 선택
- Line 2에서 다른 그룹 선택
- 각 라인별로 독립적인 Move/Delete 작업 수행

---

## 2. 현재 상태

### 2.1 기존 구현 (MainWindow.xaml / MainWindowViewModel.cs)

**MainWindow.xaml Lines 534-545**:
```xml
<TabItem Header="Combined">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="5"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <TextBlock Grid.Column="0" Text="Line 1"
                   HorizontalAlignment="Center" VerticalAlignment="Center"/>
        <GridSplitter Grid.Column="1" HorizontalAlignment="Stretch"
                      Background="{StaticResource BorderBrush}"/>
        <TextBlock Grid.Column="2" Text="Line 2"
                   HorizontalAlignment="Center" VerticalAlignment="Center"/>
    </Grid>
</TabItem>
```

**현재 상태**:
- ✅ Grid 레이아웃 구조 존재 (2개 컬럼 + GridSplitter)
- ❌ DataGrid 미구현 (텍스트 플레이스홀더만 존재)
- ✅ `GetSelectedGroups()` 메서드 존재, `ActiveTabIndex`에 따라 분기 처리됨 (기본값: `FileGroups` 전체 반환)

---

## 3. 요구사항

### 3.1 기능 요구사항

#### FR-1: 동시 표시
- Line 1 DataGrid와 Line 2 DataGrid를 좌우로 나란히 배치
- GridSplitter로 각 DataGrid의 크기 조절 가능

#### FR-2: 독립적인 선택
- Line 1 DataGrid의 선택 상태는 `Line1Groups` 컬렉션의 `IsSelected` 속성과 바인딩
- Line 2 DataGrid의 선택 상태는 `Line2Groups` 컬렉션의 `IsSelected` 속성과 바인딩
- 두 DataGrid는 독립적인 다중 선택 가능 (DragSelectBehavior 적용)

#### FR-3: 일관된 UI
- Line 1/Line 2 독립 탭과 동일한 컬럼 구조 사용
- 동일한 Style 적용 (FileGroupDataGridStyle, FileGroupRowStyle)
- Abnormal 그룹 하이라이트 동일하게 적용

#### FR-4: 작업 버튼 연동
- Combined 탭 활성화 시 Move/Delete 버튼은 **현재 포커스된 DataGrid**의 선택 항목에 적용
- 또는 두 라인의 선택 항목을 모두 처리 (정책 결정 필요)

### 3.2 비기능 요구사항

#### NFR-1: 성능
- 각 라인에 1000개 이상의 그룹이 있어도 부드러운 스크롤
- UI Virtualization 활성화 (DataGrid 기본값)

#### NFR-2: 반응성
- 창 크기 조절 시 두 DataGrid가 비율에 맞게 조정
- GridSplitter 드래그 시 실시간 크기 조절

#### NFR-3: 일관성
- 독립 탭에서 선택한 항목이 Combined 탭에서도 선택 상태 유지
- 컬렉션 변경(추가/삭제) 시 두 탭 간 동기화

---

## 4. 설계

### 4.1 UI 레이아웃

```
┌─────────────────────────────────────────────────────────────────┐
│                        Combined 탭                               │
├────────────────────────────┬─┬───────────────────────────────────┤
│       Line 1 DataGrid      │S│       Line 2 DataGrid             │
│  ┌──┬─────┬────────┬─────┐ │P│  ┌──┬─────┬────────┬─────┐      │
│  │☑│Index│Status  │Main │ │L│  │☑│Index│Status  │Main │      │
│  ├──┼─────┼────────┼─────┤ │I│  ├──┼─────┼────────┼─────┤      │
│  │☑│  1  │Complete│[img]│ │T│  │☐│ 101 │Complete│[img]│      │
│  │☐│  2  │Partial │[img]│ │T│  │☐│ 102 │Waiting │[img]│      │
│  │☑│  3  │Complete│[img]│ │E│  │☑│ 103 │Complete│[img]│      │
│  │ ...                    │ │R│  │ ...                    │      │
└────────────────────────────┴─┴───────────────────────────────────┘
```

**구성 요소**:
- **Grid.Column="0"**: Line 1 DataGrid
- **Grid.Column="1"**: GridSplitter (5px width, 드래그 가능)
- **Grid.Column="2"**: Line 2 DataGrid

### 4.2 XAML 구조

```xml
<TabItem Header="Combined">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="5"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>

        <!-- Line 1 DataGrid -->
        <Border Grid.Column="0" BorderBrush="{StaticResource BorderBrush}"
                BorderThickness="0,0,1,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- Header -->
                <Border Grid.Row="0" Background="{StaticResource PanelBackgroundBrush}"
                        Padding="8,4">
                    <TextBlock Text="Line 1" FontWeight="Bold" FontSize="12"/>
                </Border>

                <!-- Line 1 DataGrid (UserControl 사용) -->
                <local:FileGroupDataGrid Grid.Row="1"
                                       ItemsSource="{Binding Line1Groups}"
                                       SelectedGroup="{Binding SelectedLine1Group, Mode=TwoWay}"/>
            </Grid>
        </Border>

        <!-- GridSplitter -->
        <GridSplitter Grid.Column="1"
                      HorizontalAlignment="Stretch"
                      Background="{StaticResource BorderBrush}"
                      VerticalAlignment="Stretch"/>

        <!-- Line 2 DataGrid -->
        <Border Grid.Column="2" BorderBrush="{StaticResource BorderBrush}"
                BorderThickness="1,0,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="*"/>
                </Grid.RowDefinitions>

                <!-- Header -->
                <Border Grid.Row="0" Background="{StaticResource PanelBackgroundBrush}"
                        Padding="8,4">
                    <TextBlock Text="Line 2" FontWeight="Bold" FontSize="12"/>
                </Border>

                <!-- Line 2 DataGrid (UserControl 사용) -->
                <local:FileGroupDataGrid Grid.Row="1"
                                       ItemsSource="{Binding Line2Groups}"
                                       SelectedGroup="{Binding SelectedLine2Group, Mode=TwoWay}"/>
            </Grid>
        </Border>
    </Grid>
</TabItem>
```

### 4.3 컬럼 정의 전략

**옵션 1: 전체 컬럼 유지** (권장)
- Line 1/Line 2 독립 탭과 동일한 모든 컬럼 표시
- 장점: 정보 손실 없음
- 단점: 화면이 좁을 때 스크롤 필요

**옵션 2: 간소화 컬럼**
- 필수 컬럼만 표시: ☑, Index, Status, Main Img, NIR Img
- 카메라 이미지는 생략하거나 툴팁으로 제공
- 장점: 더 넓은 화면 활용
- 단점: 일부 정보 숨김

**권장**: **UserControl 기반 재사용** (피드백 반영)
- `FileGroupDataGrid` UserControl을 생성하여 Line 1, Line 2, Combined 탭에서 공통 사용
- 컬럼 정의 불일치 리스크 제거 및 유지보수성 향상

---

## 5. 구현 계획

### 5.1 Phase 1: 기본 구조 구현

#### Step 1.1: FileGroupDataGrid UserControl 생성
- `ChronoView/UI/Controls/FileGroupDataGrid.xaml` 생성
- 기존 DataGrid의 Style, Columns, Behavior를 모두 이동
- DependencyProperty 정의: `ItemsSource`, `SelectedGroup`

#### Step 1.2: MainWindow.xaml 리팩토링
- Line 1 탭: `FileGroupDataGrid`로 교체
- Line 2 탭: `FileGroupDataGrid`로 교체

#### Step 1.3: Combined 탭 구현
- Grid 레이아웃 구성 (헤더, Border 포함)
- 좌측: Line 1용 `FileGroupDataGrid` 배치
- 우측: Line 2용 `FileGroupDataGrid` 배치

#### Step 1.4: GridSplitter 개선
- ✅ GridSplitter 이미 존재
- 시각적 피드백 개선 (호버 효과)

### 5.2 Phase 2: ViewModel 연동 확인

#### Step 2.1: ActiveTabIndex 처리
- Combined 탭의 인덱스는 2번
- Combined 탭의 인덱스는 2번
- `GetSelectedGroups()` 메서드 확인 및 보완:
  ```csharp
  // MainWindowViewModel.cs
  public IReadOnlyList<FileGroupViewModel> GetSelectedGroups()
  {
      return ActiveTabIndex switch
      {
          0 => Line1Groups.Where(g => g.IsSelected).ToList(),
          1 => Line2Groups.Where(g => g.IsSelected).ToList(),
          // Combined: 두 라인의 선택 항목 합산 (또는 FileGroups 전체 사용)
          2 => Line1Groups.Concat(Line2Groups).Where(g => g.IsSelected).ToList(),
          _ => FileGroups.Where(g => g.IsSelected).ToList()
      };
  }
  ```
- **확인됨**: 현재 구현은 `_ => FileGroups`로 되어 있어 기능상 문제는 없으나, 명시적으로 `Combined` 케이스를 추가하여 의도를 명확히 할 것을 권장.

#### Step 2.2: 선택 상태 동기화
- 독립 탭에서 선택 → Combined 탭에서도 선택 표시 (자동, IsSelected 바인딩)
- Combined 탭에서 선택 → 독립 탭에서도 선택 표시 (자동)

### 5.3 Phase 3: 작업 버튼 동작 정의

#### 정책 A: 모든 선택 항목 처리 (권장)
```csharp
// Combined 탭에서는 Line1과 Line2의 선택 항목 모두 처리
var selectedGroups = Line1Groups.Concat(Line2Groups)
                                 .Where(g => g.IsSelected)
                                 .ToList();
```

**장점**: 직관적, 사용자가 두 라인의 항목을 한 번에 처리 가능
**단점**: 의도치 않은 다중 라인 작업 가능성

#### 정책 B: 현재 포커스된 DataGrid만 처리
```csharp
// Keyboard.FocusedElement 또는 마지막 클릭된 DataGrid 추적
var focusedGrid = GetFocusedDataGrid();
var selectedGroups = focusedGrid == _line1Grid
    ? Line1Groups.Where(g => g.IsSelected)
    : Line2Groups.Where(g => g.IsSelected);
```

**장점**: 명확한 작업 대상
**단점**: 구현 복잡도 증가

**권장**: 정책 A (모든 선택 항목 처리) - 단순하고 직관적

---

## 6. 기술적 고려사항

### 6.1 컬럼 정의 재사용

**문제**: Line 1과 Line 2 DataGrid가 동일한 컬럼 구조를 사용하는데, XAML에서 중복 정의 발생

**해결 방안 1: 코드 복사** (가장 간단)
- 각 DataGrid에서 컬럼 정의 복사
- 장점: 구현 간단, 독립적인 수정 가능
- 단점: 유지보수 시 세 곳을 모두 수정해야 함

**해결 방안 2: UserControl 분리 (권장)**
- `FileGroupDataGrid.xaml` UserControl 생성
- Line1/Line2/Combined에서 재사용
- **장점**: 완벽한 재사용, 단일 수정 지점, 컬럼 불일치 리스크 0%
- **단점**: UserControl 생성 및 DP 연결 코딩 필요

**해결 방안 3: DataTemplate Resource**
- DataGrid 전체를 DataTemplate으로 정의
- **장점**: XAML 내에서 해결 가능
- **단점**: SelectedItem 바인딩 등 DP 처리가 까다로움

**권장**: **UserControl 분리** - 피드백에 따라 가장 견고한 방식 채택

### 6.2 선택 상태 충돌 방지

**시나리오**: 사용자가 Line 1 탭에서 그룹 선택 → Combined 탭으로 전환

**현재 동작**:
- `IsSelected` 속성이 ViewModel에 저장되므로 자동 동기화됨
- 추가 작업 불필요

### 6.3 성능 최적화

**잠재적 문제**: Combined 탭에서 두 개의 DataGrid를 동시에 렌더링

**최적화 방안**:
1. **UI Virtualization 활성화** (기본값, 확인만 필요)
2. **이미지 로딩 지연**: 보이는 행만 썸네일 로드
3. **대용량 데이터 처리**: 각 라인 500개 이상 시 성능 테스트

---

## 7. 테스트 계획

### 7.1 기능 테스트

| 테스트 항목 | 예상 결과 | 우선순위 |
|-----------|-----------|---------|
| Combined 탭 전환 | Line 1/Line 2 DataGrid 정상 표시 | 높음 |
| GridSplitter 드래그 | 실시간 크기 조절 | 중간 |
| Line 1 선택 | 체크박스 체크, IsSelected 업데이트 | 높음 |
| Line 2 선택 | 체크박스 체크, IsSelected 업데이트 | 높음 |
| 독립 탭 → Combined 탭 | 선택 상태 유지 | 높음 |
| Combined 탭 → 독립 탭 | 선택 상태 유지 | 높음 |
| DragSelectBehavior (Line 1) | 드래그로 다중 선택 | 중간 |
| DragSelectBehavior (Line 2) | 드래그로 다중 선택 | 중간 |
| Move 버튼 (Combined) | 두 라인의 선택 항목 모두 이동 | 높음 |
| Delete 버튼 (Combined) | 두 라인의 선택 항목 모두 삭제 | 높음 |
| Abnormal 하이라이트 | 양쪽 DataGrid에서 정상 표시 | 중간 |

### 7.2 성능 테스트

| 테스트 시나리오 | 측정 항목 | 목표 |
|---------------|----------|-----|
| 각 라인 100개 그룹 | 탭 전환 속도 | <100ms |
| 각 라인 500개 그룹 | 스크롤 부드러움 | >30 FPS |
| 각 라인 1000개 그룹 | 메모리 사용량 | <500MB 증가 |
| GridSplitter 드래그 | 반응 지연 | <16ms |

### 7.3 사용성 테스트

- [ ] 사용자가 Combined 탭의 목적을 직관적으로 이해하는가?
- [ ] GridSplitter가 드래그 가능한 것을 알 수 있는가?
- [ ] 두 DataGrid의 구분이 명확한가?
- [ ] Move/Delete 작업 시 어느 항목이 처리되는지 명확한가?

---

## 8. 구현 우선순위 및 일정

### 8.1 우선순위

**낮음** - Combined 탭은 편의 기능이며, Line 1/Line 2 독립 탭으로 모든 기능 수행 가능

### 8.2 예상 소요 시간

| Phase | 작업 | 예상 시간 |
|-------|------|----------|
| Phase 1 | XAML 구조 구현 | 1-2시간 |
| Phase 2 | ViewModel 연동 | 30분-1시간 |
| Phase 3 | 작업 버튼 동작 | 30분-1시간 |
| **Total** | | **2-4시간** |

### 8.3 의존성

- ✅ Task 4.1: DragSelectBehavior 구현됨
- ✅ Task 4.3: ProgressBar 및 바인딩 완료됨
- ✅ Phase 1: ViewModel 인프라 구현됨

**차단 요소 없음** - 즉시 구현 가능

---

## 9. 대안 및 향후 개선

### 9.1 대안: Combined 탭 생략

**근거**:
- Line 1/Line 2 독립 탭으로 모든 작업 가능
- 사용 빈도가 높지 않을 것으로 예상
- 추가 개발/테스트 비용

**권장 사항**: MVP(Minimum Viable Product)에서는 생략하고, 사용자 피드백 후 추가 고려

### 9.2 향후 개선

#### 개선 1: 탭별 통계 표시
```
┌─────────────────────┬─────────────────────┐
│ Line 1: 150 Groups  │ Line 2: 120 Groups  │
│ Match: 95%          │ Match: 92%          │
└─────────────────────┴─────────────────────┘
```

#### 개선 2: 동기화 스크롤
- 사용자가 Line 1을 스크롤하면 Line 2도 동일 비율로 스크롤
- 토글 버튼으로 활성화/비활성화

#### 개선 3: 비교 모드
- 동일한 타임스탬프의 그룹을 색상으로 연결 표시
- Line 1의 그룹과 Line 2의 대응 그룹 하이라이트

---

## 10. 결론 및 권장사항

### 10.1 구현 여부 결정

**권장**: 현재 단계에서는 **구현 보류**

**이유**:
1. Line 1/Line 2 독립 탭으로 모든 기능 수행 가능
2. 우선순위가 높은 Task 5 (테스트 및 검증)가 남아있음
3. 실제 사용자 피드백 없이 필요성 불명확

**대신 수행할 작업**:
- Task 5: 단위 테스트 및 통합 테스트 작성
- 실제 운영 환경에서 사용자 피드백 수집
- Combined 탭 필요성 검증 후 재결정

### 10.2 구현 시 순서

만약 Combined 탭 구현이 결정되면:

1. **MainWindow.xaml 수정** (1-2시간)
   - Line 1 DataGrid 추가
   - Line 2 DataGrid 추가
   - 헤더 및 Border 추가

2. **MainWindowViewModel 수정** (30분-1시간)
   - `GetSelectedGroups()` 메서드에 Combined 탭 처리 추가

3. **테스트** (1-2시간)
   - 기능 테스트 수행
   - 성능 측정
   - 사용성 검증

4. **문서 작성** (30분)
   - 구현 보고서 작성
   - tasks.md 업데이트

**Total**: 3-5.5시간

---

## 11. 관련 파일

| 파일 경로 | 수정 여부 | 역할 |
|----------|----------|------|
| [ChronoView/MainWindow.xaml](../../ChronoView/MainWindow.xaml) | 수정 필요 | Combined 탭 DataGrid 추가 |
| [ChronoView/UI/ViewModels/MainWindowViewModel.cs](../../ChronoView/UI/ViewModels/MainWindowViewModel.cs) | 수정 필요 | `GetSelectedGroups()` 로직 수정 |
| [ChronoView/UI/Behaviors/DragSelectBehavior.cs](../../ChronoView/UI/Behaviors/DragSelectBehavior.cs) | 변경 없음 | 재사용 |
| [docs/spec/ui-service-integration-gaps/tasks.md](../tasks.md) | 업데이트 필요 | Task 4.5 상태 반영 |

---

## 12. 변경 이력

| 날짜 | 작업자 | 변경 내용 |
|-----|--------|----------|
| 2025-12-11 | Claude Code | Task 4.5 구현 계획 작성 (구현 보류 권장) |
