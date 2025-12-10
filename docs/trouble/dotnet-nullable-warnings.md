## dotnet build 시 Nullable 경고 정리

- 빌드 대상: `ChronoView/ChronoView.csproj`
- 경고 유형: C# nullable reference/nullable value 경고 7건
- 상태: 원인 파악만, 코드 수정은 미반영

### 1) CS8603: 가능한 null 참조 반환

- 발생 파일:
  - `ChronoView/Core/ImageProcessing/ImageProcessingService.cs` (두 곳)
- 맥락:
  - `GenerateThumbnailAsync`에서 캐시 히트 시 `cachedThumbnail`을 바로 반환.
  - `GetImageMetadataAsync`에서 캐시 히트 시 `cachedMetadata`를 바로 반환.
- 원인:
  - `LruCache.TryGet`의 out 매개변수가 실패 시 `default`(null)일 가능성이 있다고 컴파일러가 판단.
  - 메서드 반환 타입이 non-nullable(`byte[]`, `ImageMetadata`)인데, 캐시 값 null 가능성이 해소되지 않아 `CS8603` 경고가 발생.
- 영향:
  - 실제로 캐시가 null을 저장하지 않는다면 런타임 문제는 없지만, 정적 분석 상 “null 반환 가능” 경고가 계속 노출됨.

### 2) CS8602: null 가능 참조에 대한 역참조

- 발생 파일:
  - `ChronoView/UI/Controls/LogPanel.xaml.cs` (두 곳)
- 맥락:
  - `ExportToFile` 메서드에서 `_filteredView`를 `foreach`로 바로 열거.
- 원인:
  - `_filteredView` 필드는 생성자/`InitializeLogView`에서 설정되지만 필드 자체가 nullable로 선언되어 있고, 메서드 내부에서 null 검증을 하지 않아 컴파일러가 “null일 수 있음”으로 추론.
- 영향:
  - 정상 흐름에서는 null이 아니지만, 정적 분석 경고가 발생하며 잠재적 NRE 가능성을 시사.

### 3) CS8629: Nullable 값 형식이 null일 수 있음

- 발생 파일:
  - `ChronoView/Core/FileMatching/FileGroupMatcherService.cs` (세 곳)
- 맥락:
  - `ExtractTimestampFromFolderName`/`ExtractTimestampFromNirKey`가 `DateTime?`을 반환하고, 이후 `Timestamp!.Value` 접근.
  - 앞서 `.Where(x => x.Timestamp.HasValue)`로 필터링했으나 컴파일러가 값 존재를 완전히 증명하지 못함.
- 원인:
  - `Timestamp`가 nullable(`DateTime?`)인데 `!`나 `.Value` 사용 시 여전히 “null일 수 있음”으로 간주.
- 영향:
  - 실 데이터에서 null이 들어오면 NRE 가능. 필터 로직이 정확하다면 경고는 거짓 양성에 가깝지만, 추가 방어 코드/명시적 null 체크로 제거 가능.

### 후속 조치 아이디어

- 캐시 반환부: TryGet 성공 여부를 확인한 후 반환값을 null 검증하거나, 캐시에 null을 넣지 않음을 보장하도록 방어 코드 추가.
- `_filteredView` 사용부: null 체크 후 조기 반환 또는 `ArgumentNullException` throw로 분석기 설득.
- `DateTime?` 처리부: `Timestamp is DateTime ts` 패턴 매칭으로 안전하게 언랩하거나, `Timestamp!.Value` 대신 조건 분기/early-continue 사용.


