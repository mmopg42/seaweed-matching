# Prische Matching Codex - Implementation Plan

**프로젝트**: 김 품질 검사 자동화 시스템
**버전**: v0.2.0
**날짜**: 2025-11-20

---

## 진행 상황 요약

| Phase | 설명 | 진행률 | 상태 |
|-------|------|--------|------|
| Phase 0 | 긴급 수정 (P0) | 100% | ✅ 완료 (Windows 테스트 대기) |
| Phase 1 | 메인 모니터링 화면 | 0% | ⚪ 대기 |
| Phase 2 | NIR 모니터링 강화 | 0% | ⚪ 대기 |
| Phase 3 | 안정화 및 최적화 | 0% | ⚪ 대기 |
| Phase 4 | 테스트 및 배포 | 0% | ⚪ 대기 |

---

## Implementation Plan

### Phase 0: 긴급 수정 (P0) - 2-3시간

#### T0.1: ✅ Dialog 명령어 구현
- **상태**: 완료
- **제목**: select_folder, select_file, open_folder_in_explorer 구현
- **설명**:
  - `tauri-plugin-dialog` 추가
  - `src-tauri/src/commands/dialog.rs` 생성
  - 3개 명령어 구현 및 등록
- **선행 태스크**: 없음
- **완료 조건**:
  - ✅ Cargo.toml에 tauri-plugin-dialog 추가
  - ✅ dialog.rs 파일 생성
  - ✅ lib.rs에 명령어 등록
  - ✅ Settings 페이지에서 Browse 버튼 작동
- **관련 요구사항**: R0.1, R0.2, R0.3
- **담당 파일**:
  - `src-tauri/Cargo.toml`
  - `src-tauri/src/commands/dialog.rs`
  - `src-tauri/src/commands/mod.rs`
  - `src-tauri/src/lib.rs`

---

#### T0.2: ✅ 앱 종료 이벤트 처리
- **상태**: 완료
- **제목**: Window close event 핸들러 구현
- **설명**:
  - Tauri의 `on_window_event` 사용
  - CloseRequested 이벤트 처리
  - 파일 감시 중지 (자동 정리)
  - 워커 스레드 정리 (자동 정리)
  - 캐시 저장 (자동 정리)
- **선행 태스크**: 없음
- **완료 조건**:
  - ✅ lib.rs에 .setup() 및 on_window_event 추가
  - ✅ CloseRequested 이벤트 로깅 구현
  - ✅ Rust 컴파일 성공 (cargo check)
  - ⏳ 앱 종료 시 프로세스 완전히 종료됨 (Windows 테스트 필요)
- **관련 요구사항**: R0.4
- **담당 파일**:
  - `src-tauri/src/lib.rs`
- **예상 시간**: 60분

**구현 가이드**:
```rust
// src-tauri/src/lib.rs
.setup(|app| {
    let window = app.get_window("main").unwrap();
    window.on_window_event(|event| {
        if let tauri::WindowEvent::CloseRequested { api, .. } = event {
            // 1. 파일 감시 중지
            // 2. 워커 스레드 종료
            // 3. 캐시 저장
            println!("App closing...");
        }
    });
    Ok(())
})
```

---

#### T0.3: ✅ 아이콘 적용 확인 및 수정
- **상태**: 완료 (Windows 테스트 대기)
- **제목**: Windows에서 .exe 아이콘 확인 및 캐시 클리어
- **설명**:
  - Windows에서 빌드 후 .exe 아이콘 확인
  - 아이콘 캐시 클리어 (필요시)
  - tauri.conf.json 재확인
  - 다양한 크기 아이콘 생성 (필요시)
- **선행 태스크**: 없음
- **완료 조건**:
  - ✅ icon.ico 파일 존재 확인 (148K, Nov 20 업데이트됨)
  - ✅ tauri.conf.json에 icon.ico 설정 확인
  - ⏳ tauri-app.exe 아이콘이 app_icon.ico로 표시됨 (Windows 빌드 필요)
  - ⏳ MSI 설치 프로그램 아이콘 확인 (Windows 빌드 필요)
- **관련 요구사항**: R0.5
- **담당 파일**:
  - `src-tauri/icons/icon.ico`
  - `src-tauri/tauri.conf.json`
- **예상 시간**: 30분

