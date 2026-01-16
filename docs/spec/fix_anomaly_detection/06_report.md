---
Task: fix_anomaly_detection
Created: 2026-01-13
Completed: 2026-01-13
Status: Complete
Depends On: All previous spec documents
---

# Fix Anomaly Detection - Implementation Report

## 1. Summary

기존의 불안정했던 Z-Score 기반 이상치 탐지 로직을 **"클린 기준점 중심의 개발 편차 비율(Stable Baseline Percent Deviation)"** 방식으로 전면 교체하였습니다. 이 방식은 기준 이미지 크기(1896x2112)를 안정적으로 유지하면서, 가로나 세로 중 하나라도 12% 이상 차이가 나면 즉시 이상치로 판정합니다.

---

## 2. Goals Assessment

| Goal | Status | Notes |
|------|--------|-------|
| 이상치 미검출 해결 (False Negative) | ✅ Achieved | 1528, 1616 등 이전에 놓쳤던 데이터 완벽 검출 |
| 정상 데이터 오검출 해결 (False Positive) | ✅ Achieved | 1864x1904 등 자연스러운 편차는 정상 판정 |
| 기준점 오염 방지 (Pollution Prevention) | ✅ Achieved | 이상치는 기준점(Median) 계산에서 제외하도록 구현 |
| Std=0 버그 해결 | ✅ Achieved | 편차 비율 방식으로 변경되어 0나누기 문제 해결 |

### Success Criteria Results

| Criterion | Target | Actual | Status |
|-----------|--------|--------|--------|
| Outlier Detection | Detect 1528, 1616, etc. | All detected in simulation | ✅ |
| Baseline Stability | Maintain ~1896 baseline | Clean history keeps it stable | ✅ |
| False Positive | 1864x1904 = Normal | Classified as Normal (<12%) | ✅ |

---

## 3. Implementation Summary

### 3.1 Files Created

| File | Purpose | Lines |
|------|---------|-------|
| `oneoff/test_percent_clean.py` | Final simulation script | ~80 |
| `oneoff/test_stable_final.py` | Production-ready logic verification | ~85 |

### 3.2 Files Modified

| File | Changes |
|------|---------|
| `AbnormalHistoryManager.cs` | `ClearContext` 메서드 추가 (Reset 기능 지원) |
| `AbnormalDetectorService.cs` | Z-score 로직 제거, Percent Deviation 및 Clean Baseline 로직 구현 |

---

## 4. Deviations from Plan/Design

### 4.1 Architecture Changes

| Planned | Actual | Reason |
|---------|--------|--------|
| Robust Z-score (MAD) | **Percent Deviation** | MAD는 데이터가 너무 균일할 경우(0 variation) 극도로 예민해져 실무 적용이 어려움 |
| MinSamples=10 | **MinSamples=5** | 초기 가동 시 더 빨리 기준을 잡도록 소폭 하향 |
| WindowSize=10 | **WindowSize=40** | 기준점의 통계적 안정성을 위해 상향 조정 |
| Adaptive Reset | **Removed** | 사용자 요청으로 자동 초기화 기능 배제 |

---

## 5. Testing Results

### 5.1 Manual Testing (Python Simulation)

```
Key                  | W     MedW  DevW%  | H     MedH  DevH%  | Res       
--------------------------------------------------------------------------------
C260109T184650_0     | 1864  1896  1.7    | 1904  2112  9.8    | Normal
C260109T184705_0     | 1528  1896  19.4   | 1888  2112  10.6   | ABNORMAL
C260109T184707_0     | 1616  1896  14.8   | 1896  2112  10.2   | ABNORMAL
C260109T184724_0     | 1352  1896  28.7   | 1896  2112  10.2   | ABNORMAL
```

**Summary**: 사용자 실제 데이터를 활용한 시뮬레이션에서 모든 이상치가 100% 탐지되었습니다.

---

## 6. Known Limitations

### 6.1 Technical Limitations

| Limitation | Impact | Workaround | Future Fix |
|------------|--------|------------|------------|
| 12% 고정 임계값 | 샘플마다 특성이 다를 수 있음 | UI에서 설정 가능(Threshold) | 샘플링 후 자동 임계값 추천 기능 |

---

## 7. Lessons Learned

### What Went Well
- 사용자의 제안(비율/편차 중심)이 통계적 모델(Z-score)보다 실제 데이터 특성(공장 규격)에 훨씬 더 잘 부합했습니다.
- 통계 모델은 데이터가 '너무 깨끗할 때' 오히려 고장나기 쉽다는 교훈을 얻었습니다.

---

##Approval

**Status**: Complete
**Completion Date**: 2026-01-13
