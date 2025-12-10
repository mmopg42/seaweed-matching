# Task 4.5 Combined 탭 구현 - 요약

## 상태

**📋 계획 단계** - 구현 보류 권장

---

## 핵심 결론

### ✋ 구현 보류 권장

**이유**:
1. **기능 중복**: Line 1/Line 2 독립 탭으로 모든 작업 수행 가능
2. **우선순위**: Task 5 (테스트 및 검증)이 더 중요
3. **불확실성**: 실제 사용자 피드백 없이 필요성 검증 불가
4. **시간 투자**: 3-5.5시간 소요 예상 (다른 중요 작업에 투자 가능)

---

## Combined 탭이란?

Line 1과 Line 2의 파일 그룹을 **좌우로 나란히** 동시에 표시하는 탭

```
┌────────────────────────────┬─┬─────────────────────────────┐
│       Line 1 DataGrid      │S│       Line 2 DataGrid       │
│  ┌──┬─────┬────────┬─────┐ │P│  ┌──┬─────┬────────┬─────┐  │
│  │☑│Index│Status  │Main │ │L│  │☑│Index│Status  │Main │  │
│  ├──┼─────┼────────┼─────┤ │I│  ├──┼─────┼────────┼─────┤  │
│  │☑│  1  │Complete│[img]│ │T│  │☐│ 101 │Complete│[img]│  │
│  │☐│  2  │Partial │[img]│ │T│  │☐│ 102 │Waiting │[img]│  │
└────────────────────────────┴─┴─────────────────────────────┘
```

---

## 사용 시나리오

1. **생산 라인 동시 모니터링**: 두 라인 상태를 한 눈에 비교
2. **라인 간 비교**: 처리 속도, 매칭률, 실패율 동시 확인
3. **다중 라인 작업**: 각 라인에서 선택 후 독립적인 작업 수행

---

## 구현 계획 (실행 시)

### Phase 1: 기본 구조 (1-2시간)
- Line 1 DataGrid 추가 (전체 컬럼)
- Line 2 DataGrid 추가 (전체 컬럼)
- GridSplitter 개선
- 헤더 및 Border 추가

### Phase 2: ViewModel 연동 (30분-1시간)
```csharp
private IEnumerable<FileGroupViewModel> GetSelectedGroups()
{
    var groups = ActiveTabIndex switch
    {
        0 => Line1Groups,
        1 => Line2Groups,
        2 => Line1Groups.Concat(Line2Groups),  // 두 라인 모두
        _ => Enumerable.Empty<FileGroupViewModel>()
    };
    return groups.Where(g => g.IsSelected);
}
```

### Phase 3: 작업 버튼 동작 (30분-1시간)
- **정책 A** (권장): 두 라인의 모든 선택 항목 처리
- **정책 B**: 현재 포커스된 DataGrid만 처리

**Total**: 2-4시간

---

## 현재 상태

```xml
<TabItem Header="Combined">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="5"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        <TextBlock Grid.Column="0" Text="Line 1" .../>
        <GridSplitter Grid.Column="1" .../>
        <TextBlock Grid.Column="2" Text="Line 2" .../>
    </Grid>
</TabItem>
```

- ✅ Grid 레이아웃 구조 존재
- ✅ GridSplitter 준비됨
- ❌ DataGrid 미구현

---

## 의사결정 기준

### 구현하는 경우
- [ ] 사용자가 명시적으로 Combined 탭 필요성 제기
- [ ] 두 라인 동시 모니터링이 핵심 워크플로우
- [ ] 독립 탭 전환이 불편하다는 피드백 수집

### 보류하는 경우 (현재 권장)
- [x] 독립 탭으로 충분한 기능 제공
- [x] 우선순위 높은 작업 존재 (Task 5: 테스트)
- [x] MVP 범위에서 제외 가능
- [x] 실제 사용 패턴 파악 후 결정

---

## 대체 방안

**현재 추천**:
1. Task 4.4 수정 완료 (Line 2 DataGrid 오류 수정)
2. Task 5 진행 (단위 테스트 및 통합 테스트)
3. 실제 운영 환경에서 사용자 피드백 수집
4. Combined 탭 필요성 검증 후 재결정

---

## 관련 문서

- **상세 계획**: [task4.5-combined-tab-implementation-plan.md](task4.5-combined-tab-implementation-plan.md)
  - 12개 섹션, 완전한 구현 가이드
  - 설계, 코드 예시, 테스트 계획 포함

---

## 권장 사항

### 즉시 수행
1. ✅ Task 4.4 수정 (Line 2 DataGrid 오류 수정) - **우선순위 높음**
2. ✅ Task 5.1-5.3 진행 (Unit/Integration/Manual Tests)
3. ✅ 실제 운영 환경 배포 및 피드백 수집

### 향후 검토
- Combined 탭 필요성 재평가 (3-6개월 후)
- 사용자 요청 시 우선순위 재조정

---

## 변경 이력

| 날짜 | 작업자 | 변경 내용 |
|-----|--------|----------|
| 2025-12-11 | Claude Code | Task 4.5 계획 수립 및 구현 보류 권장 |