**확인 방법**:
```bash
# Windows에서
1. npm run tauri build
2. src-tauri/target/release/tauri-app.exe 우클릭 → 속성 → 아이콘 확인
3. IE4uinit.exe -show  # 아이콘 캐시 재생성 (관리자 권한)
4. 탐색기 재시작
```

---

### Phase 1: 메인 모니터링 화면 구현 - 2-3일

#### T1.1: FileGroup 타입 정의 및 Store 생성
- **상태**: ⚪ 미시작
- **제목**: Frontend 데이터 모델 및 Zustand Store 구현
- **설명**:
  - TypeScript 타입 정의 (FileGroup, GroupStatus, GroupMetadata)
  - Zustand store 생성 (useGroupStore)
  - 기본 CRUD 액션 구현
- **선행 태스크**: 없음
- **완료 조건**:
  - [ ] `src/types/group.ts` 생성
  - [ ] `src/store/useGroupStore.ts` 생성
  - [ ] addGroup, updateGroup, deleteGroup 액션 구현
  - [ ] toggleSelection, selectAll, deselectAll 구현
  - [ ] TypeScript 컴파일 에러 없음
- **관련 요구사항**: R1.1, R1.3
- **담당 파일**:
  - `src/types/group.ts` (신규)
  - `src/store/useGroupStore.ts` (신규)
- **예상 시간**: 60분

**구현 가이드**:
```typescript
// src/types/group.ts
export interface FileGroup {
  id: string;
  timestamp: number;
  status: GroupStatus;
  normal_file?: string;
  normal2_file?: string;
  nir_files: string[];
  nir2_files: string[];
  cam_files: string[];
  selected: boolean;
  metadata: GroupMetadata;
}

export type GroupStatus = 'Pending' | 'Running' | 'Completed' | 'Error';

// src/store/useGroupStore.ts
import { create } from 'zustand';

interface GroupStore {
  groups: FileGroup[];
  selectedGroups: Set<string>;
  isMonitoring: boolean;
  addGroup: (group: FileGroup) => void;
  // ...
}

export const useGroupStore = create<GroupStore>((set) => ({
  groups: [],
  selectedGroups: new Set(),
  isMonitoring: false,
  // ...
}));
```

---

#### T1.2: Toolbar 컴포넌트 구현
- **상태**: ⚪ 미시작
- **제목**: 상단 툴바 (설정, Run/Stop, 이동 등) 구현
- **설명**:
  - FlowLayout 또는 Flex로 버튼 배치
  - 버튼: 설정, 설정폴더열기, 이동대상폴더열기, 경로자동, 시료 폴더 생성
  - 버튼: 이미지 불러오기, Run, Stop, 전체선택/해제, 행삭제, 이동
  - DateInput, SubjectNameInput
  - NIR 개수, 데이터 개수 입력
- **선행 태스크**: T0.1 (dialog 명령어)
- **완료 조건**:
  - [ ] `src/components/monitoring/Toolbar.tsx` 생성
  - [ ] 모든 버튼 렌더링
  - [ ] 설정 버튼 클릭 시 Settings 다이얼로그 열림
  - [ ] 폴더 열기 버튼 작동 (open_folder_in_explorer)
  - [ ] Run/Stop 버튼 토글
  - [ ] UI 반응형 (화면 크기에 따라 줄바꿈)
- **관련 요구사항**: R1.1
- **담당 파일**:
  - `src/components/monitoring/Toolbar.tsx` (신규)
- **예상 시간**: 90분

---

#### T1.3: StatsBar 컴포넌트 구현
- **상태**: ⚪ 미시작
- **제목**: 통계 바 (파일 개수, 그룹 상태) 구현
- **설명**:
  - 2줄 구조
  - 1줄: 파일 개수 (NIR, NIR2, Normal, Normal2, Cam1-6)
  - 2줄: 그룹 상태 (총 그룹, Pending, Running, Completed, Error, 선택됨)
  - 5초마다 자동 갱신 (get_folder_stats)
- **선행 태스크**: T1.1
- **완료 조건**:
  - [ ] `src/components/monitoring/StatsBar.tsx` 생성
  - [ ] 파일 개수 표시 (10개 폴더)
  - [ ] 그룹 상태 통계 표시
  - [ ] 5초 간격 자동 갱신
  - [ ] 로딩 상태 표시
