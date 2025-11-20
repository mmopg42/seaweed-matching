# Prische Matching Codex - Requirements & Missing Features

## 현재 상태 (2025-11-20)

### ❌ 심각한 문제점

1. **메인 기능 누락**: 김 사진을 보고 관리하는 핵심 화면이 없음
2. **Browse 버튼 미동작**: `select_folder` 명령어 미구현
3. **아이콘 미적용**: 빌드된 .exe에 기본 아이콘 표시
4. **앱 종료 문제**: 창 닫기 시 프로세스가 완전히 종료되지 않음
5. **기능 불일치**: Python 원본과 Tauri 구현이 완전히 다름

---

## Python 원본 기능 분석

### 1. 메인 모니터링 화면 (monitoring_app.py)

#### 핵심 기능
- **파일 그룹 관리**
  - Timestamp 기반 파일 그룹핑
  - Normal, Normal2, NIR, NIR2, Cam1-6 파일 매칭
  - 그룹 상태: `Pending`, `Running`, `Completed`, `Error`

- **이미지 뷰어**
  - 썸네일 표시 (비동기 로딩)
  - 클릭 시 원본 크기 미리보기
  - LRU 캐시 (500개)
  - Drag & Drop 선택

- **실시간 모니터링**
  - Watchdog 파일 시스템 감시
  - 디바운스 처리 (설정 가능한 인터벌)
  - 10초 무변화 시 전체 스캔
  - 30초마다 Watchdog 상태 체크

- **파일 개수 카운터**
  - 10개 폴더 실시간 모니터링 (별도 스레드)
  - UI 렉과 완전 독립
  - 5초 간격 자동 갱신

- **파일 작업**
  - 이동/복사 모드
  - 멀티스레드 작업 (최대 8 워커)
  - 충돌 처리 (overwrite/skip)
  - 롤백 기능
  - move_plan.json 생성

#### 상단 툴바 버튼
```python
- 설정: 폴더 경로 설정 다이얼로그
- 설정폴더열기: 소스 폴더 탐색기로 열기
- 이동대상폴더열기: 목적지 폴더 탐색기로 열기
- 경로자동: 날짜 기반 자동 경로 설정
- 시료 폴더 생성: 출력 폴더에 시료별 하위 폴더 생성
- 이미지 불러오기: 강제 이미지 새로고침
- Run: 모니터링 시작
- Stop: 모니터링 중지
- 전체선택/해제: 모든 그룹 선택 토글
- 행삭제: 선택된 그룹 삭제
- 이동: 선택된 그룹 이동/복사 실행
```

#### 통계 바 (2줄)
**첫 번째 줄**: 파일 개수 현황
- NIR, NIR2, Normal, Normal2, Cam1-6 각각의 파일 수

**두 번째 줄**: 그룹 상태
- 총 그룹 수
- Pending, Running, Completed, Error 그룹 수
- 선택된 그룹 수

#### 그룹 테이블
각 행:
- 체크박스
- 타임스탬프
- 상태 아이콘
- NIR, NIR2, Normal, Normal2, Cam1-6 썸네일 (클릭 가능)
- 각 파일의 존재 여부 표시

#### 설정 다이얼로그
```python
- 10개 폴더 경로 (NIR, NIR2, Normal, Normal2, Cam1-6)
- 이동 대상 폴더
- 모니터링 간격 (디바운스)
- 분리 모드 (시료명 1개 vs 2개)
- Auto-move 설정
  - NIR 개수 조건
  - 데이터 개수 조건
```

---

### 2. NIR 모니터링 화면 (nir_app.py)

#### 기능
- NIR 스펙트럼 파일 모니터링
- Y-variation 분석
- 자동 이동/삭제 (김 검출 여부)
- 오래된 파일 정리 (Prune)

---

## 현재 Tauri 구현 상태

### ✅ 구현된 Rust 백엔드 (36개 명령어)

#### 파일 작업
- `list_files` - 파일 목록
- `delete_files` - 파일 삭제
- `move_files` - 파일 이동
- `copy_files` - 파일 복사
- `get_file_info` - 파일 정보

#### 이미지 처리
- `load_image` - 이미지 로드
- `resize_image` - 리사이즈
- `get_image_dimensions` - 크기 정보
- `batch_resize_images` - 배치 리사이즈

#### 설정
- `get_config` - 설정 불러오기
- `set_config` - 설정 저장

#### 파일 매칭 (✅ 구현됨, ❌ 미사용)
- `create_matcher` - 매처 생성
- `add_pattern` - 패턴 추가
- `match_files` - 파일 매칭 실행
- `get_matched_groups` - 매칭 그룹 가져오기

#### 파일 감시
- `start_file_watcher` - 감시 시작
- `stop_file_watcher` - 감시 중지

#### NIR 스펙트럼 (Phase 8.4-8.7)
- `analyze_nir_spectrum` - 스펙트럼 분석
- `process_nir_file` - NIR 파일 처리
- `start_nir_monitoring` - NIR 모니터링
- `prune_nir_files` - 오래된 파일 정리

