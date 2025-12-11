# Requirements Document

## Introduction

ChronoView 애플리케이션의 파일 매칭 시스템은 Normal 폴더, NIR 파일, Camera 파일들을 타임스탬프 기반으로 그룹화합니다. 현재 로깅 시스템은 매칭 실패의 원인을 파악하기 어렵고, UI에 충분한 진단 정보를 제공하지 못합니다. 이 기능은 파일 매칭 과정을 상세하게 추적하고, 매칭 성공/실패의 이유를 명확하게 UI에 표시하는 진단 로깅 시스템을 구현합니다.

## Glossary

- **FileGroupMatcherService**: 파일들을 타임스탬프 기반으로 그룹화하는 핵심 서비스
- **FileGroup**: 하나의 행(row)을 나타내는 매칭된 파일들의 집합 (Normal 폴더, NIR 파일, Camera 파일들)
- **Normal Folder**: stitched_original.png를 포함하는 메인 이미지 폴더 (normal1, normal2)
- **NIR File**: Near-Infrared 스펙트럼 데이터 파일 (nir1, nir2)
- **Camera File**: 카메라로 촬영된 이미지 파일 (cam1-cam6)
- **Matching Logic**: 타임스탬프 차이를 기반으로 파일들을 그룹에 할당하는 알고리즘
- **LogPanel**: UI에서 로그 메시지를 표시하는 컨트롤
- **Diagnostic Logging**: 매칭 과정의 상세한 정보를 기록하고 표시하는 로깅 시스템
- **Empty Row**: 일부 또는 모든 파일이 매칭되지 않은 그룹

## Requirements

### Requirement 1

**User Story:** 개발자로서, 각 파일 그룹이 어떻게 매칭되었는지 상세한 정보를 UI에서 확인하고 싶습니다. 그래야 매칭 로직이 올바르게 동작하는지 검증할 수 있습니다.

#### Acceptance Criteria

1. WHEN FileGroupMatcherService가 파일 그룹을 생성할 때 THEN 시스템은 각 그룹의 생성 이유와 매칭된 파일들의 정보를 DEBUG 레벨로 로깅해야 합니다
2. WHEN Normal 폴더가 그룹에 할당될 때 THEN 시스템은 폴더 이름, 추출된 타임스탬프, 파일 경로를 로깅해야 합니다
3. WHEN Camera 파일이 그룹에 매칭될 때 THEN 시스템은 카메라 타입(cam1-cam6), 파일명, 타임스탬프, 기준 타임스탬프와의 시간 차이(초 단위)를 로깅해야 합니다
4. WHEN NIR 파일이 그룹에 매칭될 때 THEN 시스템은 NIR 키, 파일 경로, 타임스탬프, 그룹 타임스탬프와의 시간 차이(초 단위)를 로깅해야 합니다
5. WHEN 그룹이 최종적으로 완성될 때 THEN 시스템은 그룹 ID, 라인 번호, 포함된 파일 타입 목록을 요약하여 로깅해야 합니다

### Requirement 2

**User Story:** 개발자로서, 파일이 매칭되지 않은 이유를 명확하게 알고 싶습니다. 그래야 설정을 조정하거나 데이터 문제를 해결할 수 있습니다.

#### Acceptance Criteria

1. WHEN Camera 파일이 어떤 그룹에도 매칭되지 않을 때 THEN 시스템은 파일명, 타임스탬프, 매칭 실패 이유(시간 범위 초과, 사용 가능한 그룹 없음 등)를 ERROR 레벨로 로깅해야 합니다
2. WHEN NIR 파일이 어떤 그룹에도 매칭되지 않을 때 THEN 시스템은 NIR 키, 타임스탬프, 가장 가까운 그룹과의 시간 차이, 설정된 최대 허용 시간 차이를 ERROR 레벨로 로깅해야 합니다
3. WHEN Normal 폴더에서 타임스탬프 추출이 실패할 때 THEN 시스템은 폴더명과 실패 이유를 ERROR 레벨로 로깅해야 합니다
4. WHEN 그룹에 일부 파일만 매칭되어 빈 슬롯이 있을 때 THEN 시스템은 그룹 ID와 비어있는 파일 타입 목록을 WARNING 레벨로 로깅해야 합니다
5. WHEN 매칭 프로세스가 완료된 후 THEN 시스템은 전체 통계(총 그룹 수, 완전 매칭된 그룹 수, 부분 매칭된 그룹 수, 매칭되지 않은 파일 수)를 INFO 레벨로 로깅해야 합니다

### Requirement 3

**User Story:** 사용자로서, 로그 메시지를 UI의 LogPanel에서 실시간으로 확인하고 싶습니다. 그래야 애플리케이션 동작을 모니터링할 수 있습니다.

#### Acceptance Criteria