- **관련 요구사항**: R1.2, R4.2
- **담당 파일**:
  - `src/components/monitoring/StatsBar.tsx` (신규)
- **예상 시간**: 60분

---

#### T1.4: GroupRow 컴포넌트 구현
- **상태**: ⚪ 미시작
- **제목**: 그룹 테이블 행 컴포넌트 (썸네일 포함)
- **설명**:
  - 체크박스
  - Timestamp 표시
  - 상태 아이콘 (Pending/Running/Completed/Error)
  - NIR, NIR2, Normal, Normal2, Cam1-6 썸네일
  - 썸네일 클릭 시 이벤트 발생
  - LazyImage 사용
- **선행 태스크**: T1.1
- **완료 조건**:
  - [ ] `src/components/monitoring/GroupRow.tsx` 생성
  - [ ] 모든 필드 렌더링
  - [ ] 썸네일 Lazy Loading
  - [ ] 체크박스 토글 동작
  - [ ] 상태에 따른 스타일링 (색상, 아이콘)
- **관련 요구사항**: R1.3
- **담당 파일**:
  - `src/components/monitoring/GroupRow.tsx` (신규)
- **예상 시간**: 90분

---

#### T1.5: GroupTable 컴포넌트 구현 (가상 스크롤)
- **상태**: ⚪ 미시작
- **제목**: 그룹 테이블 (VirtualList 통합)
- **설명**:
  - VirtualList 사용 (10,000개 그룹 지원)
  - GroupRow 렌더링
  - 헤더 (컬럼명)
  - 정렬 기능 (timestamp, status)
- **선행 태스크**: T1.4
- **완료 조건**:
  - [ ] `src/components/monitoring/GroupTable.tsx` 생성
  - [ ] VirtualList 통합
  - [ ] 10,000개 그룹 테스트 (60fps 유지)
  - [ ] 정렬 기능 작동
  - [ ] 빈 상태 표시 ("No groups")
- **관련 요구사항**: R1.3, R6.1
- **담당 파일**:
  - `src/components/monitoring/GroupTable.tsx` (신규)
- **예상 시간**: 75분

---

#### T1.6: PreviewDialog 컴포넌트 구현
- **상태**: ⚪ 미시작
- **제목**: 이미지 원본 크기 미리보기 다이얼로그
- **설명**:
  - Modal 다이얼로그
  - 이미지 전체 크기 표시
  - 확대/축소 기능
  - ESC 키로 닫기
  - 배경 클릭 시 닫기
- **선행 태스크**: 없음
- **완료 조건**:
  - [ ] `src/components/monitoring/PreviewDialog.tsx` 생성
  - [ ] 이미지 로딩 상태 표시
  - [ ] 확대/축소 (Ctrl + Wheel)
  - [ ] ESC, 배경 클릭으로 닫기
  - [ ] 키보드 화살표로 이전/다음 이미지
- **관련 요구사항**: R1.4
- **담당 파일**:
  - `src/components/monitoring/PreviewDialog.tsx` (신규)
- **예상 시간**: 90분

---

#### T1.7: 파일 매칭 워크플로우 구현
- **상태**: ⚪ 미시작
- **제목**: Rust Matcher를 사용한 파일 그룹핑
- **설명**:
  - useFileWatcher 훅에서 파일 변경 감지
  - add_file_to_matcher 호출
  - match_files_by_timestamp 호출
  - FileMatch → FileGroup 변환
  - useGroupStore.addGroup 호출
- **선행 태스크**: T1.1
- **완료 조건**:
  - [ ] `src/lib/hooks/useFileMatcher.ts` 생성
  - [ ] 파일 추가 시 자동 매칭
  - [ ] ±10초 오차 범위 내 그룹핑
  - [ ] UI 자동 업데이트
  - [ ] 중복 그룹 방지
- **관련 요구사항**: R1.12
- **담당 파일**:
  - `src/lib/hooks/useFileMatcher.ts` (신규)
- **예상 시간**: 90분

