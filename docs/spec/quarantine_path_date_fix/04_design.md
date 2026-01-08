---
Task: 삭제 경로에서 파일명 날짜 제거 및 오늘 날짜 사용
Created: 2025-01-06
Status: Draft
Depends On: 03_plan.md
---

# 삭제 경로 날짜 수정 - 상세 설계

## 1. 개요

### 1.1 목적
`FileGroupOperator`의 `BuildPath` 메서드에서 QuarantineSchema 경로 생성 시 파일명에서 추출한 날짜(`group.Timestamp`) 대신 오늘 날짜(`DateTime.Now`)를 사용하도록 수정하고, 일반 카메라(NormalFolder)의 경우 원본 폴더명을 경로에 포함하도록 개선합니다.

### 1.2 배경
현재 삭제(Quarantine) 경로 생성 시 파일명에서 추출한 날짜가 경로에 포함되어 잘못된 경로 구조가 생성되고 있습니다. 또한 일반 카메라의 경우 원본 폴더명이 경로에 누락되어 있습니다.

### 1.3 범위
- `FileGroupOperator.BuildPath` 메서드 수정
- `FileGroupOperator.ExecuteOpAsync` 메서드 수정
- `FileGroupOperator.DeleteComponentsAsync` 메서드 수정

## 2. 문제 분석

### 2.1 현재 문제점

#### 문제 1: 파일명 날짜가 경로에 포함됨
- **잘못된 경로 예시**: 
  ```
  C:\workspace\seaweed\data\테스트 데이터\trash\20260106\20251203\UnknownSubject\Line1\일반1\images
  ```
- **원인**: `group.Timestamp.ToString("yyyyMMdd")` 사용
- **문제**: `20251203`은 파일명에서 추출한 날짜로, 매칭 목적으로만 사용되어야 하며 삭제 경로에 포함되면 안 됨

#### 문제 2: 일반 카메라 폴더명 누락
- **잘못된 경로 예시**: 
  ```
  C:\workspace\seaweed\data\테스트 데이터\trash\20260106\UnknownSubject\Line1\일반1\images
  ```
- **원인**: 원본 폴더명(`C251203T155910_0`)이 경로에 포함되지 않음
- **문제**: 일반 카메라의 경우 원본 폴더명이 경로에 포함되어야 함

### 2.2 올바른 경로 구조

#### 일반 카메라 (NormalFolder)
```
{quarantinePath}/{오늘날짜}/{시료명}/Line{라인번호}/{role}/{원본폴더명}
```
**예시**:
```
C:\workspace\seaweed\data\테스트 데이터\trash\20260106\UnknownSubject\Line1\일반1\C251203T155910_0
```
> 경로 예시는 `Path.Combine` 결과와 동일해야 합니다. 실제 테스트로 생성 경로를 검증하고 필요 시 예시를 조정합니다.

#### 기타 컴포넌트 (NIR, 카메라 파일)
```
{quarantinePath}/{오늘날짜}/{시료명}/Line{라인번호}/{role}
```
**예시**:
```
C:\workspace\seaweed\data\테스트 데이터\trash\20260106\UnknownSubject\Line1\nir1
```

### 2.3 영향 분석

#### 변경 영향 범위
- **영향 받는 메서드**:
  - `BuildPath`: 경로 생성 로직 변경
  - `ExecuteOpAsync`: NormalFolder 처리 시 폴더명 전달
  - `DeleteComponentsAsync`: NormalFolder 처리 시 폴더명 전달

#### 영향 받지 않는 부분
- `FileGroup.Timestamp` 속성: 매칭 목적으로 계속 사용됨
- `MoveSchema` 경로 생성: 이미 올바르게 구현되어 있음
- NIR 파일 및 카메라 파일 처리: 단일 파일이므로 폴더명 불필요

## 3. 상세 설계

### 3.1 BuildPath 메서드 수정

#### 3.1.1 메서드 시그니처 변경

**현재**:
```csharp
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null)
```

**변경 후**:
```csharp
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null, string? folderName = null)
```

#### 3.1.2 QuarantineSchema 경로 생성 로직

