# 파일 그룹화

## 개요
ChronoView는 여러 소스(카메라, NIR)에서 생성된 파일들을 타임스탬프 기반으로 자동으로 그룹화합니다.

## 그룹화 방식

### 데이터 시퀀스 기반 매칭
- **DataSequenceSettings**를 사용한 시간 기반 매칭
- 각 데이터 타입(NIR, Normal, Camera)별로 순서(Order)와 시간 지연(MinDelay, MaxDelay) 설정
- 설정된 순서대로 파일을 처리하며, 이전 데이터와의 시간 차이를 기준으로 그룹화
- 예: Normal → NIR (5~50초 이내) → Camera1 (0~10초 이내)

### 그룹 ID 생성
- 타임스탬프 순서대로 순차적 ID 생성
- Line 1과 Line 2는 각각 독립적인 ID 시퀀스 사용
- 형식: 숫자 ID (예: 1, 2, 3...)

## 그룹 구성 요소

### Normal 폴더
- 여러 이미지가 포함된 폴더
- 폴더 이름에서 타임스탬프 추출
- Line 1 또는 Line 2 식별

### 개별 카메라 파일
- Camera 1~6의 개별 이미지 파일
- 파일 이름에서 타임스탬프 추출

### NIR 데이터
- NIR 스펙트럼 파일 (.spc 또는 .txt)
- 타임스탬프로 매칭
- 그래프 자동 생성

## 그룹 상태

### 완료 (Complete)
- 모든 필수 파일이 매칭됨
- NIR 데이터 포함 (선택적)

### 대기 (Pending)
- 일부 파일만 도착
- 추가 파일 대기 중

### 오류 (Error)
- 매칭 실패
- 파일 손상 또는 누락

### 비정상 (Abnormal)
- 통계적으로 이상한 데이터 감지
- 이미지 크기 이상
- NIR만 있고 카메라 데이터 없음

## 그룹 보기

### 메인 화면
- Line 1 그룹 목록
- Line 2 그룹 목록
- 통합 보기 (선택 가능)

### 그룹 정보
각 그룹에 표시되는 정보:
- 그룹 ID
- 타임스탬프
- 포함된 파일 수
- NIR 데이터 유무
- 상태 (완료/대기/오류/비정상)
- 썸네일 이미지

### 상세 보기
- 그룹 더블클릭으로 상세 화면 열기
- 모든 파일 목록 확인
- 큰 이미지 미리보기
- NIR 그래프 확인

## 설정 옵션

- **DataSequenceSettings**: 데이터 타입별 순서 및 시간 지연 설정
  - 각 데이터 타입(NIR, Normal, Camera)의 Order, MinDelaySeconds, MaxDelaySeconds 설정
- **UseCameraSubfolderNormal**: Normal 폴더 내 카메라 하위 폴더 사용 여부
- **UseCameraSubfolderNormal2**: Normal2 폴더 내 카메라 하위 폴더 사용 여부
- **UseFolderSuffix**: Normal 폴더 이름 접미사 검증 사용 여부
