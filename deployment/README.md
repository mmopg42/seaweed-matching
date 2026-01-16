# 배포 스크립트 사용 가이드

## 빠른 시작

### 단일 실행 파일 배포 (권장)

```powershell
# 64비트 단일 실행 파일
.\deployment\publish.ps1 -Mode single-file -Runtime win-x64

# 32비트 단일 실행 파일
.\deployment\publish.ps1 -Mode single-file -Runtime win-x86
```

### 모든 방식으로 배포

```powershell
.\deployment\publish.ps1 -Mode all -Runtime win-x64
```

## 배포 모드

- **single-file**: 단일 실행 파일 (Self-Contained)
- **folder**: 폴더 배포 (Self-Contained)
- **framework-dependent**: Framework-Dependent 배포
- **all**: 모든 방식으로 배포

## 런타임

- **win-x64**: 64비트 Windows
- **win-x86**: 32비트 Windows

## 출력 위치

배포된 파일은 `publish/` 폴더에 생성됩니다:

```
publish/
├── single-file-win-x64/
│   └── ChronoView.exe
├── folder-win-x64/
│   └── (여러 파일)
└── framework-dependent-win-x64/
    └── (애플리케이션 파일만)
```

## 상세 가이드

자세한 내용은 [배포 가이드](../docs/deployment/배포_가이드.md)를 참조하세요.



