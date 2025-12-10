# Task 4.4: Line2 탭 구현 - 검증 보고서

## 개요

**작업일자**: 2025-12-11
**작업명**: Task 4.4 - Line2 탭 구현 검증
**상태**: ✅ 대부분 완료 (구조적 오류 발견)

---

## 1. 검증 결과 요약

### 1.1 구현 완료 항목 ✅

- [x] DataGrid 구조 존재
- [x] `ItemsSource="{Binding Line2Groups}"` 바인딩
- [x] `SelectedItem="{Binding SelectedLine2Group}"` 바인딩
- [x] `FileGroupDataGridStyle` 적용
- [x] `FileGroupRowStyle` 적용 (IsSelected 바인딩 포함)
- [x] DragSelectBehavior 연결
- [x] CheckBoxColumn with IsSelected 바인딩
- [x] Index, Status 텍스트 컬럼
- [x] Main Img, NIR Img 템플릿 컬럼
- [x] Camera 4, 5, 6 템플릿 컬럼

### 1.2 발견된 문제 ❌

**Line 466-483: Main Img 컬럼에 중복 Border/Grid 요소**

**문제 코드**:
```xml
<DataGridTemplateColumn Header="Main Img" Width="*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border ... Background="#3a3a3a" ...>
                <Grid>
                    <Image Source="{Binding MainImageThumbnail}" .../>
            <!-- ❌ 여기서 Grid와 Border가 닫히지 않고 다시 시작됨 -->
            <Border ... Background="#3a3a3a" ...>
                <Grid>
                    <Image Source="{Binding NirImageThumbnail}" .../>
                </Grid>
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**영향**:
- XAML 파서 오류 가능성
- Main Img 컬럼에 NIR 이미지가 표시될 수 있음
- 레이아웃 깨짐

---

## 2. Line 1과 Line 2 구조 비교

### 2.1 Line 1 DataGrid (정상)

**위치**: Lines 365-452

**구조**:
```xml
<TabItem Header="Line 1">
    <DataGrid ItemsSource="{Binding Line1Groups}"
              SelectedItem="{Binding SelectedLine1Group}"
              Style="{StaticResource FileGroupDataGridStyle}"
              RowStyle="{StaticResource FileGroupRowStyle}">
        <i:Interaction.Behaviors>
            <behaviors:DragSelectBehavior/>
        </i:Interaction.Behaviors>
        <DataGrid.Columns>
            <DataGridCheckBoxColumn ... Binding="{Binding IsSelected}"/>
            <DataGridTextColumn Header="Index" ... Binding="{Binding GroupId}"/>
            <DataGridTextColumn Header="Status" ... Binding="{Binding StatusText}"/>
            <DataGridTemplateColumn Header="Main Img">
                <DataTemplate>
                    <Border ...>
                        <Grid>
                            <Image Source="{Binding MainImageThumbnail}"/>
                            <ProgressBar ... Visibility="..."/>
                        </Grid>
                    </Border>
                </DataTemplate>
            </DataGridTemplateColumn>
            <DataGridTemplateColumn Header="NIR Img">
                <!-- 유사 구조 -->
            </DataGridTemplateColumn>
            <DataGridTemplateColumn Header="Cam 1">
                <!-- 유사 구조 -->
            </DataGridTemplateColumn>
            <DataGridTemplateColumn Header="Cam 2">
                <!-- 유사 구조 -->
            </DataGridTemplateColumn>
            <DataGridTemplateColumn Header="Cam 3">
                <!-- 유사 구조 -->
            </DataGridTemplateColumn>
        </DataGrid.Columns>
    </DataGrid>