#### 그룹 관리 (Phase 8.6)
- `create_group` - 그룹 생성
- `delete_group` - 그룹 삭제
- `update_group_metadata` - 메타데이터 업데이트

#### 파일 통계 (Phase 8.7)
- `get_folder_stats` - 10개 폴더 파일 개수

### ❌ 미구현된 기능

#### 1. 파일 다이얼로그
```rust
// ❌ 없음
select_folder() -> Result<String>
select_file(filters) -> Result<String>
open_folder_in_explorer(path: String)
```

#### 2. 메인 화면 UI
- 파일 그룹 테이블
- 이미지 썸네일 표시
- 그룹 선택/삭제
- 상태 표시

#### 3. 이미지 캐싱
- LRU 캐시
- 썸네일 생성/저장

#### 4. 파일 매칭 사용
- `get_matches()` 메서드 호출 코드 없음
- 프론트엔드에서 `match_files` 호출하는 곳 없음

#### 5. Timestamp 파싱
- `extract_datetime_from_nir_key()` 함수 미사용
- NIR 파일명에서 datetime 추출 로직 활용 안 됨

---

## 미구현/오작동 세부 목록

### 1. ❌ select_folder / select_file 명령어
**문제**: Browse 버튼 클릭 시 아무 반응 없음

**원인**:
```typescript
// src/pages/Settings.tsx:58
const selected = await invoke<string | null>('select_folder')
// ❌ src-tauri에 이 명령어가 없음!
```

**필요 구현**:
```rust
// src-tauri/src/commands/dialog.rs (신규 파일)
use tauri::api::dialog;

#[tauri::command]
pub async fn select_folder() -> Result<Option<String>, String> {
    let folder = dialog::blocking::FileDialogBuilder::new()
        .pick_folder();
    Ok(folder.map(|p| p.to_string_lossy().to_string()))
}

#[tauri::command]
pub async fn select_file(filters: Vec<(String, Vec<String>)>) -> Result<Option<String>, String> {
    // 구현 필요
}
```

---

### 2. ❌ 메인 화면 (그룹 테이블 + 이미지 뷰어)

**현재**: Dashboard에 통계 카드만 있음

**필요**:
```typescript
// src/pages/MainMonitoring.tsx (신규)
interface FileGroup {
  id: string;
  timestamp: number;
  status: 'Pending' | 'Running' | 'Completed' | 'Error';
  normal_file?: string;
  normal2_file?: string;
  nir_files: string[];
  nir2_files: string[];
  cam_files: string[];
  selected: boolean;
}

// 테이블 표시
// 썸네일 클릭 -> 원본 이미지 미리보기
// 체크박스로 선택
// 이동/복사/삭제 버튼
```

---

### 3. ❌ 아이콘 미적용

**문제**: 빌드된 .exe에 기본 아이콘 표시

**확인 필요**:
```bash
# Windows에서 실행
# 아이콘이 app_icon.ico로 바뀌었는지 확인
# 안 바뀌었다면 tauri.conf.json 재확인
```

**tauri.conf.json**:
```json
"bundle": {
  "icon": [
    "icons/icon.ico"  // ✅ 설정됨
  ]
}
```

**가능한 원인**:
- 아이콘 캐시 (Windows 탐색기)
- MSI/NSIS 빌드 시 아이콘 임베딩 실패

---

### 4. ❌ 앱 종료 문제

**문제**: 창 닫기 시 프로세스 남아있음

**원인**: Tauri closeEvent 처리 누락

**필요**:
```rust
// src-tauri/src/lib.rs
.on_window_event(|event| match event.event() {
    tauri::WindowEvent::CloseRequested { api, .. } => {
        // 모든 스레드 정리
        // 파일 감시 중지
        // 워커 종료
        api.prevent_close(); // 필요시
    }
    _ => {}
})
```

---

### 5. ❌ 파일 매칭 미활용

**구현됨**:
```rust
// src/models/matcher.rs:50
pub fn get_matches(&self) -> &Vec<FileMatch> {
    &self.matches
}
```

**하지만**: 아무 곳에서도 호출 안 됨

**원인**: 프론트엔드에서 `match_files` → `get_matched_groups` 워크플로우 없음

**필요**:
```typescript
// 1. 매처 생성
await invoke('create_matcher', { matcherId: 'main' })

// 2. 패턴 추가
await invoke('add_pattern', {
  matcherId: 'main',
  pattern: { ... }
})

// 3. 파일 매칭
await invoke('match_files', {
  matcherId: 'main',
  files: ['...']
})

// 4. 결과 가져오기
const groups = await invoke('get_matched_groups', { matcherId: 'main' })
```

---

### 6. ❌ Timestamp 파싱 미활용

**구현됨**:
```rust
// src/utils/timestamp.rs:34
pub fn extract_datetime_from_nir_key(nir_key: &str) -> Option<DateTime<Utc>> {
    // yyyyMMddHHmmss 파싱
}
```

**하지만**: 호출하는 곳 없음

**원인**: NIR 파일 그룹핑 로직이 프론트엔드에 없음

**필요**:
- NIR 파일명에서 timestamp 추출
- 같은 timestamp끼리 그룹핑
- UI에 그룹별로 표시

---

