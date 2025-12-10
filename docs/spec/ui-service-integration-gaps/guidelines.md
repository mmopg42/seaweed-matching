# UI Service Integration 적용 가이드 (재사용/이식용)

본 가이드는 `ui-service-integration-gaps`에서 적용한 변경 사항을 다른 프로젝트에 이식할 때 필요한 규칙과 주의사항을 정리한 문서입니다. 구현·테스트 시 아래 항목을 체크하세요.

---

## 1. 파일 작업 (Move/Delete)
1. **Copy-then-Delete 원칙**  
   - Move: `File.Copy`/`CopyDirectory` → 검증(크기/파일 수) → 원본 삭제.  
   - Normal 폴더: 재귀 복사 후 검증, 성공 시 원본 디렉터리 삭제.
2. **Soft Delete(Quarantine)**  
   - Delete는 물리 삭제 대신 `DeleteQuarantinePath`(설정값, 비어 있으면 `BasePath/Trash`)로 이동.  
   - Normal 폴더도 보관 폴더로 이동(충돌 처리 동일).
3. **충돌 처리 & Apply-to-All**  
   - `ConflictResolution` = Overwrite / Skip / Abort.  
   - 첫 충돌 시 결정(sticky) → 이후 동일 적용.  
   - Skip은 “처리됨”으로 집계(실패 아님).
4. **집계/결과**  
   - `FilesProcessed`에 Skip/Overwrite/성공을 포함.  
   - 취소/예외 시 `FailedFiles`에 현재 처리 경로를 남기고 `FilesFailed = Total - Processed`.
5. **검증 규칙**  
   - 파일: 크기 동일성(필요 시 해시).  
   - 폴더: 파일 수/합계 크기 비교 또는 파일별 검증.  
   - 검증 실패 시 삭제/이동 금지, 예외 처리.

## 2. 경로 관리
1. **키 일관성**  
   - `GeneratePathsFromDate` 결과 키: `NIR1/2`, `Normal1/2`, `Cam1~Cam6`, `Output` (대문자).  
   - 호출부에서도 동일 대문자 키 사용.
2. **설정 모델**  
   - `MatchingSettings`: Line1/2 경로(NIR/Normal/Cam1~6), `LineMode`(integrated|separated).  
   - `WorkflowSettings`: `DeleteQuarantinePath`.
3. **Browse 지원**  
   - SettingsDialog에 모든 경로(NIR1/2, Normal1/2, Cam1~6, Output, Quarantine) TextBox + Browse(CommandParameter) 연결.  
   - 기본값: `BasePath/Trash`(Quarantine) 자동 채움.
4. **샘플 폴더**  
   - `CreateSampleFoldersAsync`: 설정된 모든 경로에 `sampleName` 폴더 생성, 존재 시 스킵.
5. **Path Auto Config**  
   - 기본 날짜: 오늘(yyyyMMdd).  
   - 생성된 경로를 설정에 반영 후 저장, 모니터링 재시작 안내.

## 3. UI/UX 패턴
1. **탭 구조**  
   - Line1 / Line2 / Combined 탭. Combined는 좌우 DataGrid + GridSplitter.  
   - DragSelectBehavior 각 DataGrid에 연결, RowStyle IsSelected 양방향 바인딩.
2. **선택 로직**  
   - `GetSelectedGroups()`: Line1/Line2/Combined에 따라 Line1Groups, Line2Groups, (Line1+Line2) 합산 반환.  
   - Move/Delete는 Combined에서 두 라인 선택을 모두 처리(정책 A).
3. **Abnormal 표시**  
   - `FileGroupViewModel`: `IsAbnormal`, `AbnormalReason`, `LineNumber`, `IsSelected`.  
   - RowStyle에서 `IsAbnormal==True` 시 하이라이트, StatusText에 표시.  
   - `AbnormalCount` 통계 업데이트.
4. **Progress/취소**  
   - `IsOperationInProgress`로 버튼 가드, `BeginOperation/EndOperation`, Dispatcher 마샬링.  
   - StatusBar ProgressBar: `ProgressValue`, `IsOperationInProgress`.
5. **경고/가드**  
   - Refresh: 모니터링 OFF 시 경고 후 중단, 컬렉션 직접 Clear 금지(이벤트 기반).  
   - Move/Delete/PathAuto/샘플 생성: IsOperationInProgress 가드, 확인 다이얼로그 후 실행.

## 4. 서비스/이벤트
1. **Orchestrator/Statistics 이벤트**  
   - GroupCreated/Removed/Updated → UI 컬렉션 추가/삭제/갱신 후 통계 업데이트.  
   - FileCountsUpdated/MatchingStatisticsUpdated → Dispatcher로 카운트/매칭 통계 반영(라인별 포함).
2. **DI 수명**  
   - Singleton: ConfigManager, FileWatcher, Stats, Orchestrator.  
   - Transient: FileOperationService, PathManagementService, ViewModels.
3. **이미지 로딩**  
   - FileGroupViewModel에서 비동기 썸네일 로딩, CancellationToken 지원, Placeholder fallback.

## 5. 테스트 체크리스트
- Move: Copy-then-Delete, 충돌 Overwrite/Skip/Abort, 취소/롤백, FailedFiles 집계.
- Delete: Quarantine 이동, 충돌 처리, 취소 시 처리/실패 집계.
- Path Auto Config: 키 대문자 일치, 설정 반영/저장, 기본 Quarantine 경로.
- Browse: 모든 경로 필드에 Browse 동작, 저장 후 재시작 시 값 유지.
- Combined 탭: 양쪽 DataGrid 표시, DragSelect, 선택 합산 후 Move/Delete 동작.
- Abnormal: UI 하이라이트/StatusText/AbnormalCount 반영.
- Refresh: 모니터링 OFF 가드, 토큰 전달, UI 이벤트 기반 갱신.

## 6. 적용 시 유의사항
- 다른 프로젝트로 이식 시, 기존 코드의 경로 키(대소문자), 충돌 처리 정책, 삭제 정책이 상이할 수 있으므로 설정/서비스 인터페이스를 먼저 정합화할 것.
- UI 컬럼 정의를 UserControl로 재사용하면 Line1/Line2/Combined 불일치 리스크를 줄일 수 있음.
- Copy-then-Delete는 성능/스토리지 여유를 전제로 하므로 대용량/네트워크 드라이브 환경에서는 배치/버퍼링 전략을 검토.
- Quarantine 경로는 권한/용량을 사전 점검하고, 기본값(BasePath/Trash) 사용 시 사용자에게 명시적으로 안내.