</TabItem>
```

**컬럼 목록**:
1. CheckBox (IsSelected)
2. Index (GroupId)
3. Status (StatusText)
4. Main Img (MainImageThumbnail)
5. NIR Img (NirImageThumbnail)
6. Cam 1 (Camera1Thumbnail)
7. Cam 2 (Camera2Thumbnail)
8. Cam 3 (Camera3Thumbnail)

**카메라 범위**: Camera1, Camera2, Camera3 (Line 1용)

---

### 2.2 Line 2 DataGrid (구조적 오류 있음)

**위치**: Lines 454-524

**구조** (문제 부분):
```xml
<TabItem Header="Line 2">
    <DataGrid ItemsSource="{Binding Line2Groups}"
              SelectedItem="{Binding SelectedLine2Group}"
              Style="{StaticResource FileGroupDataGridStyle}"
              RowStyle="{StaticResource FileGroupRowStyle}">
        <i:Interaction.Behaviors>
            <behaviors:DragSelectBehavior/>
        </i:Interaction.Behaviors>
        <DataGrid.Columns>
            <DataGridCheckBoxColumn ... Binding="{Binding IsSelected}"/>
            <DataGridTextColumn Header="Index" ... Binding="{Binding GroupId}"/>
            <DataGridTextColumn Header="Status" ... Binding="{Binding StatusText}"/>

            <!-- ❌ Main Img 컬럼에 오류 -->
            <DataGridTemplateColumn Header="Main Img">
                <DataTemplate>
                    <Border ...>
                        <Grid>
                            <Image Source="{Binding MainImageThumbnail}"/>
                    <!-- ❌ 닫히지 않음, 다음 Border가 시작됨 -->
                    <Border ...>
                        <Grid>
                            <Image Source="{Binding NirImageThumbnail}"/>
                        </Grid>
                    </Border>
                </DataTemplate>
            </DataGridTemplateColumn>

            <DataGridTemplateColumn Header="Cam 4">
                <!-- 정상 -->
            </DataGridTemplateColumn>
            <DataGridTemplateColumn Header="Cam 5">
                <!-- 정상 -->
            </DataGridTemplateColumn>
            <DataGridTemplateColumn Header="Cam 6">
                <!-- 정상 -->
            </DataGridTemplateColumn>
        </DataGrid.Columns>
    </DataGrid>
</TabItem>
```

**컬럼 목록** (의도):
1. CheckBox (IsSelected)
2. Index (GroupId)
3. Status (StatusText)
4. ❌ Main Img (구조 오류)
5. ❓ NIR Img (누락됨, Main Img에 혼재)
6. Cam 4 (Camera4Thumbnail)
7. Cam 5 (Camera5Thumbnail)
8. Cam 6 (Camera6Thumbnail)

**카메라 범위**: Camera4, Camera5, Camera6 (Line 2용) ✅

---

## 3. 수정 필요 사항

### 3.1 Main Img 컬럼 수정

**현재 코드** (Lines 466-483):
```xml
<DataGridTemplateColumn Header="Main Img" Width="*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border Width="{Binding DataContext.DisplayImageWidth, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=120}"
                    Height="{Binding DataContext.DisplayImageHeight, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=90}"
                    Background="#3a3a3a" BorderBrush="#505050" BorderThickness="1">
                <Grid>
                    <Image Source="{Binding MainImageThumbnail}" Stretch="Uniform"/>
            <Border Width="{Binding DataContext.DisplayImageWidth, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=120}"
                    Height="{Binding DataContext.DisplayImageHeight, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=90}"
                    Background="#3a3a3a" BorderBrush="#505050" BorderThickness="1">
                <Grid>
                    <Image Source="{Binding NirImageThumbnail}" Stretch="Uniform"/>
                </Grid>
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

**수정 후 코드** (Main Img 컬럼):
```xml
<DataGridTemplateColumn Header="Main Img" Width="*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border Width="{Binding DataContext.DisplayImageWidth, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=120}"
                    Height="{Binding DataContext.DisplayImageHeight, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=90}"
                    Background="#3a3a3a" BorderBrush="#505050" BorderThickness="1">
                <Grid>
                    <Image Source="{Binding MainImageThumbnail}" Stretch="Uniform"/>
                </Grid>
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

### 3.2 NIR Img 컬럼 추가

**추가할 코드** (Main Img 컬럼 다음):
```xml
<DataGridTemplateColumn Header="NIR Img" Width="*">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <Border Width="{Binding DataContext.DisplayImageWidth, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=120}"
                    Height="{Binding DataContext.DisplayImageHeight, RelativeSource={RelativeSource AncestorType=Window}, FallbackValue=90}"
                    Background="#3a3a3a" BorderBrush="#505050" BorderThickness="1">
                <Grid>
                    <Image Source="{Binding NirImageThumbnail}" Stretch="Uniform"/>
                </Grid>
            </Border>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

---

## 4. 수정 작업 요약

### 4.1 수정 범위

**파일**: `ChronoView/MainWindow.xaml`
**시작 라인**: 466
**종료 라인**: 483

### 4.2 작업 단계

1. **Lines 466-483 제거**: 잘못된 Main Img 컬럼 정의 삭제
2. **Main Img 컬럼 추가**: 올바른 구조로 재작성
3. **NIR Img 컬럼 추가**: 누락된 NIR 컬럼 추가
4. **빌드 검증**: XAML 파서 오류 해결 확인
5. **런타임 테스트**: Line 2 탭에서 이미지 정상 표시 확인