### 7. ❌ Python의 주요 기능들

#### 없는 기능
1. **Config Manager**
   - JSON 설정 파일 자동 저장/로드
   - 앱 디렉터리 관리

2. **Group Manager**
   - 그룹 상태 관리 (Pending → Running → Completed)
   - groups_state.json 저장/복원

3. **File Count Worker**
   - 별도 스레드로 5초마다 파일 개수 카운트
   - ✅ Rust에는 `get_folder_stats` 있지만 주기적 호출 안 함

4. **Image Loader Worker**
   - 비동기 썸네일 생성
   - 캐시 디렉터리 관리

5. **File Operation Worker**
   - 멀티스레드 파일 이동/복사
   - 충돌 처리 UI
   - 롤백 기능
   - move_plan.json 생성

6. **Settings Dialog**
   - ✅ Settings.tsx 있지만 기능 미작동

7. **Preview Dialog**
   - 이미지 클릭 시 원본 크기 팝업

8. **Auto Path Setting**
   - 날짜 기반 자동 경로 생성
   - 시료 폴더 자동 생성

---

## 필수 수정 사항 (우선순위)

### P0 - 즉시 수정 필요

1. **select_folder / select_file 명령어 구현**
   - Settings에서 Browse 버튼 작동하게

2. **앱 종료 처리**
   - 창 닫기 시 모든 리소스 정리

3. **아이콘 적용**
   - .exe 파일 아이콘 확인 및 수정

### P1 - 핵심 기능

4. **메인 모니터링 화면 구현**
   - 파일 그룹 테이블
   - 이미지 썸네일 표시
   - 그룹 선택/삭제

5. **파일 매칭 활용**
   - 프론트엔드에서 `match_files` 워크플로우 구현
   - `get_matches()` 활용

6. **실시간 파일 개수**
   - Dashboard에서 5초마다 `get_folder_stats` 호출
   - ✅ 이미 구현됨 (Dashboard.tsx)

### P2 - 편의 기능

7. **이미지 미리보기**
   - 썸네일 클릭 시 원본 팝업

8. **자동 경로 설정**
   - 날짜 기반 폴더 자동 생성

9. **파일 작업 진행 상황**
   - 이동/복사 시 프로그레스 바

---

## Rust 경고 제거

### Warning 1: `get_matches` is never used
```rust
// src/models/matcher.rs:50
pub fn get_matches(&self) -> &Vec<FileMatch> {
    &self.matches
}
```

**해결책**:
- 프론트엔드에서 사용하도록 워크플로우 구현
- 또는 임시로 `#[allow(dead_code)]` 추가

### Warning 2: `extract_datetime_from_nir_key` is never used
```rust
// src/utils/timestamp.rs:34
pub fn extract_datetime_from_nir_key(nir_key: &str) -> Option<DateTime<Utc>> {
```

**해결책**:
- NIR 파일 그룹핑 시 사용
- 또는 `#[allow(dead_code)]` 추가

---

## 권장 아키텍처 변경

### 현재 구조
```
Dashboard (통계만)
├── Settings (폴더 설정)
├── Monitoring (시작/중지)
└── NIR (스펙트럼 분석)
```

### 권장 구조 (Python 원본과 유사)
```
Main Monitoring (핵심 화면)
├── 파일 그룹 테이블
├── 이미지 썸네일
├── Run/Stop 버튼
├── 이동/복사 버튼
├── Settings 다이얼로그
└── Preview 다이얼로그

NIR Monitoring (별도 화면)
├── 스펙트럼 분석
├── 자동 이동/삭제
└── Prune 기능

Dashboard (요약 통계)
└── 파일 개수, 그룹 상태 등
```

---

## 다음 단계

1. **긴급 수정** (P0)
   - select_folder 구현
   - 앱 종료 처리
   - 아이콘 확인

2. **메인 화면 재설계** (P1)
   - Python 원본 참조하여 MainMonitoring 페이지 작성
   - 파일 그룹 테이블 구현
   - 이미지 로더 연결

3. **파일 매칭 활성화** (P1)
   - get_matches 사용
   - extract_datetime_from_nir_key 사용

4. **단계별 검증**
   - 각 기능 구현 후 Python 원본과 비교
   - 사용자 시나리오 테스트

---

## 참고 파일

### Python 원본
- `/home/seaweed/workspace/main.py` - 진입점
- `/home/seaweed/workspace/monitoring_app.py` - 메인 모니터링 (1300줄)
- `/home/seaweed/workspace/nir_app.py` - NIR 모니터링
- `/home/seaweed/workspace/file_operations.py` - 파일 작업 워커
- `/home/seaweed/workspace/image_loader.py` - 이미지 로더
- `/home/seaweed/workspace/preview_dialog.py` - 이미지 미리보기

### Tauri 현재
- `src-tauri/src/lib.rs` - 진입점
- `src-tauri/src/commands/` - 36개 명령어
- `src/pages/Dashboard.tsx` - 통계 대시보드
- `src/pages/Settings.tsx` - 설정 (미작동)
- `src/pages/Monitoring.tsx` - 간단한 시작/중지
- `src/pages/NIR.tsx` - NIR 분석