**구현 가이드**:
```typescript
// src/lib/hooks/useFileMatcher.ts
export function useFileMatcher() {
  const addGroup = useGroupStore((state) => state.addGroup);

  const matchFiles = async (files: string[]) => {
    // 1. 각 파일을 matcher에 추가
    for (const file of files) {
      const timestamp = extractTimestamp(file);
      const fileType = detectFileType(file);
      await invoke('add_file_to_matcher', { path: file, timestamp, fileType });
    }

    // 2. Timestamp 기반 매칭
    const matches = await invoke<FileMatch[]>('match_files_by_timestamp', {
      toleranceSec: 10
    });

    // 3. FileMatch → FileGroup 변환
    for (const match of matches) {
      const group = convertToFileGroup(match);
      addGroup(group);
    }
  };

  return { matchFiles };
}
```

---

#### T1.8: MainMonitoring 페이지 통합
- **상태**: ⚪ 미시작
- **제목**: 모든 컴포넌트를 MainMonitoring 페이지로 통합
- **설명**:
  - Toolbar, StatsBar, GroupTable, PreviewDialog 조합
  - 이벤트 핸들러 연결
  - Run/Stop 버튼 로직
  - 파일 이동/삭제 로직
- **선행 태스크**: T1.2, T1.3, T1.5, T1.6, T1.7
- **완료 조건**:
  - [ ] `src/pages/MainMonitoring.tsx` 생성
  - [ ] 모든 컴포넌트 렌더링
  - [ ] Run 클릭 시 파일 감시 시작
  - [ ] Stop 클릭 시 파일 감시 중지
  - [ ] 이동 버튼으로 선택 그룹 이동
  - [ ] 행삭제 버튼으로 그룹 삭제
- **관련 요구사항**: R1.1, R1.2, R1.3, R1.4
- **담당 파일**:
  - `src/pages/MainMonitoring.tsx` (신규)
- **예상 시간**: 90분

---

#### T1.9: AppShell 라우팅 업데이트
- **상태**: ⚪ 미시작
- **제목**: Sidebar에 MainMonitoring 추가 및 기본 페이지로 설정
- **설명**:
  - Sidebar에 "Main Monitoring" 메뉴 추가
  - AppShell에서 MainMonitoring 렌더링
  - 기본 페이지를 MainMonitoring으로 변경
  - Dashboard는 요약 통계만 표시
- **선행 태스크**: T1.8
- **완료 조건**:
  - [ ] Sidebar.tsx 업데이트
  - [ ] AppShell.tsx 라우팅 추가
  - [ ] 앱 시작 시 MainMonitoring 표시
  - [ ] 메뉴 전환 작동
- **관련 요구사항**: R1.1
- **담당 파일**:
  - `src/components/layout/Sidebar.tsx`
  - `src/components/layout/AppShell.tsx`
- **예상 시간**: 30분

---

### Phase 2: NIR 모니터링 강화 - 1일

#### T2.1: NIR 모니터링 자동화
- **상태**: ⚪ 미시작
- **제목**: NIR 파일 자동 모니터링 및 처리
- **설명**:
  - start_nir_monitoring 명령어 완성
  - 자동으로 NIR 파일 분석
  - 김 검출 시 자동 이동
  - 미검출 시 자동 삭제
  - 이벤트 emit (nir-processed)
- **선행 태스크**: 없음
- **완료 조건**:
  - [ ] `src-tauri/src/commands/nir_spectrum.rs` 업데이트
  - [ ] NIR 폴더 감시
  - [ ] .txt 파일 자동 분석
  - [ ] Move/Delete 자동 실행
  - [ ] Frontend에서 이벤트 수신
- **관련 요구사항**: R2.3
- **담당 파일**:
  - `src-tauri/src/commands/nir_spectrum.rs`
- **예상 시간**: 90분

---

#### T2.2: NIR 결과 히스토리 저장
- **상태**: ⚪ 미시작
- **제목**: NIR 분석 결과 JSON 파일로 저장
- **설명**:
  - nir_results.json 파일 생성
  - 각 분석 결과 저장 (timestamp, file, action, regions)
  - Frontend에서 히스토리 조회
  - 통계 계산 (총 분석, Move, Delete 비율)
- **선행 태스크**: T2.1
- **완료 조건**:
  - [ ] nir_results.json 저장 로직
  - [ ] get_nir_history 명령어 추가
  - [ ] NIR 페이지에 히스토리 탭 추가
  - [ ] 통계 차트 표시