**현재 코드** (Line 84-95):
```84:95:ChronoView/Core/FileOperations/FileGroupOperator.cs
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null)
{
    var subj = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
    if (schema == PathSchema.QuarantineSchema) {
        return Path.Combine(basePath, group.Timestamp.ToString("yyyyMMdd"), subj, $"Line{group.LineNumber}", role);
    } else {
        var nir = group.HasNir ? "with NIR" : "without NIR";
        if (role.StartsWith("cam")) return Path.Combine(basePath, subj, nir, "복합 카메라", role);
        if (role == "일반" || role == "일반2") return Path.Combine(basePath, subj, nir, group.HasNir ? role : $"{role} 카메라");
        return Path.Combine(basePath, subj, nir, role);
    }
}
```

**변경 후 코드** (주석은 블록 상단에서 설명):

- 오늘 날짜 사용 (`DateTime.Now.ToString("yyyyMMdd")`)
- 일반 카메라의 경우 `folderName`을 끝 경로에 추가

```csharp
private string BuildPath(string basePath, FileGroup group, string role, PathSchema schema, string? subject = null, string? folderName = null)
{
    var subj = string.IsNullOrWhiteSpace(subject) ? "UnknownSubject" : subject;
    if (schema == PathSchema.QuarantineSchema) {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var baseQuarantinePath = Path.Combine(basePath, today, subj, $"Line{group.LineNumber}", role);
        if (!string.IsNullOrEmpty(folderName))
        {
            return Path.Combine(baseQuarantinePath, folderName);
        }
        
        return baseQuarantinePath;
    } else {
        var nir = group.HasNir ? "with NIR" : "without NIR";
        if (role.StartsWith("cam")) return Path.Combine(basePath, subj, nir, "복합 카메라", role);
        if (role == "일반" || role == "일반2") return Path.Combine(basePath, subj, nir, group.HasNir ? role : $"{role} 카메라");
        return Path.Combine(basePath, subj, nir, role);
    }
}
```

#### 3.1.3 설계 결정사항

1. **날짜 사용**: `DateTime.Now`를 사용하여 실행 시점의 날짜를 경로에 포함
2. **폴더명 파라미터**: `folderName`을 선택적 파라미터로 추가하여 일반 카메라에만 적용
3. **하위 호환성**: `MoveSchema` 경로 생성 로직은 변경하지 않음

### 3.2 ExecuteOpAsync 메서드 수정

#### 3.2.1 NormalFolder 처리 로직

**현재 코드** (Line 27-31):
```27:31:ChronoView/Core/FileOperations/FileGroupOperator.cs
if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = schema == PathSchema.MoveSchema ? (group.LineNumber == 1 ? "일반" : "일반2") : (group.LineNumber == 1 ? "일반1" : "일반2");
    var destPath = BuildPath(targetBase, group, role, schema);
    _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
    await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
}
```

**변경 후 코드** (주석은 블록 상단에서 설명):

- QuarantineSchema일 때만 `folderName` 추출 후 전달

```csharp
if (!string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = schema == PathSchema.MoveSchema ? (group.LineNumber == 1 ? "일반" : "일반2") : (group.LineNumber == 1 ? "일반1" : "일반2");
    string? folderName = null;
    if (schema == PathSchema.QuarantineSchema)
    {
        folderName = Path.GetFileName(group.NormalFolder);
    }
    
    var destPath = BuildPath(targetBase, group, role, schema, null, folderName);
    _logger.LogInformation("Moving NormalFolder: {Src} -> {Dest}", group.NormalFolder, destPath);
    await MoveDirectoryAtomicAsync(group.NormalFolder, destPath, movedItems, ct);
}
```

#### 3.2.2 설계 결정사항

1. **조건부 폴더명 추출**: `QuarantineSchema`인 경우에만 폴더명을 추출하여 `MoveSchema`에는 영향 없음
2. **폴더명 추출 방법**: `Path.GetFileName()`을 사용하여 경로에서 폴더명만 추출
3. **null 처리**: `folderName`이 null인 경우 `BuildPath`에서 무시됨
4. **예외 처리**: `Path.GetFileName()`이 빈 문자열을 반환하는 경우 `folderName`을 null로 간주하여 `BuildPath`에서 무시됨

### 3.3 DeleteComponentsAsync 메서드 수정