---

## 5. 예상 결과

### 5.1 수정 후 컬럼 구조

**Line 2 DataGrid 최종 컬럼**:
1. ☑ CheckBox (IsSelected)
2. Index (GroupId)
3. Status (StatusText)
4. ✅ Main Img (MainImageThumbnail) - 수정됨
5. ✅ NIR Img (NirImageThumbnail) - 추가됨
6. Cam 4 (Camera4Thumbnail)
7. Cam 5 (Camera5Thumbnail)
8. Cam 6 (Camera6Thumbnail)

**Total**: 8개 컬럼 (Line 1과 동일한 수)

### 5.2 기능 검증

수정 후 다음 기능들이 정상 작동해야 함:

- [ ] Line 2 탭 클릭 시 DataGrid 정상 표시
- [ ] Main Img 컬럼에 MainImageThumbnail 표시
- [ ] NIR Img 컬럼에 NirImageThumbnail 표시
- [ ] Camera 4, 5, 6 썸네일 정상 표시
- [ ] 체크박스 선택 시 IsSelected 업데이트
- [ ] DragSelectBehavior로 다중 선택
- [ ] Abnormal 그룹 노란색 하이라이트
- [ ] Move/Delete 버튼 작동

---

## 6. Line 1과 Line 2의 차이점

| 항목 | Line 1 | Line 2 |
|-----|--------|--------|
| DataGrid 바인딩 | `Line1Groups` | `Line2Groups` |
| SelectedItem 바인딩 | `SelectedLine1Group` | `SelectedLine2Group` |
| 카메라 컬럼 | Cam 1, 2, 3 | Cam 4, 5, 6 |
| 카메라 바인딩 | `Camera1/2/3Thumbnail` | `Camera4/5/6Thumbnail` |
| 기타 컬럼 | 동일 | 동일 |
| DragSelectBehavior | ✅ | ✅ |
| IsSelected 바인딩 | ✅ | ✅ |

---

## 7. 완료 체크리스트

### 7.1 구현 항목 (tasks.md 기준)

- [x] Line1과 동일한 DataGrid 구조 복제 (구조적 오류 발견)
- [x] `ItemsSource="{Binding Line2Groups}"` 바인딩
- [x] `SelectedItem="{Binding SelectedLine2Group}"` 바인딩
- [x] DragSelectBehavior 연결
- [ ] **Main Img / NIR Img 컬럼 수정 필요** ⚠️

### 7.2 수정 후 완료 예정

- [ ] Main Img 컬럼 수정 완료
- [ ] NIR Img 컬럼 추가 완료
- [ ] 빌드 성공 확인
- [ ] 런타임 테스트 완료
- [ ] tasks.md 업데이트 (Task 4.4 ✅ COMPLETED)

---

## 8. 관련 파일

| 파일 경로 | 현재 상태 | 수정 필요 |
|----------|----------|----------|
| [ChronoView/MainWindow.xaml](../../ChronoView/MainWindow.xaml) | 구조적 오류 있음 | ✅ 수정 필요 |
| [ChronoView/UI/ViewModels/MainWindowViewModel.cs](../../ChronoView/UI/ViewModels/MainWindowViewModel.cs) | 정상 | 변경 없음 |
| [ChronoView/UI/ViewModels/FileGroupViewModel.cs](../../ChronoView/UI/ViewModels/FileGroupViewModel.cs) | 정상 | 변경 없음 |
| [ChronoView/UI/Behaviors/DragSelectBehavior.cs](../../ChronoView/UI/Behaviors/DragSelectBehavior.cs) | 정상 | 변경 없음 |
| [docs/spec/ui-service-integration-gaps/tasks.md](../tasks.md) | Task 4.4 미완료 | 수정 후 ✅ 표시 |

---

## 9. 다음 단계

1. **즉시**: MainWindow.xaml의 Line 2 DataGrid 수정
2. **빌드 검증**: 오류 없이 빌드되는지 확인
3. **런타임 테스트**: 실제 Line 2 탭에서 이미지 정상 표시 확인
4. **tasks.md 업데이트**: Task 4.4를 ✅ COMPLETED로 표시
5. **Task 4.5**: Combined 탭 구현 여부 결정 (계획 문서 참조)

---

## 10. 변경 이력

| 날짜 | 작업자 | 변경 내용 |
|-----|--------|----------|
| 2025-12-11 | Claude Code | Task 4.4 검증 및 구조적 오류 발견 |