- **관련 요구사항**: R2.3
- **담당 파일**:
  - `src-tauri/src/commands/nir_spectrum.rs`
  - `src/pages/NIR.tsx`
- **예상 시간**: 75분

---

#### T2.3: Prune 기능 스케줄링
- **상태**: ⚪ 미시작
- **제목**: 오래된 NIR 파일 자동 정리 (스케줄러)
- **설명**:
  - 설정에서 Prune 주기 설정 (예: 매일 자정)
  - 자동으로 prune_nir_files 실행
  - 삭제 개수 로그
  - 알림 표시
- **선행 태스크**: T2.1
- **완료 조건**:
  - [ ] Settings에 Prune 설정 추가
  - [ ] 백그라운드 스케줄러 구현 (Rust)
  - [ ] 실행 로그 저장
  - [ ] Frontend 알림
- **관련 요구사항**: R2.4
- **담당 파일**:
  - `src-tauri/src/utils/scheduler.rs` (신규)
  - `src/pages/Settings.tsx`
- **예상 시간**: 90분

---

### Phase 3: 안정화 및 최적화 - 1-2일

#### T3.1: 에러 타입 정의
- **상태**: ⚪ 미시작
- **제목**: Rust 에러 타입 통합 (thiserror)
- **설명**:
  - thiserror crate 추가
  - AppError enum 정의
  - 모든 명령어에서 AppError 사용
  - 에러 메시지 표준화
- **선행 태스크**: 없음
- **완료 조건**:
  - [ ] `src-tauri/src/error.rs` 생성
  - [ ] AppError 정의 (FileNotFound, PermissionDenied 등)
  - [ ] 모든 명령어 시그니처 변경
  - [ ] 컴파일 에러 없음
- **관련 요구사항**: R8.1
- **담당 파일**:
  - `src-tauri/src/error.rs` (신규)
  - `src-tauri/src/commands/*.rs` (수정)
- **예상 시간**: 90분

---

#### T3.2: ErrorBoundary 컴포넌트
- **상태**: ⚪ 미시작
- **제목**: React ErrorBoundary 구현
- **설명**:
  - Class 컴포넌트로 ErrorBoundary 생성
  - 에러 발생 시 Fallback UI
  - Reload 버튼
  - 에러 로깅 (콘솔)
- **선행 태스크**: 없음
- **완료 조건**:
  - [ ] `src/components/ErrorBoundary.tsx` 생성
  - [ ] App.tsx에 적용
  - [ ] 에러 발생 시 Fallback 표시
  - [ ] Reload 버튼 작동
- **관련 요구사항**: R8.2
- **담당 파일**:
  - `src/components/ErrorBoundary.tsx` (신규)
  - `src/App.tsx`
- **예상 시간**: 45분

---

#### T3.3: 로딩 상태 통합
- **상태**: ⚪ 미시작
- **제목**: 모든 비동기 작업에 로딩 상태 추가
- **설명**:
  - LoadingSpinner 사용
  - 파일 이동/복사 시 프로그레스
  - 이미지 로딩 시 스켈레톤
  - 데이터 fetching 시 로딩 표시
- **선행 태스크**: T1.8
- **완료 조건**:
  - [ ] 모든 invoke 호출에 loading 상태
  - [ ] 프로그레스 바 (파일 작업)
  - [ ] 스켈레톤 (이미지)
  - [ ] 스피너 (데이터 로딩)
- **관련 요구사항**: R5.3
- **담당 파일**:
  - 모든 페이지 컴포넌트
- **예상 시간**: 60분

---

#### T3.4: Toast 알림 통합
- **상태**: ⚪ 미시작
- **제목**: Sonner 사용하여 알림 표시
- **설명**:
  - 성공/실패 메시지
  - 파일 이동 완료
  - NIR 분석 완료
  - 에러 메시지
- **선행 태스크**: T1.8
- **완료 조건**:
  - [ ] Toaster 컴포넌트 추가 (App.tsx)
  - [ ] 모든 작업 완료 시 toast
  - [ ] 에러 발생 시 toast.error
  - [ ] 중복 알림 방지 (debounce)
- **관련 요구사항**: 없음 (UX 개선)
- **담당 파일**:
  - `src/App.tsx`
  - 모든 페이지 컴포넌트
- **예상 시간**: 45분

---

