---
name: chronoview-skills
description: "ChronoView UI 자동화 스킬 모음 (92개 스킬)"
---

# ChronoView UI 자동화 스킬

**사용 가능한 92개 스킬** - ChronoView WPF 애플리케이션을 위한 UI 자동화 명령어입니다.

## 카테고리별 스킬 목록

### 앱 관리 (app:) - 4개
| 스킬 | 설명 |
|------|------|
| `app:launch` | ChronoView 시작 |
| `app:stop` | 모든 ChronoView 프로세스 중지 |
| `app:restart` | ChronoView 재시작 |
| `app:status` | 실행 상태 확인 |

### 일괄 작업 (batch:) - 3개
| 스킬 | 설명 |
|------|------|
| `batch:select-and-move` | 여러 행 선택 후 이동 |
| `batch:select-and-delete` | 여러 행 선택 후 삭제 |
| `batch:export-all` | 모든 데이터 내보내기 |

### 콘솔 로그 (console-logs:) - 3개
| 스킬 | 설명 |
|------|------|
| `console-logs:list` | 로그 파일 목록 |
| `console-logs:tail` | 최근 N줄 읽기 |
| `console-logs:search` | 텍스트 검색 |

### 데이터 패널 (data-panel:) - 7개
| 스킬 | 설명 |
|------|------|
| `data-panel:stats` | 통계 읽기 |
| `data-panel:headers` | 열 헤더 가져오기 |
| `data-panel:rows` | 행 수 가져오기 |
| `data-panel:data` | 모든 데이터 추출 |
| `data-panel:info` | 요약 정보 |
| `data-panel:cell` | 특정 셀 값 |
| `data-panel:export` | JSON으로 내보내기 |

### 파일 작업 (file-ops:) - 15개
| 스킬 | 설명 |
|------|------|
| `file-ops:select-row-index` | 인덱스로 선택 |
| `file-ops:select-group-id` | GroupId로 선택 |
| `file-ops:select-prefix` | 접두사로 선택 |
| `file-ops:select-all` | 모두 선택 |
| `file-ops:clear-selection` | 선택 해제 |
| `file-ops:selected` | 선택된 행 인덱스 |
| `file-ops:move-rows` | 행 이동 |
| `file-ops:move-group-ids` | GroupId 이동 |
| `file-ops:move-prefix` | 접두사 이동 |
| `file-ops:delete-rows` | 행 삭제 |
| `file-ops:delete-group-ids` | GroupId 삭제 |
| `file-ops:wait-move` | 이동 완료 대기 |
| `file-ops:wait-delete` | 삭제 완료 대기 |
| `file-ops:confirm` | 확인 대화상자 처리 |
| `file-ops:verify-deleted` | 삭제 확인 |
| `file-ops:verify-row-count` | 행 수 변경 확인 |

### 로그 패널 (logs:) - 4개
| 스킬 | 설명 |
|------|------|
| `logs:get` | 모든 로그 메시지 |
| `logs:tail` | 최근 N개 메시지 |
| `logs:filter` | 심각도 필터 |
| `logs:search` | 텍스트 검색 |

### 설정 대화상자 (settings-dialog:) - 17개
| 스킬 | 설명 |
|------|------|
| `settings-dialog:open` | 열기 |
| `settings-dialog:close` | 닫기 |
| `settings-dialog:inspect` | 구조 검사 |
| `settings-dialog:status` | 열림 상태 |
| `settings-dialog:path:get-all` | 모든 경로 |
| `settings-dialog:path:get-line1` | Line 1 경로 |
| `settings-dialog:path:get-line2` | Line 2 경로 |
| `settings-dialog:path:get-output` | 출력 경로 |
| `settings-dialog:path:get-quarantine` | 검역 경로 |
| `settings-dialog:path:set` | 경로 설정 |
| `settings-dialog:checkbox:get` | 체크박스 상태 |
| `settings-dialog:checkbox:set` | 체크박스 설정 |
| `settings-dialog:checkbox:list` | 모든 체크박스 |
| `settings-dialog:action:save` | 저장 |
| `settings-dialog:action:apply` | 적용 |
| `settings-dialog:action:cancel` | 취소 |
| `settings-dialog:action:reset` | 재설정 |

### 설정 (setup:) - 4개
| 스킬 | 설명 |
|------|------|
| `setup:verify-config` | 구성 확인 |
| `setup:complete-full` | 전체 워크플로우 완료 |
| `setup:open-settings` | SettingsDialog 열기 |
| `setup:camera-states` | 카메라 상태 |

### 테스트 (test:) - 3개
| 스킬 | 설명 |
|------|------|
| `test:connectivity` | 연결 확인 |
| `test:capabilities` | 자동화 기능 목록 |
| `test:datagrid` | DataGrid 접근성 |

### 툴바 (toolbar:) - 9개
| 스킬 | 설명 |
|------|------|
| `toolbar:start` | 시작 버튼 |
| `toolbar:stop` | 중지 버튼 |
| `toolbar:settings` | 설정 버튼 |
| `toolbar:refresh` | 새로고침 버튼 |
| `toolbar:move` | 이동 버튼 |
| `toolbar:delete` | 삭제 버튼 |
| `toolbar:list` | 모든 버튼 목록 |
| `toolbar:click` | 텍스트로 클릭 |
| `toolbar:enabled` | 버튼 활성화 확인 |

### 유틸리티 (utility:) - 5개
| 스킬 | 설명 |
|------|------|
| `utility:inspect-workflow` | WorkflowPanel 검사 |
| `utility:inspect-log` | LogPanel 검사 |
| `utility:config-path` | 구성 파일 위치 |
| `utility:config-read` | 구성 읽기 |
| `utility:config-get` | 특정 값 가져오기 |

### 윈도우 (windows:) - 6개
| 스킬 | 설명 |
|------|------|
| `windows:main` | MainWindow 찾기 |
| `windows:setup` | SetupWindow 찾기 |
| `windows:setup-complete` | Setup 완료 |
| `windows:settings` | SettingsDialog 찾기 |
| `windows:preview` | ImagePreviewWindow 찾기 |
| `windows:all` | 모든 윈도우 목록 |

### 워크플로우 (workflow:) - 11개
| 스킬 | 설명 |
|------|------|
| `workflow:launch-general` | General 카메라 시작 |
| `workflow:launch-nir` | NIR 1 카메라 시작 |
| `workflow:launch-nir2` | NIR 2 카메라 시작 |
| `workflow:toggle-filtering` | NIR 필터링 전환 |
| `workflow:camera-states` | 카메라 상태 |
| `workflow:path:get-line1` | Line 1 경로 |
| `workflow:path:get-line2` | Line 2 경로 |
| `workflow:path:get-all` | 모든 경로 |
| `workflow:path:set-line1` | Line 1 경로 설정 |
| `workflow:path:set-line2` | Line 2 경로 설정 |
| `workflow:select-tab` | 탭 선택 |

## 사용 예시

```bash
# 앱 시작
/app:launch

# 툴바 조작
/toolbar:start
/toolbar:stop

# 파일 작업
/file-ops:select-all
/file-ops:move-rows --rows 0,1,2

# 데이터 읽기
/data-panel:stats
/data-panel:data --json

# 설정
/settings-dialog:open
/settings-dialog:path:get-all
```

## 관련 문서
- [test-executor.md](../agents/test-executor.md) - 실행 세부 정보
- [test-executor-skills.md](../agents/test-executor-skills.md) - 원본 스킬 레지스트리