#### 3.3.1 Normal 컴포넌트 처리 로직

**현재 코드** (Line 61-63):
```61:63:ChronoView/Core/FileOperations/FileGroupOperator.cs
if (comp == "Normal" && !string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = group.LineNumber == 1 ? "일반1" : "일반2";
    await MoveDirectoryAtomicAsync(group.NormalFolder, BuildPath(quarantinePath, group, role, PathSchema.QuarantineSchema, subject), movedItems, ct);
}
```

**변경 후 코드**:
```csharp
if (comp == "Normal" && !string.IsNullOrEmpty(group.NormalFolder) && Directory.Exists(group.NormalFolder)) {
    var role = group.LineNumber == 1 ? "일반1" : "일반2";
    var folderName = Path.GetFileName(group.NormalFolder);
    await MoveDirectoryAtomicAsync(group.NormalFolder, BuildPath(quarantinePath, group, role, PathSchema.QuarantineSchema, subject, folderName), movedItems, ct);
}
```

#### 3.3.2 설계 결정사항

1. **일관성**: `ExecuteOpAsync`와 동일한 방식으로 폴더명 추출 및 전달
2. **단순화**: `DeleteComponentsAsync`는 항상 `QuarantineSchema`를 사용하므로 조건문 불필요

## 4. 구현 세부사항

### 4.1 변경 파일 목록

1. **ChronoView/Core/FileOperations/FileGroupOperator.cs**
   - `BuildPath` 메서드 수정 (Line 84-95)
   - `ExecuteOpAsync` 메서드 수정 (Line 27-31)
   - `DeleteComponentsAsync` 메서드 수정 (Line 61-63)

### 4.2 변경 사항 요약

| 메서드 | 변경 내용 | 영향 범위 |
|--------|----------|----------|
| `BuildPath` | `folderName` 파라미터 추가, `DateTime.Now` 사용 | QuarantineSchema 경로 생성 |
| `ExecuteOpAsync` | NormalFolder 처리 시 폴더명 추출 및 전달 | QuarantineSchema 사용 시 |
| `DeleteComponentsAsync` | Normal 컴포넌트 처리 시 폴더명 추출 및 전달 | Normal 컴포넌트 삭제 시 |

### 4.3 코드 변경 상세

#### 변경 1: BuildPath 메서드
- **위치**: Line 84-95
- **변경 타입**: 메서드 시그니처 및 구현 수정
- **위험도**: 중간 (기존 호출부에 영향 없음 - 선택적 파라미터)

#### 변경 2: ExecuteOpAsync 메서드
- **위치**: Line 27-31
- **변경 타입**: 로직 추가
- **위험도**: 낮음 (조건부 로직 추가)

#### 변경 3: DeleteComponentsAsync 메서드
- **위치**: Line 61-63
- **변경 타입**: 로직 수정
- **위험도**: 낮음 (폴더명 추가만)

## 5. 테스트 계획

### 5.1 단위 테스트

#### 테스트 케이스 1: BuildPath - QuarantineSchema 일반 카메라
- **입력**: `schema = QuarantineSchema`, `role = "일반1"`, `folderName = "C251203T155910_0"`
- **예상 결과**: `{basePath}/{오늘날짜}/UnknownSubject/Line1/일반1/C251203T155910_0`
- **검증**: 경로에 오늘 날짜가 포함되고, 파일명 날짜가 포함되지 않으며, 폴더명이 포함됨

#### 테스트 케이스 2: BuildPath - QuarantineSchema NIR
- **입력**: `schema = QuarantineSchema`, `role = "nir1"`, `folderName = null`
- **예상 결과**: `{basePath}/{오늘날짜}/UnknownSubject/Line1/nir1`
- **검증**: 경로에 오늘 날짜가 포함되고, 폴더명이 포함되지 않음

#### 테스트 케이스 3: BuildPath - MoveSchema (변경 없음)
- **입력**: `schema = MoveSchema`, `role = "일반"`
- **예상 결과**: 기존과 동일한 경로 구조
- **검증**: 기존 동작 유지

### 5.2 통합 테스트