#### T3.5: 설정 영속성 개선
- **상태**: ⚪ 미시작
- **제목**: 설정 자동 저장 및 복원
- **설명**:
  - 앱 시작 시 설정 자동 로드
  - 설정 변경 시 자동 저장 (debounce)
  - 기본값 제공
  - 마이그레이션 (버전 관리)
- **선행 태스크**: T3.1
- **완료 조건**:
  - [ ] 앱 시작 시 load_app_config 호출
  - [ ] useEffect로 설정 변경 감지
  - [ ] 1초 debounce 후 자동 저장
  - [ ] 기본값 fallback
- **관련 요구사항**: R3.4, R3.5, R9.3
- **담당 파일**:
  - `src/pages/Settings.tsx`
  - `src-tauri/src/commands/config.rs`
- **예상 시간**: 60분

---

#### T3.6: 경로 검증 강화
- **상태**: ⚪ 미시작
- **제목**: 파일 경로 보안 검증
- **설명**:
  - 모든 파일 명령어에 validate_path 추가
  - 경로 순회 공격 방지 (..)
  - 절대 경로 검증
  - 존재 여부 확인
- **선행 태스크**: T3.1
- **완료 조건**:
  - [ ] `src-tauri/src/utils/path_validator.rs` 생성
  - [ ] validate_path 함수 구현
  - [ ] 모든 파일 명령어에 적용
  - [ ] 테스트 (../ 경로 차단)
- **관련 요구사항**: R7.1
- **담당 파일**:
  - `src-tauri/src/utils/path_validator.rs` (신규)
  - `src-tauri/src/commands/*.rs` (수정)
- **예상 시간**: 60분

---

#### T3.7: 성능 프로파일링
- **상태**: ⚪ 미시작
- **제목**: 10,000개 그룹 성능 테스트
- **설명**:
  - 10,000개 가짜 그룹 생성
  - 스크롤 성능 측정 (fps)
  - 메모리 사용량 측정
  - 병목 지점 파악
  - 최적화 적용
- **선행 태스크**: T1.5
- **완료 조건**:
  - [ ] 테스트 데이터 생성 스크립트
  - [ ] Chrome DevTools 프로파일링
  - [ ] 60fps 유지 확인
  - [ ] 메모리 누수 없음
- **관련 요구사항**: R6.1
- **담당 파일**:
  - 없음 (테스트)
- **예상 시간**: 90분

---

### Phase 4: 테스트 및 배포 - 1일

#### T4.1: 사용자 시나리오 테스트
- **상태**: ⚪ 미시작
- **제목**: Python 원본과 기능 비교 테스트
- **설명**:
  - 파일 모니터링 시나리오
  - 파일 그룹핑 시나리오
  - 이미지 뷰어 시나리오
  - NIR 분석 시나리오
  - 설정 저장/불러오기 시나리오
- **선행 태스크**: T1.8, T2.1
- **완료 조건**:
  - [ ] 테스트 케이스 문서 작성
  - [ ] 각 시나리오 실행
  - [ ] 버그 리스트 작성
  - [ ] 크리티컬 버그 수정
- **관련 요구사항**: 모든 R
- **담당 파일**:
  - `TESTING.md` (신규)
- **예상 시간**: 120분

**테스트 시나리오 예시**:
```markdown
# 시나리오 1: 파일 모니터링
1. 설정에서 NIR 폴더 설정
2. Run 버튼 클릭
3. NIR 폴더에 파일 10개 추가
4. 10초 대기
5. 확인: 그룹 테이블에 그룹 표시됨
6. 확인: 썸네일 로딩됨
7. Stop 버튼 클릭
```

---

#### T4.2: 버그 수정
- **상태**: ⚪ 미시작
- **제목**: T4.1에서 발견된 버그 수정
- **설명**:
  - 버그 재현
  - 원인 파악
  - 수정
  - 재테스트
- **선행 태스크**: T4.1
- **완료 조건**:
  - [ ] 모든 크리티컬 버그 수정
  - [ ] 재테스트 통과
  - [ ] 알려진 이슈 문서화
- **관련 요구사항**: 없음
- **담당 파일**:
  - 다양함
- **예상 시간**: 가변적 (60-180분)

---

