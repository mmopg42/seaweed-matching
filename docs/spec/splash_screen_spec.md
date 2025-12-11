# 스플래시 스크린 (Splash Screen) 구현 계획

## 1. 개요
프로그램 실행 시 초기 로딩 시간 동안 사용자에게 브랜드 아이덴티티를 전달하고, 프로그램이 준비 중임을 알리는 스플래시 스크린을 도입합니다.

## 2. 디자인 사양
첨부된 이미지를 바탕으로 XAML을 구성합니다.

### 2.1 색상 및 스타일
- **배경색 (Background)**: Deep Red (예: `#D41C24` 또는 `#E31B23`) - *정확한 컬러 코드는 브랜드 가이드라인 또는 이미지 피킹 필요*
- **텍스트 색상**: White (`#FFFFFF`)

### 2.2 레이아웃 구성
중앙 정렬된 수직 스택 구조:

1.  **로고 박스 (Logo Box)**:
    -   테두리: 흰색 실선 (두께 약 3-5px)
    -   내부 텍스트: "PRISCHE" (대문자, 굵은 고딕 계열 폰트)
    -   배경: 투명 또는 배경색과 동일
    -   여백(Padding)을 넉넉히 주어 박스 형태 유지
2.  **메인 타이틀 (App Title)**:
    -   텍스트: "ChronoView Pro"
    -   폰트 크기: 로고보다 작지만 부제목보다 큼 (약 24-32pt)
    -   스타일: Regular 또는 Semi-Bold
    -   위치: 로고 박스 하단에 적절한 간격(Margin) 두고 배치
3.  **서브 타이틀 (Subtitle)**:
    -   텍스트: "Professional Monitoring Application"
    -   폰트 크기: 작음 (약 14-16pt)
    -   스타일: Light 또는 Regular
    -   위치: 메인 타이틀 하단 배치

## 3. 기술적 구현 방안 (WPF)

### 3.1 `SplashWindow.xaml` 생성
`Window` 클래스를 상속받는 별도의 XAML 파일을 생성하여 커스텀 UI를 구현합니다.

*   **Window 설정**:
    *   `WindowStyle="None"`: 타이틀 바 제거
    *   `AllowsTransparency="True"`: 투명 배경 지원 (필요 시)
    *   `Background="Transparent"`: 투명 처리 후 Border로 배경색 지정 권장
    *   `ResizeMode="NoResize"`: 크기 조절 방지
    *   `WindowStartupLocation="CenterScreen"`: 화면 중앙 표시
    *   `Topmost="True"`: 다른 창 위에 표시

### 3.2 로직 흐름 (`App.xaml.cs`)
`Application`의 `OnStartup` 이벤트를 오버라이드하여 제어합니다.

1.  **Splash Screen 표시**:
    ```csharp
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. 스플래시 윈도우 생성 및 표시
        var splash = new SplashWindow();
        splash.Show();

        // 2. 초기화 작업 (비동기 시뮬레이션 또는 실제 로딩)
        // 실제 로딩 로직이 짧다면 Task.Delay로 최소 노출 시간 보장
        await Task.Run(async () => 
        {
            // 예: 설정 로드, DB 연결, 리소스 준비 등
            await InitializeServicesAsync(); 
            await Task.Delay(2000); // UI 확인을 위한 최소 대기 시간 (2초)
        });

        // 3. 메인 윈도우 생성 및 표시
        var mainWindow = new MainWindow();
        mainWindow.Show();

        // 4. 스플래시 종료
        splash.Close();
    }
    ```

## 4. 작업 목록 (Task List)
- [ ] `SplashWindow.xaml` 디자인 구현 (Grid, Border, TextBlock 활용)
- [ ] `App.xaml`의 `StartupUri` 제거 (C# 코드에서 제어하기 위함)
- [ ] `App.xaml.cs`에 `OnStartup` 로직 구현
- [ ] 최소 로딩 시간 (`Task.Delay`) 적용하여 스플래시가 너무 빨리 사라지는 현상 방지

## 5. 예상 결과
프로그램 실행 시 붉은 배경의 PRISCHE 로고 화면이 약 2~3초간 중앙에 나타난 뒤, 메인 프로그램화면으로 부드럽게 전환됩니다.
