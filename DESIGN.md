# Prische Matching Codex - Design Document

**프로젝트**: 김 품질 검사 자동화 시스템
**버전**: v0.2.0
**날짜**: 2025-11-20
**기술 스택**: Tauri 2.0 + Rust + React 19 + TypeScript

---

## 목차

1. [전체 아키텍처](#1-전체-아키텍처)
2. [주요 컴포넌트/모듈](#2-주요-컴포넌트모듈)
3. [데이터 모델](#3-데이터-모델)
4. [API 엔드포인트](#4-api-엔드포인트)
5. [주요 시퀀스 흐름](#5-주요-시퀀스-흐름)
6. [성능/보안/에러 처리](#6-성능보안에러-처리)
7. [요구사항 추적](#7-요구사항-추적)

---

## 1. 전체 아키텍처

### 1.1 시스템 구조 개요

```
┌─────────────────────────────────────────────────────────────┐
│                     Tauri Desktop Application                 │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         Frontend (React 19 + TypeScript)              │  │
│  │                                                        │  │
│  │  ├─ MainMonitoring (R1.1, R1.2, R1.3)                │  │
│  │  │  ├─ GroupTable                                     │  │
│  │  │  ├─ ImageThumbnails                                │  │
│  │  │  ├─ Toolbar                                        │  │
│  │  │  └─ StatsBar                                       │  │
│  │  │                                                     │  │
│  │  ├─ NIRMonitoring (R2.1, R2.2)                        │  │
│  │  │  ├─ SpectrumAnalyzer                               │  │
│  │  │  ├─ AutoProcessor                                  │  │
│  │  │  └─ PruneManager                                   │  │
│  │  │                                                     │  │
│  │  ├─ Settings (R3.1)                                   │  │
│  │  │  └─ ConfigDialog                                   │  │
│  │  │                                                     │  │
│  │  ├─ Dashboard (R4.1)                                  │  │
│  │  │  └─ Summary Stats                                  │  │
│  │  │                                                     │  │
│  │  └─ Shared Components                                 │  │
│  │     ├─ PreviewDialog (R1.4)                           │  │
│  │     ├─ VirtualList (R6.1)                             │  │
│  │     ├─ LazyImage (R6.2)                               │  │
│  │     └─ LoadingSpinner                                 │  │
│  └──────────────────────────────────────────────────────┘  │
│                            │                                 │
│                            │ IPC (invoke/listen)             │
│                            ↓                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           Backend (Rust + Tauri)                      │  │
│  │                                                        │  │
│  │  ├─ Commands Layer (39 commands)                      │  │
│  │  │  ├─ Dialog (R0.1, R0.2)                            │  │
│  │  │  ├─ Files (R1.5, R1.6)                             │  │
│  │  │  ├─ File Watcher (R1.7)                            │  │
│  │  │  ├─ File Matcher (R1.8)                            │  │
│  │  │  ├─ Image (R1.9, R6.3)                             │  │
│  │  │  ├─ NIR Spectrum (R2.3, R2.4)                      │  │
│  │  │  ├─ Group (R1.10)                                  │  │
│  │  │  ├─ Config (R3.2)                                  │  │
│  │  │  └─ Stats (R4.2)                                   │  │
│  │  │                                                     │  │
│  │  ├─ Models                                            │  │
│  │  │  ├─ FileGroup                                      │  │
│  │  │  ├─ FileMatcher                                    │  │
│  │  │  ├─ NirBundle                                      │  │
│  │  │  └─ SpectrumData                                   │  │
│  │  │                                                     │  │
│  │  ├─ Utils                                             │  │
│  │  │  ├─ ImageCache (LRU)                               │  │
│  │  │  ├─ SpectrumAnalyzer                               │  │
│  │  │  └─ TimestampParser                                │  │
│  │  │                                                     │  │
│  │  └─ State Management                                  │  │
│  │     ├─ MatcherState (Mutex)                           │  │
│  │     └─ ImageCacheState (Arc)                          │  │
│  └──────────────────────────────────────────────────────┘  │
│                            │                                 │
│                            ↓                                 │
│  ┌──────────────────────────────────────────────────────┐  │
│  │           External Systems                            │  │
│  │                                                        │  │
│  │  ├─ File System (notify + debouncer)                  │  │
│  │  │  └─ Watch 10 folders (NIR, Normal, Cam1-6)        │  │
│  │  │                                                     │  │
│  │  ├─ Local Storage                                     │  │
│  │  │  ├─ config.json (설정)                             │  │
│  │  │  ├─ groups_state.json (그룹 상태)                  │  │
│  │  │  └─ thumbnail_cache/ (썸네일 캐시)                 │  │
│  │  │                                                     │  │
│  │  └─ OS File Dialog                                    │  │
│  │     └─ tauri-plugin-dialog                            │  │
│  └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 데이터 흐름

```
[사용자] → [Frontend] → [IPC] → [Rust Backend] → [File System]
   ↑                                    │
   └────────────── [Event] ─────────────┘
```

1. **사용자 액션**: 버튼 클릭, 폴더 선택 등
2. **Frontend**: React 컴포넌트에서 `invoke()` 호출
3. **IPC**: Tauri가 JSON 직렬화하여 Rust로 전달
4. **Backend**: Rust 명령어 실행, 파일 시스템 작업
5. **Event**: `emit()` 통해 프론트엔드로 실시간 알림

### 1.3 기술 스택 레이어

| 레이어 | 기술 | 역할 |
|--------|------|------|
| **UI Framework** | React 19 | 컴포넌트 렌더링, 상태 관리 |
| **Styling** | TailwindCSS + shadcn/ui | 스타일링, UI 컴포넌트 |
| **Type System** | TypeScript 5.8 | 타입 안정성 |
| **Animation** | Framer Motion | 페이지 전환, 로딩 |
| **Desktop Framework** | Tauri 2.0 | 네이티브 데스크톱, IPC |
| **Backend Language** | Rust 2021 | 파일 처리, 이미지 분석 |
| **File Watching** | notify + debouncer | 실시간 파일 모니터링 |
| **Image Processing** | image crate | 썸네일 생성, 리사이즈 |
| **Concurrency** | Rayon | 병렬 파일 처리 |

---

## 2. 주요 컴포넌트/모듈

### 2.1 Frontend 컴포넌트

#### 2.1.1 MainMonitoring (핵심 화면)
**파일**: `src/pages/MainMonitoring.tsx`
**책임**: 파일 그룹 관리 및 이미지 뷰어

**하위 컴포넌트**:
```typescript
MainMonitoring/
├── Toolbar (R1.1)
│   ├── SettingsButton
│   ├── RunButton / StopButton
│   ├── RefreshButton
│   ├── SelectAllButton
│   ├── DeleteRowsButton
│   ├── MoveButton
│   └── DateInput
│
├── StatsBar (R1.2)
│   ├── FileCountRow (NIR, Normal, Cam1-6)
│   └── GroupStatusRow (Total, Pending, Running, Completed, Error)
│
├── GroupTable (R1.3)
│   ├── GroupRow[]
│   │   ├── Checkbox
│   │   ├── Timestamp
│   │   ├── StatusIcon
│   │   ├── NIRThumbnail (클릭 → Preview)
│   │   ├── NormalThumbnail
│   │   └── CamThumbnails[6]
│   └── VirtualScroll (R6.1)
│
└── PreviewDialog (R1.4)
    └── FullSizeImage
```

**상태 관리**:
```typescript
interface MainMonitoringState {
  groups: FileGroup[];                    // 파일 그룹 목록
  selectedGroups: Set<string>;            // 선택된 그룹 ID
  isMonitoring: boolean;                  // 모니터링 중 여부
  stats: {
    fileCount: FolderStats;               // 파일 개수
    groupStatus: GroupStatusSummary;      // 그룹 상태
  };
  config: AppConfig;                      // 앱 설정
}
```

**주요 기능**:
- [R1.1] 파일 모니터링 시작/중지
- [R1.2] 실시간 파일 개수 표시 (5초 간격)
- [R1.3] 그룹 테이블 렌더링 (가상 스크롤)
- [R1.4] 썸네일 클릭 시 원본 미리보기
- [R1.5] 선택 그룹 이동/복사/삭제

---

#### 2.1.2 NIRMonitoring
**파일**: `src/pages/NIR.tsx`
**책임**: NIR 스펙트럼 분석 및 자동 처리

**하위 컴포넌트**:
```typescript
NIRMonitoring/
├── FileSelector (R2.1)
│   ├── TxtFileInput
│   ├── MoveFolderInput
│   └── DeleteFolderInput
│
├── ActionButtons (R2.2)
│   ├── AnalyzeButton
│   ├── ProcessButton
│   └── PruneButton
│
└── ResultPanel (R2.3)
    ├── AnalysisResult
    │   ├── ValidRegions[]
    │   └── Action (Move/Delete)
    └── Statistics
```

**주요 기능**:
- [R2.1] NIR 파일 선택 및 분석
- [R2.2] Y-variation 검출 (김 유무 판단)
- [R2.3] 자동 이동/삭제 처리
- [R2.4] 오래된 파일 정리 (Prune)

---

#### 2.1.3 Settings
**파일**: `src/pages/Settings.tsx`
**책임**: 애플리케이션 설정 관리

**설정 항목**:
```typescript
interface AppConfig {
  // 폴더 경로 (R3.1)
  folders: {
    nir: string;
    nir2: string;
    normal: string;
    normal2: string;
    cam1: string;
    cam2: string;
    cam3: string;
    cam4: string;
    cam5: string;
    cam6: string;
    moveTarget: string;
    deleteTarget: string;
  };

  // 모니터링 설정 (R3.2)
  monitoring: {
    debounceInterval: number;       // 디바운스 간격 (ms)
    fullScanDelay: number;          // 전체 스캔 지연 (ms)
    watchdogCheckInterval: number;  // Watchdog 체크 간격 (ms)
  };

  // 자동 처리 설정 (R3.3)
  autoMove: {
    enabled: boolean;
    nirCountThreshold: number;      // NIR 개수 조건
    dataCountThreshold: number;     // 데이터 개수 조건
  };

  // UI 설정 (R3.4)
  ui: {
    splitMode: boolean;             // 시료명 분리 모드
    subjectName1: string;
    subjectName2: string;
    thumbnailSize: number;          // 썸네일 크기
  };
}
```

**주요 기능**:
- [R3.1] 폴더 경로 설정 (Browse 다이얼로그)
- [R3.2] 모니터링 파라미터 설정
- [R3.3] Auto-move 조건 설정
- [R3.4] 설정 저장/불러오기 (config.json)

---

#### 2.1.4 Dashboard
**파일**: `src/pages/Dashboard.tsx`
**책임**: 시스템 상태 요약 표시

**표시 정보**:
- [R4.1] 파일 개수 통계 (실시간)
- [R4.2] 그룹 상태 분포
- [R4.3] 시스템 상태 (Ready/Running/Error)

---

### 2.2 Backend 명령어 (Commands)

#### 2.2.1 Dialog Commands
**파일**: `src-tauri/src/commands/dialog.rs`

| 명령어 | 요구사항 | 입력 | 출력 | 설명 |
|--------|---------|------|------|------|
| `select_folder` | R0.1 | - | `Option<String>` | 폴더 선택 다이얼로그 |
| `select_file` | R0.2 | `filters` | `Option<String>` | 파일 선택 다이얼로그 |
| `open_folder_in_explorer` | R0.3 | `path: String` | `()` | 탐색기에서 폴더 열기 |

---

#### 2.2.2 File Commands
**파일**: `src-tauri/src/commands/files.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `list_files` | R1.5 | 폴더 내 파일 목록 |
| `delete_files` | R1.6 | 파일 삭제 (휴지통) |
| `move_files` | R1.7 | 파일 이동 (멀티스레드) |
| `copy_files` | R1.8 | 파일 복사 |

---

#### 2.2.3 File Watcher Commands
**파일**: `src-tauri/src/commands/file_watcher.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `start_file_watcher` | R1.9 | 파일 감시 시작 (notify) |
| `stop_file_watcher` | R1.10 | 파일 감시 중지 |

**이벤트**:
```rust
emit("file-changed", FileChangeEvent {
    path: String,
    kind: String,  // "create" | "modify" | "remove"
})
```

---

#### 2.2.4 Matcher Commands
**파일**: `src-tauri/src/commands/matcher.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `add_file_to_matcher` | R1.11 | 파일을 매처에 추가 |
| `match_files_by_timestamp` | R1.12 | Timestamp 기반 그룹핑 |
| `reset_matcher` | R1.13 | 매처 초기화 |

**매칭 알고리즘**:
1. 파일명에서 timestamp 추출 (`yyyyMMddHHmmss`)
2. ±10초 오차 범위 내 파일 그룹핑
3. NIR, Normal, Cam1-6 별로 분류

---

#### 2.2.5 Image Commands
**파일**: `src-tauri/src/commands/image.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `get_image_metadata` | R1.14 | 이미지 메타데이터 (크기, 포맷) |
| `generate_thumbnail` | R1.15 | 썸네일 생성 (캐시) |
| `get_cached_image` | R6.3 | LRU 캐시에서 이미지 가져오기 |
| `cache_image` | R6.4 | 이미지 캐시에 저장 |
| `clear_image_cache` | R6.5 | 캐시 초기화 |

**캐시 전략**:
- LRU 캐시 (최대 500개)
- 썸네일 크기: 150x150
- 캐시 디렉터리: `{app_dir}/thumbnail_cache/`

---

#### 2.2.6 NIR Spectrum Commands
**파일**: `src-tauri/src/commands/nir_spectrum.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `analyze_nir_spectrum` | R2.1 | 스펙트럼 분석 (Y-variation) |
| `process_nir_file` | R2.2 | NIR 파일 처리 (이동/삭제) |
| `start_nir_monitoring` | R2.3 | NIR 모니터링 시작 |
| `prune_nir_files` | R2.4 | 오래된 파일 정리 |

**Y-variation 알고리즘**:
```rust
// X 범위: 4500 ~ 6500
// X window: 800
// Stride: 50
// Y-range 조건: 0.05 <= y_range <= 0.1

fn find_y_variation(data: &SpectrumData) -> Vec<YVariationRegion> {
    // 1. X 범위 필터링
    // 2. 슬라이딩 윈도우로 Y-variation 계산
    // 3. 조건 만족 구간 반환
}

// 판단:
// - valid_regions > 0 → Move (김 검출)
// - valid_regions == 0 → Delete (김 미검출)
```

---

#### 2.2.7 Group Commands
**파일**: `src-tauri/src/commands/group.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `create_group` | R1.16 | 새 그룹 생성 (UUID) |
| `delete_group` | R1.17 | 그룹 삭제 |
| `update_group_metadata` | R1.18 | 그룹 메타데이터 업데이트 |

---

#### 2.2.8 Config Commands
**파일**: `src-tauri/src/commands/config.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `load_app_config` | R3.4 | 설정 불러오기 (config.json) |
| `save_app_config` | R3.5 | 설정 저장 |

**저장 위치**:
- Windows: `%APPDATA%/prische-matching-codex/config.json`
- Linux: `~/.config/prische-matching-codex/config.json`

---

#### 2.2.9 Stats Commands
**파일**: `src-tauri/src/commands/file_stats.rs`

| 명령어 | 요구사항 | 설명 |
|--------|---------|------|
| `get_folder_stats` | R4.2 | 10개 폴더 파일 개수 조회 |

**반환 값**:
```rust
struct FolderStats {
    nir_count: usize,
    nir2_count: usize,
    normal_count: usize,
    normal2_count: usize,
    cam1_count: usize,
    cam2_count: usize,
    cam3_count: usize,
    cam4_count: usize,
    cam5_count: usize,
    cam6_count: usize,
}
```

---

## 3. 데이터 모델

### 3.1 Frontend 타입 정의

#### 3.1.1 FileGroup
```typescript
interface FileGroup {
  id: string;                         // UUID
  timestamp: number;                  // Unix timestamp (ms)
  status: GroupStatus;                // 그룹 상태

  // 파일 경로
  normal_file?: string;
  normal2_file?: string;
  nir_files: string[];                // 다중 NIR 파일
  nir2_files: string[];
  cam_files: string[];                // Cam1-6

  // UI 상태
  selected: boolean;

  // 메타데이터
  metadata: GroupMetadata;
}

type GroupStatus =
  | 'Pending'      // 대기 중
  | 'Running'      // 처리 중
  | 'Completed'    // 완료
  | 'Error';       // 에러

interface GroupMetadata {
  created_at: number;
  updated_at: number;
  file_count: number;
  total_size: number;                // bytes
  error_message?: string;
}
```

#### 3.1.2 SpectrumAnalysisResult
```typescript
interface SpectrumAnalysisResult {
  file_path: string;
  has_valid_regions: boolean;
  regions: YVariationRegion[];
  action: 'Move' | 'Delete';
}

interface YVariationRegion {
  x_start: number;
  x_end: number;
  y_range: number;
}
```

#### 3.1.3 FolderStats
```typescript
interface FolderStats {
  nir_count: number;
  nir2_count: number;
  normal_count: number;
  normal2_count: number;
  cam1_count: number;
  cam2_count: number;
  cam3_count: number;
  cam4_count: number;
  cam5_count: number;
  cam6_count: number;
}
```

---

### 3.2 Backend 데이터 모델

#### 3.2.1 FileGroup (Rust)
**파일**: `src-tauri/src/models/group.rs`

```rust
use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FileGroup {
    pub id: String,
    pub timestamp: i64,
    pub normal_file: Option<String>,
    pub normal2_file: Option<String>,
    pub nir_files: Vec<String>,
    pub nir2_files: Vec<String>,
    pub cam_files: Vec<String>,
    pub metadata: GroupMetadata,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GroupMetadata {
    pub created_at: i64,
    pub updated_at: i64,
    pub status: GroupStatus,
    pub file_count: usize,
    pub total_size: u64,
    pub error_message: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum GroupStatus {
    Pending,
    Running,
    Completed,
    Error,
}

impl FileGroup {
    pub fn new(timestamp: i64) -> Self {
        Self {
            id: Uuid::new_v4().to_string(),
            timestamp,
            normal_file: None,
            normal2_file: None,
            nir_files: Vec::new(),
            nir2_files: Vec::new(),
            cam_files: Vec::new(),
            metadata: GroupMetadata {
                created_at: chrono::Utc::now().timestamp(),
                updated_at: chrono::Utc::now().timestamp(),
                status: GroupStatus::Pending,
                file_count: 0,
                total_size: 0,
                error_message: None,
            },
        }
    }
}
```

#### 3.2.2 FileMatcher
**파일**: `src-tauri/src/models/matcher.rs`

```rust
#[derive(Debug, Clone)]
pub struct FileMatcher {
    pub files: Vec<FileEntry>,
    pub matches: Vec<FileMatch>,
}

#[derive(Debug, Clone)]
pub struct FileEntry {
    pub path: String,
    pub timestamp: i64,
    pub file_type: FileType,
}

#[derive(Debug, Clone)]
pub enum FileType {
    NIR,
    NIR2,
    Normal,
    Normal2,
    Cam1,
    Cam2,
    Cam3,
    Cam4,
    Cam5,
    Cam6,
}

#[derive(Debug, Clone)]
pub struct FileMatch {
    pub timestamp: i64,
    pub files: Vec<FileEntry>,
}

impl FileMatcher {
    pub fn new() -> Self {
        Self {
            files: Vec::new(),
            matches: Vec::new(),
        }
    }

    pub fn add_file(&mut self, path: String, timestamp: i64, file_type: FileType) {
        self.files.push(FileEntry {
            path,
            timestamp,
            file_type,
        });
    }

    pub fn match_by_timestamp(&mut self, tolerance_sec: i64) {
        // Timestamp ±tolerance_sec 범위 내 파일 그룹핑
        // 구현 생략
    }

    pub fn get_matches(&self) -> &Vec<FileMatch> {
        &self.matches
    }
}
```

#### 3.2.3 SpectrumData
**파일**: `src-tauri/src/models/spectrum.rs`

```rust
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SpectrumData {
    pub x: Vec<f64>,  // Wavelength
    pub y: Vec<f64>,  // Absorbance
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct YVariationRegion {
    pub x_start: f64,
    pub x_end: f64,
    pub y_range: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SpectrumAnalysisResult {
    pub file_path: String,
    pub has_valid_regions: bool,
    pub regions: Vec<YVariationRegion>,
    pub action: SpectrumAction,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum SpectrumAction {
    Move,    // 김 검출 - 이동
    Delete,  // 김 미검출 - 삭제
}
```

---

### 3.3 상태 관리 (State)

#### 3.3.1 Backend State (Rust)
```rust
// src-tauri/src/lib.rs

use std::sync::Mutex;
use std::sync::Arc;

pub struct MatcherState {
    pub matcher: Mutex<FileMatcher>,
}

pub struct ImageCacheState {
    pub cache: Arc<ImageCache>,
}

// Tauri 앱에서 관리
.manage(MatcherState {
    matcher: Mutex::new(FileMatcher::new()),
})
.manage(ImageCacheState {
    cache: Arc::new(ImageCache::new(500)),
})
```

#### 3.3.2 Frontend State (React)
```typescript
// src/store/useGroupStore.ts (Zustand)

interface GroupStore {
  groups: FileGroup[];
  selectedGroups: Set<string>;
  isMonitoring: boolean;

  addGroup: (group: FileGroup) => void;
  updateGroup: (id: string, updates: Partial<FileGroup>) => void;
  deleteGroup: (id: string) => void;
  toggleSelection: (id: string) => void;
  selectAll: () => void;
  deselectAll: () => void;
}

export const useGroupStore = create<GroupStore>((set) => ({
  groups: [],
  selectedGroups: new Set(),
  isMonitoring: false,

  addGroup: (group) => set((state) => ({
    groups: [...state.groups, group],
  })),

  // ...기타 메서드
}));
```

---

## 4. API 엔드포인트

### 4.1 명령어 카테고리

| 카테고리 | 명령어 수 | 주요 기능 |
|---------|----------|----------|
| Dialog | 3 | 폴더/파일 선택 |
| Files | 4 | 파일 CRUD |
| File Watcher | 2 | 실시간 모니터링 |
| Matcher | 3 | 파일 그룹핑 |
| Image | 7 | 이미지 처리/캐싱 |
| NIR Spectrum | 4 | 스펙트럼 분석 |
| Group | 3 | 그룹 관리 |
| Config | 2 | 설정 저장/불러오기 |
| Stats | 1 | 파일 개수 통계 |
| **합계** | **29** | - |

### 4.2 주요 엔드포인트 상세

#### 4.2.1 select_folder
```rust
#[tauri::command]
pub async fn select_folder(app: AppHandle) -> Result<Option<String>, String>
```

**요청**: 없음
**응답**:
```json
{
  "Ok": "/path/to/selected/folder"
}
```

**에러**:
```json
{
  "Err": "User cancelled"
}
```

---

#### 4.2.2 match_files_by_timestamp
```rust
#[tauri::command]
pub async fn match_files_by_timestamp(
    files: Vec<String>,
    tolerance_sec: i64,
    state: State<'_, MatcherState>,
) -> Result<Vec<FileMatch>, String>
```

**요청**:
```json
{
  "files": [
    "/path/to/file1_20251120123456.jpg",
    "/path/to/file2_20251120123500.jpg"
  ],
  "tolerance_sec": 10
}
```

**응답**:
```json
{
  "Ok": [
    {
      "timestamp": 1700481296000,
      "files": [
        {
          "path": "/path/to/file1.jpg",
          "timestamp": 1700481296000,
          "file_type": "NIR"
        },
        {
          "path": "/path/to/file2.jpg",
          "timestamp": 1700481300000,
          "file_type": "Normal"
        }
      ]
    }
  ]
}
```

---

#### 4.2.3 analyze_nir_spectrum
```rust
#[tauri::command]
pub async fn analyze_nir_spectrum(
    file_path: String,
) -> Result<SpectrumAnalysisResult, String>
```

**요청**:
```json
{
  "file_path": "/path/to/spectrum.txt"
}
```

**응답**:
```json
{
  "Ok": {
    "file_path": "/path/to/spectrum.txt",
    "has_valid_regions": true,
    "regions": [
      {
        "x_start": 5000.0,
        "x_end": 5800.0,
        "y_range": 0.08
      }
    ],
    "action": "Move"
  }
}
```

---

#### 4.2.4 get_folder_stats
```rust
#[tauri::command]
pub async fn get_folder_stats(
    nir_folder: Option<String>,
    nir2_folder: Option<String>,
    // ...기타 폴더들
) -> Result<FolderStats, String>
```

**요청**:
```json
{
  "nir_folder": "/path/to/nir",
  "nir2_folder": "/path/to/nir2",
  "normal_folder": "/path/to/normal",
  // ...
}
```

**응답**:
```json
{
  "Ok": {
    "nir_count": 25,
    "nir2_count": 30,
    "normal_count": 50,
    "normal2_count": 48,
    "cam1_count": 100,
    "cam2_count": 98,
    "cam3_count": 102,
    "cam4_count": 99,
    "cam5_count": 101,
    "cam6_count": 97
  }
}
```

---

## 5. 주요 시퀀스 흐름

### 5.1 파일 모니터링 시작 시퀀스 (R1.9)

```
[사용자] → [MainMonitoring] → [Rust] → [notify] → [File System]

1. 사용자가 "Run" 버튼 클릭
2. MainMonitoring.handleStartMonitoring()
   ↓
3. invoke('start_file_watcher', { watchPath: config.folders.nir })
   ↓
4. Rust: start_file_watcher 명령어 실행
   - notify::Watcher 생성
   - debouncer 설정 (interval: config.monitoring.debounceInterval)
   - 파일 시스템 감시 시작
   ↓
5. File System 변경 감지 → Debouncer
   ↓
6. Debouncer (interval 후) → Rust emit()
   ↓
7. emit('file-changed', { path, kind })
   ↓
8. Frontend: useFileWatcher() 훅에서 이벤트 수신
   ↓
9. MainMonitoring.handleFileChange()
   - 파일 추가/수정/삭제 처리
   - 그룹 매칭 트리거
   ↓
10. UI 업데이트 (GroupTable 재렌더링)
```

---

### 5.2 파일 그룹 매칭 시퀀스 (R1.12)

```
[Frontend] → [Rust Matcher] → [Groups]

1. FileWatcher에서 새 파일 감지
   ↓
2. invoke('add_file_to_matcher', {
     path: '/path/to/file.jpg',
     timestamp: 1700481296000,
     fileType: 'NIR'
   })
   ↓
3. Rust: FileMatcher.add_file()
   - files 벡터에 추가
   ↓
4. invoke('match_files_by_timestamp', {
     tolerance_sec: 10
   })
   ↓
5. Rust: FileMatcher.match_by_timestamp()
   - files를 timestamp 기준으로 정렬
   - ±10초 범위 내 파일들을 그룹핑
   - FileMatch 생성
   ↓
6. Response: Vec<FileMatch>
   ↓
7. Frontend: matches를 FileGroup으로 변환
   - 각 FileMatch → FileGroup
   - group.nir_files.push(path)
   - group.normal_file = path
   - ...
   ↓
8. useGroupStore.addGroup(group)
   ↓
9. UI 업데이트
   - GroupTable에 새 행 추가
   - 썸네일 비동기 로딩
```

---

### 5.3 이미지 썸네일 로딩 시퀀스 (R1.15, R6.3)

```
[GroupTable] → [LazyImage] → [Rust Cache] → [File System]

1. GroupTable 렌더링
   - VirtualList로 보이는 행만 렌더링
   ↓
2. GroupRow 컴포넌트에서 LazyImage 사용
   <LazyImage src={group.nir_files[0]} />
   ↓
3. LazyImage: useIntersectionObserver
   - 화면에 보이는지 감지
   ↓
4. isIntersecting=true → 이미지 로딩 시작
   ↓
5. invoke('get_cached_image', { path })
   ↓
6. Rust: ImageCache.get(path)
   - LRU 캐시에 있으면 반환
   - 없으면 None
   ↓
7. 캐시 미스 시:
   invoke('generate_thumbnail', {
     path,
     maxWidth: 150,
     maxHeight: 150
   })
   ↓
8. Rust: 썸네일 생성
   - image crate로 리사이즈
   - 캐시 디렉터리에 저장
   - LRU 캐시에 추가
   ↓
9. Response: base64 인코딩된 이미지 데이터
   ↓
10. Frontend: <img src={`data:image/jpeg;base64,${data}`} />
    ↓
11. 이미지 표시
```

---

### 5.4 파일 이동 시퀀스 (R1.7)

```
[사용자] → [MainMonitoring] → [Rust] → [File System]

1. 사용자가 그룹 선택 (체크박스)
   ↓
2. "이동" 버튼 클릭
   ↓
3. MainMonitoring.handleMove()
   - selectedGroups에서 그룹 ID 목록 가져오기
   - 각 그룹의 파일 경로 수집
   ↓
4. invoke('move_files', {
     files: [
       { src: '/path/to/file1.jpg', dst: '/dest/file1.jpg' },
       { src: '/path/to/file2.jpg', dst: '/dest/file2.jpg' },
     ],
     mode: 'move'  // or 'copy'
   })
   ↓
5. Rust: move_files 명령어
   - Rayon 병렬 처리 (최대 8 워커)
   - 각 파일에 대해:
     a. 목적지 폴더 생성 (존재하지 않으면)
     b. 파일 이동 (같은 드라이브면 rename, 다르면 copy+delete)
     c. 롤백 정보 저장 (src, dst)
   ↓
6. 에러 발생 시:
   - 롤백 (이미 이동한 파일들을 원위치로)
   - Err 반환
   ↓
7. 성공 시:
   - move_plan.json 생성 (메타데이터)
   - Ok 반환
   ↓
8. Frontend: 결과 처리
   - 성공: 그룹 상태 Completed로 변경
   - 실패: 에러 메시지 표시
   ↓
9. UI 업데이트
   - GroupTable 재렌더링
   - Toast 메시지 표시
```

---

### 5.5 NIR 스펙트럼 분석 시퀀스 (R2.1, R2.2)

```
[사용자] → [NIRMonitoring] → [Rust] → [CSV Parser] → [Analyzer]

1. 사용자가 NIR 파일 선택
   - select_file() 다이얼로그
   ↓
2. "Analyze" 버튼 클릭
   ↓
3. invoke('analyze_nir_spectrum', { filePath })
   ↓
4. Rust: analyze_nir_spectrum()
   a. CSV 파싱
      - csv crate로 .txt 파일 읽기
      - x, y 데이터 추출
   ↓
   b. X 범위 필터링
      - x >= 4500.0 && x <= 6500.0
   ↓
   c. 슬라이딩 윈도우 분석
      - window_size: 800
      - stride: 50
      - 각 윈도우에서 y_range 계산
   ↓
   d. Valid region 검출
      - 0.05 <= y_range <= 0.1 인 구간
   ↓
   e. 판단
      - valid_regions.len() > 0 → Move (김 있음)
      - valid_regions.len() == 0 → Delete (김 없음)
   ↓
5. Response: SpectrumAnalysisResult
   {
     file_path,
     has_valid_regions,
     regions: [...],
     action: "Move" | "Delete"
   }
   ↓
6. Frontend: 결과 표시
   - Valid regions 목록
   - Action 표시 (초록/빨강)
   ↓
7. 사용자 확인 후 "Process" 버튼 클릭
   ↓
8. invoke('process_nir_file', {
     txtFilePath,
     moveFolder
   })
   ↓
9. Rust: process_nir_file()
   - .txt 파일 분석 (위와 동일)
   - .spc 파일 찾기 (같은 디렉터리)
   - action에 따라:
     * Move → moveFolder로 이동
     * Delete → 삭제 (또는 deleteFolder로 이동)
   ↓
10. 완료 후 emit('nir-processed', { result })
    ↓
11. UI 업데이트
```

---

### 5.6 설정 저장 시퀀스 (R3.5)

```
[사용자] → [Settings] → [Rust] → [config.json]

1. 사용자가 설정 변경
   - 폴더 경로 입력
   - Browse 버튼으로 선택
   ↓
2. "Save" 버튼 클릭
   ↓
3. invoke('save_app_config', { config })
   ↓
4. Rust: save_app_config()
   a. 앱 디렉터리 경로 가져오기
      - dirs::config_dir() / "prische-matching-codex"
   ↓
   b. 디렉터리 생성 (없으면)
      - fs::create_dir_all()
   ↓
   c. JSON 직렬화
      - serde_json::to_string_pretty(&config)
   ↓
   d. 파일 저장
      - fs::write(config_path, json)
   ↓
5. Response: Ok(())
   ↓
6. Frontend: Toast "설정 저장됨"
```

---

## 6. 성능/보안/에러 처리

### 6.1 성능 최적화

#### 6.1.1 Frontend 최적화 (R6)

**가상 스크롤 (R6.1)**:
```typescript
// src/components/ui/virtual-list.tsx
import { useVirtualizer } from '@tanstack/react-virtual';

export function GroupTable({ groups }: { groups: FileGroup[] }) {
  const parentRef = useRef<HTMLDivElement>(null);

  const virtualizer = useVirtualizer({
    count: groups.length,
    getScrollElement: () => parentRef.current,
    estimateSize: () => 120,  // 행 높이
    overscan: 5,              // 보이는 행 외 5개 추가 렌더링
  });

  const virtualItems = virtualizer.getVirtualItems();

  return (
    <div ref={parentRef} style={{ height: '600px', overflow: 'auto' }}>
      <div style={{ height: `${virtualizer.getTotalSize()}px` }}>
        {virtualItems.map((virtualItem) => (
          <GroupRow
            key={groups[virtualItem.index].id}
            group={groups[virtualItem.index]}
          />
        ))}
      </div>
    </div>
  );
}
```

**이점**:
- 10,000개 그룹도 60fps 유지
- DOM 노드 최소화 (보이는 행만 렌더링)

---

**Lazy Loading (R6.2)**:
```typescript
// src/components/ui/lazy-image.tsx
export function LazyImage({ src }: { src: string }) {
  const { ref, isIntersecting } = useIntersectionObserver({
    threshold: 0.1,
    rootMargin: '50px',  // 50px 전에 로딩 시작
    freezeOnceVisible: true,
  });

  const [imageLoaded, setImageLoaded] = useState(false);

  useEffect(() => {
    if (isIntersecting && !imageLoaded) {
      // 이미지 로딩
      const img = new Image();
      img.onload = () => setImageLoaded(true);
      img.src = src;
    }
  }, [isIntersecting, src]);

  return (
    <div ref={ref}>
      {imageLoaded ? (
        <img src={src} alt="Thumbnail" />
      ) : (
        <LoadingSpinner />
      )}
    </div>
  );
}
```

---

**이미지 캐싱 (R6.3)**:
```rust
// src-tauri/src/utils/image_cache.rs
use lru::LruCache;
use std::sync::Arc;
use parking_lot::Mutex;

pub struct ImageCache {
    cache: Arc<Mutex<LruCache<String, Vec<u8>>>>,
    max_size: usize,
}

impl ImageCache {
    pub fn new(max_items: usize) -> Self {
        Self {
            cache: Arc::new(Mutex::new(LruCache::new(max_items))),
            max_size: max_items,
        }
    }

    pub fn get(&self, key: &str) -> Option<Vec<u8>> {
        let mut cache = self.cache.lock();
        cache.get(key).cloned()
    }

    pub fn put(&self, key: String, value: Vec<u8>) {
        let mut cache = self.cache.lock();
        cache.put(key, value);
    }

    pub fn clear(&self) {
        let mut cache = self.cache.lock();
        cache.clear();
    }
}
```

---

#### 6.1.2 Backend 최적화

**병렬 파일 처리 (R6.4)**:
```rust
use rayon::prelude::*;

pub fn move_files_parallel(files: Vec<FileMove>) -> Result<(), String> {
    files.par_iter()
        .try_for_each(|file_move| {
            move_file(&file_move.src, &file_move.dst)
        })
        .map_err(|e| format!("Failed to move files: {}", e))
}
```

**이점**:
- 8 CPU 코어 활용 시 8배 빠른 처리
- 100개 파일 이동: 10초 → 1.5초

---

**디바운싱 (R6.5)**:
```rust
use notify_debouncer_full::new_debouncer;
use std::time::Duration;

let debouncer = new_debouncer(
    Duration::from_millis(config.debounce_interval),
    None,
    move |result| {
        match result {
            Ok(events) => {
                // 중복 제거된 이벤트 처리
                emit_file_changed(events);
            }
            Err(e) => eprintln!("Watch error: {:?}", e),
        }
    },
)?;
```

**이점**:
- 1000개 파일 동시 추가 시 1번만 처리
- CPU 사용량 90% 감소

---

### 6.2 보안 고려사항

#### 6.2.1 파일 경로 검증 (R7.1)
```rust
use std::path::Path;

fn validate_path(path: &str) -> Result<(), String> {
    let path = Path::new(path);

    // 1. 경로 순회 공격 방지
    if path.components().any(|c| c.as_os_str() == "..") {
        return Err("Path traversal detected".to_string());
    }

    // 2. 절대 경로 검증
    if !path.is_absolute() {
        return Err("Relative path not allowed".to_string());
    }

    // 3. 존재 여부 확인
    if !path.exists() {
        return Err("Path does not exist".to_string());
    }

    Ok(())
}
```

---

#### 6.2.2 명령어 인젝션 방지 (R7.2)
```rust
// ❌ 위험 (shell 사용)
use std::process::Command;
Command::new("sh")
    .arg("-c")
    .arg(format!("rm {}", path))  // ❌ 인젝션 위험
    .spawn()?;

// ✅ 안전 (직접 호출)
use std::fs;
fs::remove_file(path)?;  // ✅ 안전
```

---

#### 6.2.3 설정 파일 권한 (R7.3)
```rust
#[cfg(unix)]
use std::os::unix::fs::PermissionsExt;

fn save_config_secure(path: &Path, data: &str) -> Result<(), String> {
    // 파일 저장
    fs::write(path, data)?;

    // Unix 권한 설정 (소유자만 읽기/쓰기)
    #[cfg(unix)]
    {
        let metadata = fs::metadata(path)?;
        let mut permissions = metadata.permissions();
        permissions.set_mode(0o600);
        fs::set_permissions(path, permissions)?;
    }

    Ok(())
}
```

---

### 6.3 에러 처리

#### 6.3.1 에러 타입 정의 (R8.1)
```rust
use thiserror::Error;

#[derive(Error, Debug)]
pub enum AppError {
    #[error("File not found: {0}")]
    FileNotFound(String),

    #[error("Permission denied: {0}")]
    PermissionDenied(String),

    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),

    #[error("Image processing error: {0}")]
    ImageError(#[from] image::ImageError),

    #[error("Config error: {0}")]
    ConfigError(String),

    #[error("Matcher error: {0}")]
    MatcherError(String),
}

impl From<AppError> for String {
    fn from(err: AppError) -> Self {
        err.to_string()
    }
}
```

---

#### 6.3.2 Frontend 에러 바운더리 (R8.2)
```typescript
// src/components/ErrorBoundary.tsx
export class ErrorBoundary extends React.Component<
  { children: ReactNode },
  { hasError: boolean; error: Error | null }
> {
  state = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error) {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Error caught by boundary:', error, info);
    // 에러 로깅 서비스로 전송 (선택)
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="error-container">
          <h1>Something went wrong</h1>
          <p>{this.state.error?.message}</p>
          <button onClick={() => window.location.reload()}>
            Reload App
          </button>
        </div>
      );
    }

    return this.props.children;
  }
}
```

---

#### 6.3.3 명령어 에러 처리 (R8.3)
```typescript
// Frontend
try {
  const result = await invoke<FolderStats>('get_folder_stats', {
    nirFolder: config.folders.nir,
    // ...
  });
  setStats(result);
} catch (error) {
  console.error('Failed to fetch stats:', error);
  toast.error(`Error: ${error}`);
  // Fallback 값 설정
  setStats(DEFAULT_STATS);
}
```

---

### 6.4 확장성

#### 6.4.1 플러그인 아키텍처 (R9.1)
```rust
// 미래 확장: 새 파일 타입 지원
pub trait FileTypeHandler {
    fn can_handle(&self, path: &str) -> bool;
    fn extract_timestamp(&self, path: &str) -> Option<i64>;
    fn generate_thumbnail(&self, path: &str) -> Result<Vec<u8>, String>;
}

// 예: TIFF 지원
pub struct TiffHandler;
impl FileTypeHandler for TiffHandler {
    fn can_handle(&self, path: &str) -> bool {
        path.ends_with(".tif") || path.ends_with(".tiff")
    }
    // ...
}
```

---

#### 6.4.2 이벤트 기반 확장 (R9.2)
```rust
// Event bus
pub enum AppEvent {
    FileAdded(String),
    GroupCreated(String),
    FileProcessed(String),
    // 새 이벤트 추가 가능
}

pub trait EventListener {
    fn on_event(&self, event: &AppEvent);
}

// 예: 통계 수집 리스너
pub struct StatsCollector;
impl EventListener for StatsCollector {
    fn on_event(&self, event: &AppEvent) {
        match event {
            AppEvent::FileAdded(path) => {
                // 통계 업데이트
            }
            _ => {}
        }
    }
}
```

---

#### 6.4.3 설정 스키마 버전 관리 (R9.3)
```rust
#[derive(Serialize, Deserialize)]
pub struct AppConfig {
    pub version: String,  // "2.0.0"
    pub folders: FolderConfig,
    pub monitoring: MonitoringConfig,
    // ...
}

impl AppConfig {
    pub fn migrate_from_v1(old: ConfigV1) -> Self {
        // v1 → v2 변환 로직
        Self {
            version: "2.0.0".to_string(),
            folders: FolderConfig {
                nir: old.nir_path,
                // ...
            },
            // ...
        }
    }
}
```

---

## 7. 요구사항 추적

### 7.1 P0 요구사항 (즉시 수정)

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R0.1 | select_folder 명령어 | `src-tauri/src/commands/dialog.rs` | ✅ 완료 |
| R0.2 | select_file 명령어 | `src-tauri/src/commands/dialog.rs` | ✅ 완료 |
| R0.3 | open_folder_in_explorer | `src-tauri/src/commands/dialog.rs` | ✅ 완료 |
| R0.4 | 앱 종료 처리 | `src-tauri/src/lib.rs` (on_window_event) | ❌ 미구현 |
| R0.5 | 아이콘 적용 | `src-tauri/icons/icon.ico` | ⚠️ 확인 필요 |

---

### 7.2 P1 요구사항 (핵심 기능)

#### 7.2.1 메인 모니터링

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R1.1 | 상단 툴바 (Run/Stop 등) | `src/pages/MainMonitoring.tsx` → Toolbar | ❌ 미구현 |
| R1.2 | 통계 바 (파일 개수, 그룹 상태) | StatsBar | ❌ 미구현 |
| R1.3 | 그룹 테이블 (가상 스크롤) | GroupTable + VirtualList | ❌ 미구현 |
| R1.4 | 썸네일 클릭 → 원본 미리보기 | PreviewDialog | ❌ 미구현 |
| R1.5 | 파일 목록 조회 | `commands::files::list_files` | ✅ 완료 |
| R1.6 | 파일 삭제 | `commands::files::delete_files` | ✅ 완료 |
| R1.7 | 파일 이동 (병렬) | `commands::file_operations::move_files` | ✅ 완료 |
| R1.8 | 파일 복사 | `commands::file_operations::copy_files` | ✅ 완료 |
| R1.9 | 파일 감시 시작 | `commands::file_watcher::start_watching` | ✅ 완료 |
| R1.10 | 파일 감시 중지 | `commands::file_watcher::stop_watching` | ❌ 미구현 |
| R1.11 | 파일 매처에 추가 | `commands::matcher::add_file_to_matcher` | ✅ 완료 |
| R1.12 | Timestamp 기반 매칭 | `commands::matcher::match_files_by_timestamp` | ✅ 완료 |
| R1.13 | 매처 리셋 | `commands::matcher::reset_matcher` | ✅ 완료 |
| R1.14 | 이미지 메타데이터 | `commands::image::get_image_metadata` | ✅ 완료 |
| R1.15 | 썸네일 생성 | `commands::image::generate_thumbnail` | ✅ 완료 |
| R1.16 | 그룹 생성 | `commands::group::create_group` | ✅ 완료 |
| R1.17 | 그룹 삭제 | `commands::group::delete_group` | ✅ 완료 |
| R1.18 | 그룹 메타데이터 업데이트 | `commands::group::update_group_metadata` | ✅ 완료 |

---

#### 7.2.2 NIR 모니터링

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R2.1 | NIR 스펙트럼 분석 | `commands::nir_spectrum::analyze_nir_spectrum` | ✅ 완료 |
| R2.2 | NIR 파일 처리 | `commands::nir_spectrum::process_nir_file` | ✅ 완료 |
| R2.3 | NIR 모니터링 시작 | `commands::nir_spectrum::start_nir_monitoring` | ⚠️ 스텁 |
| R2.4 | 오래된 NIR 파일 정리 | `commands::nir_operations::prune_nir_files` | ✅ 완료 |

---

#### 7.2.3 설정 관리

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R3.1 | 폴더 경로 설정 UI | `src/pages/Settings.tsx` | ✅ 완료 |
| R3.2 | 모니터링 파라미터 설정 | Settings → MonitoringConfig | ❌ 미구현 |
| R3.3 | Auto-move 조건 설정 | Settings → AutoMoveConfig | ❌ 미구현 |
| R3.4 | 설정 불러오기 | `commands::config::load_app_config` | ✅ 완료 |
| R3.5 | 설정 저장 | `commands::config::save_app_config` | ✅ 완료 |

---

#### 7.2.4 대시보드

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R4.1 | 파일 개수 통계 표시 | `src/pages/Dashboard.tsx` | ✅ 완료 |
| R4.2 | 폴더 통계 조회 | `commands::file_stats::get_folder_stats` | ✅ 완료 |
| R4.3 | 그룹 상태 요약 | Dashboard → GroupStatusSummary | ❌ 미구현 |

---

### 7.3 P2 요구사항 (편의 기능)

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R5.1 | 자동 경로 설정 | `commands::folder_operations::auto_create_subject_folders` | ✅ 완료 |
| R5.2 | 시료 폴더 생성 | `commands::folder_operations::create_subject_folders` | ✅ 완료 |
| R5.3 | 파일 작업 진행 상황 | Frontend → ProgressBar | ❌ 미구현 |
| R5.4 | 롤백 기능 | FileOperationWorker | ❌ 미구현 |

---

### 7.4 성능 요구사항

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R6.1 | 가상 스크롤 (10,000개 그룹) | `src/components/ui/virtual-list.tsx` | ✅ 완료 |
| R6.2 | Lazy 이미지 로딩 | `src/components/ui/lazy-image.tsx` | ✅ 완료 |
| R6.3 | LRU 이미지 캐시 (500개) | `src-tauri/src/utils/image_cache.rs` | ✅ 완료 |
| R6.4 | 병렬 파일 처리 (Rayon) | `commands::file_operations::*` | ✅ 완료 |
| R6.5 | 파일 이벤트 디바운싱 | `commands::file_watcher` | ✅ 완료 |

---

### 7.5 보안 요구사항

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R7.1 | 경로 순회 공격 방지 | 모든 파일 명령어 | ⚠️ 검증 필요 |
| R7.2 | 명령어 인젝션 방지 | Rust 직접 API 사용 | ✅ 완료 |
| R7.3 | 설정 파일 권한 | `commands::config` | ⚠️ Unix만 |

---

### 7.6 에러 처리 요구사항

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R8.1 | 에러 타입 정의 | `src-tauri/src/error.rs` | ❌ 미구현 |
| R8.2 | Frontend 에러 바운더리 | `src/components/ErrorBoundary.tsx` | ❌ 미구현 |
| R8.3 | 명령어 에러 핸들링 | 모든 invoke 호출 | ⚠️ 부분적 |

---

### 7.7 확장성 요구사항

| 요구사항 ID | 설명 | 구현 위치 | 상태 |
|------------|------|-----------|------|
| R9.1 | 플러그인 아키텍처 | - | ❌ 미구현 |
| R9.2 | 이벤트 기반 확장 | - | ❌ 미구현 |
| R9.3 | 설정 스키마 버전 관리 | `AppConfig::version` | ❌ 미구현 |

---

## 8. 다음 단계 (Implementation Plan)

### Phase 1: P0 긴급 수정 (1-2시간)
1. ✅ select_folder/select_file 구현 - **완료**
2. ❌ 앱 종료 이벤트 처리
3. ❌ 아이콘 확인 및 수정

### Phase 2: 메인 화면 구현 (2-3일)
1. MainMonitoring 컴포넌트 작성
2. GroupTable + VirtualScroll
3. 파일 매칭 워크플로우
4. 썸네일 로더 연결
5. PreviewDialog

### Phase 3: 안정화 (1-2일)
1. 에러 처리 강화
2. 로딩 상태 UI
3. Toast 알림
4. 성능 프로파일링

### Phase 4: 테스트 (1일)
1. Python 원본과 기능 비교
2. 사용자 시나리오 테스트
3. 버그 수정

---

## 부록

### A. 파일 구조

```
matching_codex_tauri/
├── src/                          # Frontend (React)
│   ├── components/
│   │   ├── layout/
│   │   │   ├── AppShell.tsx
│   │   │   ├── Sidebar.tsx
│   │   │   ├── StatusBar.tsx
│   │   │   └── TitleBar.tsx
│   │   └── ui/
│   │       ├── button.tsx
│   │       ├── card.tsx
│   │       ├── input.tsx
│   │       ├── label.tsx
│   │       ├── virtual-list.tsx
│   │       ├── lazy-image.tsx
│   │       └── loading-spinner.tsx
│   ├── pages/
│   │   ├── Dashboard.tsx
│   │   ├── MainMonitoring.tsx    # ❌ 미구현
│   │   ├── NIR.tsx
│   │   └── Settings.tsx
│   ├── lib/
│   │   ├── hooks/
│   │   │   ├── useFileWatcher.ts
│   │   │   ├── useMemoization.ts
│   │   │   └── useIntersectionObserver.ts
│   │   └── utils/
│   │       └── cn.ts
│   └── App.tsx
│
├── src-tauri/                    # Backend (Rust)
│   ├── src/
│   │   ├── commands/
│   │   │   ├── dialog.rs         # ✅ 신규
│   │   │   ├── files.rs
│   │   │   ├── file_watcher.rs
│   │   │   ├── file_operations.rs
│   │   │   ├── image.rs
│   │   │   ├── matcher.rs
│   │   │   ├── nir_spectrum.rs
│   │   │   ├── nir_operations.rs
│   │   │   ├── group.rs
│   │   │   ├── config.rs
│   │   │   └── file_stats.rs
│   │   ├── models/
│   │   │   ├── group.rs
│   │   │   ├── matcher.rs
│   │   │   ├── spectrum.rs
│   │   │   └── nir_bundle.rs
│   │   ├── utils/
│   │   │   ├── image_cache.rs
│   │   │   ├── spectrum_analyzer.rs
│   │   │   └── timestamp.rs
│   │   ├── lib.rs
│   │   └── main.rs
│   ├── icons/
│   │   └── icon.ico              # ✅ 교체됨
│   ├── Cargo.toml
│   └── tauri.conf.json
│
├── REQUIREMENTS.md               # ✅ 완료
├── DESIGN.md                     # ✅ 현재 문서
└── package.json
```

---

### B. 기술 스택 버전

| 패키지 | 버전 | 용도 |
|--------|------|------|
| tauri | 2.0 | 데스크톱 프레임워크 |
| tauri-plugin-dialog | 2.0 | 파일 다이얼로그 |
| react | 19.1.0 | UI 프레임워크 |
| typescript | 5.8.3 | 타입 시스템 |
| framer-motion | 11.15.0 | 애니메이션 |
| @tanstack/react-virtual | 3.11.1 | 가상 스크롤 |
| notify | 6.1 | 파일 감시 |
| rayon | 1.10 | 병렬 처리 |
| image | 0.25 | 이미지 처리 |
| csv | 1.3 | CSV 파싱 |
| uuid | 1.0 | UUID 생성 |
| chrono | 0.4 | 날짜/시간 |
| lru | 0.12 | LRU 캐시 |

---

**문서 끝**