#### T4.3: 빌드 및 패키징 테스트
- **상태**: ⚪ 미시작
- **제목**: Windows 빌드 및 설치 테스트
- **설명**:
  - npm run tauri build 실행
  - .exe 파일 테스트
  - MSI 설치 프로그램 테스트
  - 아이콘 확인
  - 설치/제거 테스트
- **선행 태스크**: T4.2
- **완료 조건**:
  - [ ] 빌드 성공 (경고 없음)
  - [ ] .exe 실행 성공
  - [ ] MSI 설치 성공
  - [ ] 아이콘 정상 표시
  - [ ] 제거 후 설정 파일 삭제 확인
- **관련 요구사항**: R0.5
- **담당 파일**:
  - 없음 (빌드)
- **예상 시간**: 45분

---

#### T4.4: 문서 업데이트
- **상태**: ⚪ 미시작
- **제목**: README, CHANGELOG 작성
- **설명**:
  - README.md 업데이트 (설치, 사용법)
  - CHANGELOG.md 작성 (v0.2.0)
  - 스크린샷 추가
  - 알려진 이슈 문서화
- **선행 태스크**: T4.3
- **완료 조건**:
  - [ ] README.md 업데이트
  - [ ] CHANGELOG.md 작성
  - [ ] 스크린샷 3장 이상
  - [ ] Known Issues 섹션
- **관련 요구사항**: 없음
- **담당 파일**:
  - `README.md`
  - `CHANGELOG.md` (신규)
- **예상 시간**: 60분

---

## 리팩터링 및 개선 태스크

### R1: Rust 경고 제거
- **상태**: ⚪ 미시작
- **제목**: get_matches, extract_datetime_from_nir_key 경고 제거
- **설명**:
  - get_matches를 실제로 사용하거나 #[allow(dead_code)] 추가
  - extract_datetime_from_nir_key 활용 또는 제거
- **선행 태스크**: T1.7
- **완료 조건**:
  - [ ] cargo build 시 경고 없음
  - [ ] get_matches 활용 (또는 allow)
  - [ ] extract_datetime_from_nir_key 활용 (또는 allow)
- **관련 요구사항**: 없음 (코드 품질)
- **담당 파일**:
  - `src-tauri/src/models/matcher.rs`
  - `src-tauri/src/utils/timestamp.rs`
- **예상 시간**: 30분

---

### R2: Python 백엔드 제거 (선택)
- **상태**: ⚪ 미시작
- **제목**: python_backend 폴더 삭제
- **설명**:
  - Python 관련 코드 제거 (선택)
  - ENABLE_PYTHON 환경 변수 제거
  - lib.rs 단순화
- **선행 태스크**: T1.8
- **완료 조건**:
  - [ ] python_backend/ 폴더 삭제 (또는 유지)
  - [ ] lib.rs에서 Python 코드 제거 (또는 유지)
  - [ ] 빌드 성공
- **관련 요구사항**: 없음
- **담당 파일**:
  - `python_backend/` (삭제 여부)
  - `src-tauri/src/lib.rs`
  - `src-tauri/src/python/` (삭제 여부)
- **예상 시간**: 30분

**권장**: 일단 유지하고, 나중에 확장 가능성이 없으면 제거

---

### R3: TypeScript strict mode 활성화
- **상태**: ⚪ 미시작
- **제목**: tsconfig.json strict: true 설정
- **설명**:
  - strict 모드 활성화
  - 타입 에러 수정
  - any 타입 제거
- **선행 태스크**: T1.8
- **완료 조건**:
  - [ ] tsconfig.json strict: true
  - [ ] 모든 TypeScript 에러 수정
  - [ ] any 타입 최소화 (virtual-list 제외)
- **관련 요구사항**: 없음 (코드 품질)
- **담당 파일**:
  - `tsconfig.json`
  - 모든 `.ts`, `.tsx` 파일
- **예상 시간**: 90분

---

## 마일스톤

### M1: P0 완료 (T0.2, T0.3)
- **목표**: 긴급 수정 완료
- **완료 조건**:
  - ✅ Dialog 명령어 작동 (완료)
  - [ ] 앱 정상 종료
  - [ ] 아이콘 적용
- **예상 완료일**: 1일 이내

### M2: 메인 화면 완성 (T1.1~T1.9)
- **목표**: Python 원본과 동일한 메인 화면
- **완료 조건**:
  - [ ] 파일 그룹 테이블 표시
  - [ ] 썸네일 로딩
  - [ ] 파일 매칭 자동화
  - [ ] 이동/삭제 기능