#### 테스트 케이스 1: ExecuteOpAsync - QuarantineSchema
- **시나리오**: 일반 카메라 폴더를 삭제 경로로 이동
- **검증**: 
  - 올바른 경로에 폴더가 이동됨
  - 경로에 오늘 날짜가 포함됨
  - 경로에 원본 폴더명이 포함됨
  - 파일명 날짜가 경로에 포함되지 않음

#### 테스트 케이스 2: DeleteComponentsAsync - Normal 컴포넌트
- **시나리오**: Normal 컴포넌트 삭제
- **검증**:
  - 올바른 경로에 폴더가 이동됨
  - 경로에 오늘 날짜가 포함됨
  - 경로에 원본 폴더명이 포함됨

### 5.3 수동 테스트 시나리오

1. **일반 카메라 삭제 테스트**
   - 일반 카메라 폴더(`C251203T155910_0`)를 삭제
   - 생성된 경로 확인: `{quarantinePath}/{오늘날짜}/{시료명}/Line{라인번호}/일반1/C251203T155910_0`

2. **NIR 파일 삭제 테스트**
   - NIR 파일을 삭제
   - 생성된 경로 확인: `{quarantinePath}/{오늘날짜}/{시료명}/Line{라인번호}/nir1`

3. **복합 카메라 파일 삭제 테스트**
   - 복합 카메라 파일을 삭제
   - 생성된 경로 확인: `{quarantinePath}/{오늘날짜}/{시료명}/Line{라인번호}/{camKey}`

## 6. 리스크 및 대응 방안

### 6.1 리스크 분석

| 리스크 | 가능성 | 영향도 | 대응 방안 |
|--------|--------|--------|----------|
| 기존 삭제된 파일 경로와 불일치 | 중간 | 낮음 | 문서화 및 마이그레이션 가이드 제공 |
| 날짜 변경 시 경로 불일치 | 낮음 | 낮음 | `DateTime.Now` 사용으로 자동 해결 |
| 폴더명 추출 실패 | 낮음 | 중간 | null 체크 및 예외 처리 |

### 6.2 하위 호환성

- **MoveSchema**: 변경 없음, 기존 동작 유지
- **기존 삭제 경로**: 새로운 경로 구조로 변경되지만, 기존 파일에는 영향 없음
- **API 호환성**: `BuildPath`에 선택적 파라미터 추가로 기존 호출부 호환

## 7. 참고사항

### 7.1 FileGroup.Timestamp 속성
- `FileGroup.Timestamp`는 파일명에서 추출한 날짜로, **매칭 목적으로만 사용**되어야 함
- 삭제 및 이동 경로에는 사용하지 않음

### 7.2 날짜 사용 원칙
- **삭제 경로**: 오늘 날짜(`DateTime.Now`) 사용
- **이동 경로**: 기존 로직 유지 (MoveSchema)
- **매칭**: 파일명에서 추출한 날짜(`group.Timestamp`) 사용

### 7.3 폴더명 포함 원칙
- **일반 카메라 (NormalFolder)**: 원본 폴더명 포함 필요
- **NIR 파일**: 단일 파일이므로 폴더명 불필요
- **카메라 파일**: 단일 파일이므로 폴더명 불필요

## 8. 구현 체크리스트

- [ ] `BuildPath` 메서드에 `folderName` 파라미터 추가
- [ ] `BuildPath` 메서드에서 `DateTime.Now` 사용하도록 수정
- [ ] `BuildPath` 메서드에서 일반 카메라 폴더명 포함 로직 추가
- [ ] `ExecuteOpAsync` 메서드에서 NormalFolder 처리 시 폴더명 추출 및 전달
- [ ] `DeleteComponentsAsync` 메서드에서 Normal 컴포넌트 처리 시 폴더명 추출 및 전달
- [ ] 단위 테스트 작성 및 실행
- [ ] 통합 테스트 실행
- [ ] 수동 테스트 수행
- [ ] 코드 리뷰
- [ ] 경로 충돌 시나리오 테스트 (동일 경로 대상 중복 이동/삭제)
- [ ] `DateTime.Now` 모킹 기반 단위 테스트 (날짜 고정)
- [ ] 빈 폴더명/예외 케이스 테스트 (`Path.GetFileName()` 빈 문자열)