1. WHEN FileGroupMatcherService가 로그 메시지를 생성할 때 THEN 시스템은 메시지를 LogPanel의 ObservableCollection에 추가해야 합니다
2. WHEN 로그 메시지가 추가될 때 THEN LogPanel은 자동으로 스크롤하여 최신 메시지를 표시해야 합니다(AutoScroll이 활성화된 경우)
3. WHEN 사용자가 로그 레벨 필터를 선택할 때 THEN LogPanel은 선택된 레벨(Debug, Info, Warning, Error)의 메시지만 표시해야 합니다
4. WHEN 로그 메시지가 1000개를 초과할 때 THEN 시스템은 가장 오래된 메시지를 자동으로 제거하여 메모리를 관리해야 합니다
5. WHEN 사용자가 로그를 검색할 때 THEN LogPanel은 메시지 내용과 소스 필드에서 검색어를 찾아 필터링해야 합니다

### Requirement 4

**User Story:** 개발자로서, 로깅 시스템이 성능에 영향을 주지 않으면서도 충분한 정보를 제공하기를 원합니다.

#### Acceptance Criteria

1. WHEN 로그 메시지가 생성될 때 THEN 시스템은 UI 스레드를 차단하지 않고 비동기적으로 처리해야 합니다
2. WHEN DEBUG 레벨 로깅이 비활성화되어 있을 때 THEN 시스템은 DEBUG 메시지 생성을 건너뛰어 성능을 최적화해야 합니다
3. WHEN 로그 메시지가 파일에 기록될 때 THEN 시스템은 I/O 오류가 발생해도 애플리케이션 동작을 중단하지 않아야 합니다
4. WHEN 매칭 프로세스가 실행될 때 THEN 로깅 오버헤드는 전체 실행 시간의 5% 미만이어야 합니다
5. WHEN 로그 메시지가 생성될 때 THEN 시스템은 문자열 보간을 사용하여 불필요한 문자열 생성을 최소화해야 합니다

### Requirement 5

**User Story:** 개발자로서, 로그 메시지의 형식이 일관되고 구조화되어 있기를 원합니다. 그래야 자동화된 분석이나 파싱이 가능합니다.

#### Acceptance Criteria

1. WHEN 로그 메시지가 생성될 때 THEN 시스템은 일관된 형식(타임스탬프, 레벨, 소스, 메시지)을 사용해야 합니다
2. WHEN 파일 매칭 로그가 생성될 때 THEN 메시지는 구조화된 데이터(키-값 쌍)를 포함해야 합니다
3. WHEN 시간 차이가 로깅될 때 THEN 시스템은 일관된 단위(초, 소수점 2자리)를 사용해야 합니다
4. WHEN 파일 경로가 로깅될 때 THEN 시스템은 상대 경로 또는 파일명만 표시하여 가독성을 높여야 합니다
5. WHEN 로그 메시지가 파일에 저장될 때 THEN 시스템은 기계 판독 가능한 형식(JSON 또는 구조화된 텍스트)을 사용해야 합니다

### Requirement 6

**User Story:** 개발자로서, 특정 그룹이나 파일에 대한 로그를 쉽게 추적하고 싶습니다.

#### Acceptance Criteria

1. WHEN 로그 메시지가 특정 그룹과 관련될 때 THEN 시스템은 그룹 ID를 메시지에 포함해야 합니다
2. WHEN 로그 메시지가 특정 파일과 관련될 때 THEN 시스템은 파일명 또는 키를 메시지에 포함해야 합니다
3. WHEN 사용자가 LogPanel에서 그룹 ID로 검색할 때 THEN 시스템은 해당 그룹과 관련된 모든 로그를 표시해야 합니다
4. WHEN 매칭 프로세스가 시작될 때 THEN 시스템은 고유한 세션 ID를 생성하여 관련 로그를 그룹화해야 합니다
5. WHEN 로그가 파일에 저장될 때 THEN 시스템은 세션 ID를 포함하여 여러 실행을 구분할 수 있어야 합니다

### Requirement 7

**User Story:** 개발자로서, 로깅 시스템을 쉽게 확장하고 커스터마이즈할 수 있기를 원합니다.

#### Acceptance Criteria

1. WHEN 새로운 로그 소스가 추가될 때 THEN 시스템은 중앙화된 로깅 인터페이스를 통해 메시지를 전송해야 합니다
2. WHEN 로그 메시지가 생성될 때 THEN 시스템은 Microsoft.Extensions.Logging 인터페이스를 사용해야 합니다
3. WHEN 로깅 설정이 변경될 때 THEN 시스템은 애플리케이션 재시작 없이 설정을 적용해야 합니다
4. WHEN 커스텀 로그 포맷터가 필요할 때 THEN 시스템은 플러그인 가능한 포맷터 인터페이스를 제공해야 합니다
5. WHEN 로그 출력 대상이 추가될 때(예: 원격 서버) THEN 시스템은 기존 코드 수정 없이 새로운 로그 프로바이더를 추가할 수 있어야 합니다