- **예상 완료일**: 3-4일

### M3: 안정화 (T3.1~T3.7)
- **목표**: 프로덕션 준비
- **완료 조건**:
  - [ ] 에러 처리 완료
  - [ ] 성능 최적화
  - [ ] 보안 강화
- **예상 완료일**: 5-6일

### M4: 배포 (T4.1~T4.4)
- **목표**: v0.2.0 릴리스
- **완료 조건**:
  - [ ] 모든 테스트 통과
  - [ ] 문서 작성
  - [ ] Windows 빌드 성공
- **예상 완료일**: 7일

---

## 우선순위 요약

### 🔴 High Priority (1-2일 내)
1. T0.2: 앱 종료 이벤트 처리
2. T0.3: 아이콘 확인
3. T1.1: FileGroup 타입 및 Store
4. T1.4: GroupRow 컴포넌트
5. T1.7: 파일 매칭 워크플로우

### 🟡 Medium Priority (3-5일)
6. T1.2: Toolbar
7. T1.3: StatsBar
8. T1.5: GroupTable
9. T1.6: PreviewDialog
10. T1.8: MainMonitoring 통합

### 🟢 Low Priority (1주 이상)
11. T2.1~T2.3: NIR 강화
12. T3.1~T3.7: 안정화
13. T4.1~T4.4: 테스트 및 배포
14. R1~R3: 리팩터링

---

## 일일 계획 예시

### Day 1
- ✅ T0.1: Dialog 명령어 (완료)
- [ ] T0.2: 앱 종료 이벤트 (60분)
- [ ] T0.3: 아이콘 확인 (30분)
- [ ] T1.1: FileGroup 타입/Store (60분)
- [ ] T1.4: GroupRow (90분)
- **총**: 4시간

### Day 2
- [ ] T1.2: Toolbar (90분)
- [ ] T1.3: StatsBar (60분)
- [ ] T1.5: GroupTable (75분)
- [ ] T1.6: PreviewDialog (90분)
- **총**: 5시간

### Day 3
- [ ] T1.7: 파일 매칭 (90분)
- [ ] T1.8: MainMonitoring 통합 (90분)
- [ ] T1.9: AppShell 라우팅 (30분)
- [ ] T4.1: 사용자 테스트 (120분)
- **총**: 5.5시간

### Day 4-7
- NIR 강화, 안정화, 테스트, 배포

---

## 태스크 체크리스트

**Phase 0 (P0)**
- [x] T0.1: Dialog 명령어
- [ ] T0.2: 앱 종료 이벤트
- [ ] T0.3: 아이콘 확인

**Phase 1 (메인 화면)**
- [ ] T1.1: FileGroup 타입/Store
- [ ] T1.2: Toolbar
- [ ] T1.3: StatsBar
- [ ] T1.4: GroupRow
- [ ] T1.5: GroupTable
- [ ] T1.6: PreviewDialog
- [ ] T1.7: 파일 매칭
- [ ] T1.8: MainMonitoring 통합
- [ ] T1.9: AppShell 라우팅

**Phase 2 (NIR)**
- [ ] T2.1: NIR 자동화
- [ ] T2.2: NIR 히스토리
- [ ] T2.3: Prune 스케줄링

**Phase 3 (안정화)**
- [ ] T3.1: 에러 타입
- [ ] T3.2: ErrorBoundary
- [ ] T3.3: 로딩 상태
- [ ] T3.4: Toast 알림
- [ ] T3.5: 설정 영속성
- [ ] T3.6: 경로 검증
- [ ] T3.7: 성능 프로파일링

**Phase 4 (테스트)**
- [ ] T4.1: 사용자 테스트
- [ ] T4.2: 버그 수정
- [ ] T4.3: 빌드 테스트
- [ ] T4.4: 문서 업데이트

**리팩터링**
- [ ] R1: Rust 경고 제거
- [ ] R2: Python 백엔드 제거 (선택)
- [ ] R3: TypeScript strict (선택)

---

**문서 끝**

**다음 단계**: T0.2 (앱 종료 이벤트 처리) 또는 T1.1 (FileGroup 타입/Store) 부터 시작하세요!
