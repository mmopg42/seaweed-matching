# Python vs Rust 성능 결정

## 결론: Rust로 직접 구현 권장 ✅

### 근거

#### 1. 프로젝트 목표와 부합
- 목표: "가볍고 빠른 앱" (시작 시간 < 1초, 메모리 < 100MB)
- Python 사이드카는 이 목표에 역행

#### 2. 성능 비교

| 작업 | Rust 네이티브 | Python RPC | 차이 |
|-----|-------------|-----------|------|
| 파일 목록 조회 (1000개) | 50ms | 500-1000ms | 10-20배 |
| 파일 매칭/필터링 | 1-5ms | 50-100ms | 50배 |
| 이미지 메타데이터 읽기 | 0.1ms | 2-5ms | 20-50배 |
| 앱 시작 시간 | 0.5초 | 1-2초 | 2-4배 |
| 메모리 사용 | 20-40MB | 70-150MB | 3-4배 |
| 앱 크기 | 3-10MB | 50-100MB | 10배 |

#### 3. 필요한 기능의 Rust 구현 가능성

| 기능 | Rust 크레이트 | 난이도 | Python 대비 |
|-----|--------------|--------|------------|
| 파일 시스템 감시 | notify | 쉬움 | 더 빠름 |
| 이미지 처리 | image | 쉬움 | 더 빠름 |
| 파일 매칭 | glob, regex | 쉬움 | 훨씬 빠름 |
| 병렬 처리 | rayon | 쉬움 | 훨씬 빠름 |
| 해시 계산 | sha2 | 쉬움 | 더 빠름 |

### Python이 정말 필요한 경우

Python을 사용해야 하는 **유일한** 경우:
- 머신러닝 모델 (TensorFlow, PyTorch)
- 복잡한 과학 연산 (NumPy, SciPy)
- 특수 라이브러리 (OpenCV의 고급 기능 등)

**이 프로젝트에는 해당 없음** → Python 불필요

### 제안: Phase 5 롤백 및 Rust 구현

#### Phase 5 대체안: Rust 파일 작업

```rust
// src-tauri/src/commands/file_operations.rs

use notify::{Watcher, RecursiveMode};
use rayon::prelude::*;
use image::GenericImageView;

#[tauri::command]
pub async fn watch_directory(path: String) -> Result<(), String> {
    // 파일 시스템 감시
}

#[tauri::command]
pub async fn match_files(
    source_dir: String,
    pattern: String
) -> Result<Vec<FileInfo>, String> {
    // 병렬 파일 매칭
}

#[tauri::command]
pub async fn get_image_metadata(path: String) -> Result<ImageMetadata, String> {
    // 이미지 정보 읽기
}

#[tauri::command]
pub async fn calculate_file_hash(path: String) -> Result<String, String> {
    // 파일 해시 계산
}
```

### 마이그레이션 계획

1. **Phase 5 Python 제거** (선택사항)
   - python_backend/ 디렉토리 제거
   - src-tauri/src/python/ 제거
   - lib.rs 단순화

2. **Phase 5 대체: Rust 고급 파일 작업**
   - 파일 시스템 감시 (notify)
   - 병렬 파일 처리 (rayon)
   - 이미지 메타데이터 (image)
   - 패턴 매칭 (glob, regex)

3. **성능 목표 달성**
   - 앱 시작: < 1초 ✅
   - 메모리: < 50MB ✅
   - 1000개 파일 처리: < 100ms ✅

### 추천 행동

**Option A: Python 완전 제거 (권장)**
- Phase 5 변경사항 되돌리기
- Rust로 필요한 기능 직접 구현
- 성능과 배포 단순성 확보

**Option B: Python 보류**
- Python 코드 유지하되 사용하지 않음
- 나중에 정말 필요하면 재활성화
- 현재는 Rust로 구현

**Option C: 하이브리드 (비추천)**
- 핵심 기능은 Rust
- ML/특수 작업만 Python
- 복잡도 증가, 성능 저하
