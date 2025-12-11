# NIR Visualization - Technical Overview

## 📋 현재 상황 (Before)

### NIR 데이터 구조
- **파일 형식**: `.spc` (바이너리) + `.txt` (텍스트 스펙트럼 데이터)
- **파일 위치**: `C:\workspace\seaweed\data\20251204\2021_A014\with NIR\Nir\`
  - 예시: `run_120251204T111028.spc` + `run_120251204T111028A.txt`

### .txt 파일 내용 예시
```
#时间: 2025-12-04 11:10:28
#XUnit: cm-1
4000.00    0.043478
4003.06    0.042891
4006.11    0.041234
...
9994.16   -0.244920
```
- Wavelength (파장, cm⁻¹) 와 Intensity (강도) 의 쌍
- ~1951개 데이터 포인트

### 문제점
**NIR는 이미지가 아니라 숫자 데이터!**
- `FileGroupViewModel`에 `_nirImageThumbnail` 속성은 있지만 **비어있음**
- DataGrid의 NIR 칼럼에 **표시할 이미지가 없음**

---

## 🎯 작업 목표 (After)

### 목표: .txt 파일 → 그래프 이미지로 변환

**데이터 → 시각화 파이프라인:**

```
┌─────────────────────────────────────────────────────────────┐
│ Step 1: .txt 파일 읽기                                       │
├─────────────────────────────────────────────────────────────┤
│ run_120251204T111028A.txt                                   │
│ ├─ Header (#으로 시작) 스킵                                  │
│ └─ 4000.00  0.043478  ← Wavelength, Intensity 파싱          │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 2: 스펙트럼 그래프 생성 (ScottPlot)                     │
├─────────────────────────────────────────────────────────────┤
│ NirGraphGenerator.GenerateGraph()                           │
│ ├─ X축: Wavelength (4000~9994 cm⁻¹)                        │
│ ├─ Y축: Intensity (-0.24 ~ 0.04)                           │
│ ├─ 스타일: 파란선 (#0078d4), 회색배경 (#f5f5f5)              │
│ └─ 출력: BitmapSource (250x100 픽셀, 가로로 긴 형태)         │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 3: ViewModel에 바인딩                                   │
├─────────────────────────────────────────────────────────────┤
│ FileGroupViewModel.NirGraphThumbnail = [BitmapSource]       │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│ Step 4: DataGrid에 표시                                      │
├─────────────────────────────────────────────────────────────┤
│ <Image Source="{Binding NirGraphThumbnail}" />             │
│ → 사용자는 NIR 스펙트럼을 시각적으로 확인 가능! 🎉            │
└─────────────────────────────────────────────────────────────┘
```

---

## 📐 크기 설정 (가로로 긴 형태)

### 왜 가로로 긴 형태?
- 스펙트럼 그래프는 **X축(파장)이 넓은 범위** (4000~9994)
- 세로로 긴 형태는 시각적으로 정보 손실이 큼
- 가로로 긴 형태 (250x100)가 스펙트럼 표현에 적합

### 크기 비교
| 항목 | 기본 이미지 썸네일 | NIR 그래프 썸네일 |
|------|-------------------|------------------|
| Width | 120 px | **250 px** |
| Height | 90 px | **100 px** |
| 비율 | 4:3 (정방형에 가까움) | **5:2 (가로로 긴 형태)** |

---

## 🔧 Phase 3에서 할 일

### 1. `FileGroupViewModel.cs` 수정
```csharp
// 이미 선언되어 있음 ✅
private BitmapSource? _nirGraphThumbnail;

// 추가할 Public Property
public BitmapSource? NirGraphThumbnail
{
    get => _nirGraphThumbnail;
    set => SetProperty(ref _nirGraphThumbnail, value);
}
```

### 2. `LoadThumbnailsAsync()` 수정
```csharp
// .spc 파일에서 .txt 파일 경로 찾기
if (HasNir && Config.MatchingSettings.EnableNirGraph)
{
    var txtPath = ResolveTxtPath(_fileGroup.NirFilePath);
    if (txtPath != null)
    {
        // 파싱 + 그래프 생성
        var spectrum = NirSpectrumParser.Parse(txtPath);
        var graph = NirGraphGenerator.GenerateGraph(
            spectrum, 
            Config.UISettings.NirThumbnailWidth,   // 250
            Config.UISettings.NirThumbnailHeight   // 100
        );
        graph.Freeze();  // UI thread 안전성
        NirGraphThumbnail = graph;
    }
}
```

### 3. `.txt` 파일 매칭 로직
```csharp
string? ResolveTxtPath(string spcPath)
{
    // 1. 우선순위: run_XXX.spc → run_XXXA.txt
    var txtWithA = Path.ChangeExtension(spcPath, null) + "A.txt";
    if (File.Exists(txtWithA)) return txtWithA;
    
    // 2. Fallback: run_XXX.spc → run_XXX.txt
    var txtDirect = Path.ChangeExtension(spcPath, ".txt");
    if (File.Exists(txtDirect)) return txtDirect;
    
    return null;
}
```

---

## 📊 최종 결과 미리보기

### DataGrid에 표시될 모습
| Timestamp | Main Img | **NIR Graph** | Cam1 | Cam2 | ... |
|-----------|----------|--------------|------|------|-----|
| 11:10:28 | 🖼️ | **📈 [스펙트럼 그래프]** | 🖼️ | 🖼️ | ... |

**NIR Graph 칼럼:**
- 기존: 빈 칸 (이미지 없음)
- 변경 후: 파란색 스펙트럼 곡선 표시 (250x100px)

---

## ✅ 요약

| 항목 | 설명 |
|------|------|
| **현재 문제** | NIR 데이터는 텍스트 → 이미지가 없어서 썸네일 비어있음 |
| **해결 방법** | `.txt` 파싱 → ScottPlot으로 그래프 생성 → BitmapSource |
| **Phase 1** | Core Logic (Parser, Generator) ✅ 완료 |
| **Phase 2** | Configuration (크기 설정) ✅ 완료 |
| **Phase 3 (현재)** | ViewModel 연결 + UI 바인딩 |
| **최종 결과** | DataGrid NIR 칼럼에 스펙트럼 그래프 표시 |
