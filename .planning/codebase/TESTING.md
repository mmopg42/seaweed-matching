# Testing Patterns

**Analysis Date:** 2026-01-16

## Test Framework

**Runner:**
- xUnit 2.9.3 - Primary test framework
- .NET test runner via Visual Studio or `dotnet test`

**Assertion Library:**
- xUnit built-in assertions (Assert class)
- Matchers: True, False, Equal, Throws, Contains, etc.

**Run Commands:**
```bash
dotnet test                              # Run all tests
dotnet test --filter "FullyQualifiedName~FileMatching"  # Filter tests
dotnet test --collect:"XPlat Code Coverage"             # Coverage report
dotnet test ChronoView.Tests/ChronoView.Tests.csproj   # Specific project
```

## Test File Organization

**Location:**
- Separate test project: `ChronoView.Tests/`
- Mirrors source structure (Core/, UI/, Models/)

**Naming:**
- Unit tests: `{ClassName}Tests.cs` (e.g., `FileMatchingEngineTests.cs`)
- Property tests: `{Feature}PropertyTests.cs` (e.g., `ConfigurationPersistencePropertyTests.cs`)
- Integration tests: `{Feature}IntegrationTests.cs`

**Structure:**
```
ChronoView.Tests/
├── Core/
│   ├── Analytics/         # AbnormalDetectorServiceTests, StatisticsServiceTests
│   ├── Configuration/     # DefaultConfigurationTests, configuration property tests
│   ├── FileMatching/      # FileMatchingEngineTests, TimestampParsingTests
│   ├── FileOperations/    # FileOperationServiceTests, path tests
│   ├── FileWatching/      # FileWatcherServiceSmokeTests, orchestrator construction
│   └── ImageProcessing/   # ImageProcessingPropertyTests
├── Models/                 # DataModelTests
├── UI/
│   └── ViewModels/        # ViewModelTests, StartCommandTests, StopCommandTests
└── Integration/           # CoreServicesIntegrationTests
```

## Test Structure

**Suite Organization:**
```csharp
public class FileMatchingEngineTests : IDisposable
{
    private readonly Mock<IGroupIdGenerator> _mockIdGenerator;
    private readonly DataSequenceSettings _settings;

    public FileMatchingEngineTests()
    {
        // Arrange shared test state
        _mockIdGenerator = new Mock<IGroupIdGenerator>();
        _settings = new DataSequenceSettings { ... };
    }

    [Fact]
    public void MatchFiles_ValidInput_ReturnsExpectedGroups()
    {
        // Arrange
        var unmatched = new UnmatchedFiles();
        // ... setup test data

        // Act
        var result = _engine.MatchFiles(unmatched, _settings);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    public void Dispose()
    {
        // Cleanup test resources
    }
}
```

**Patterns:**
- Constructor for shared test setup
- IDisposable for cleanup (temp files, etc.)
- Arrange/Act/Test comments for clarity
- Mock<T> from Moq for dependencies

## Mocking

**Framework:**
- Moq 4.20.72 - Mocking library

**Patterns:**
```csharp
// Mock interface
var mockConfig = new Mock<IConfigurationManager>();
mockConfig.Setup(c => c.LoadConfiguration<ApplicationConfiguration>())
          .Returns(config);

// Verify calls
mockLogger.Verify(l => l.LogInformation(
    "Processing {FileCount} files", It.IsAny<int>()), Times.Once);
```

**What to Mock:**
- IConfigurationManager for settings
- IGroupIdGenerator for ID generation
- ILogger<T> for logging
- File system operations (where abstracted)

**What NOT to Mock:**
- Pure functions and utilities
- Simple value objects (DataSequenceSettings, etc.)

## Fixtures and Factories

**Test Data:**
```csharp
// Inline factory pattern in test constructors
private Mock<IGroupIdGenerator> _mockIdGenerator;
private DataSequenceSettings _settings;

public FileMatchingEngineTests()
{
    _mockIdGenerator = new Mock<IGroupIdGenerator>();
    _mockIdGenerator.Setup(x => x.GenerateNextId(It.IsAny<int>()))
                    .Returns((int line) => $"G_{line}_{Guid.NewGuid()}");

    _settings = new DataSequenceSettings { ... };
}
```

**Location:**
- Factory methods inline in test classes
- Test-specific helpers in test files
- No shared fixtures directory

## Coverage

**Requirements:**
- No enforced coverage target
- Coverage tracked via `dotnet test --collect:"XPlat Code Coverage"`
- Focus on critical paths (file matching, abnormal detection)

**Configuration:**
- coverlet.collector 6.0.4 for coverage
- No exclusions configured

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
# Results in TestResults/{guid}/coverage.cobertura.xml
```

## Test Types

**Unit Tests:**
- Test single class in isolation
- Mock all external dependencies
- Fast execution (milliseconds per test)
- Examples: FileMatchingEngineTests, StatisticsServiceTests

**Property-Based Tests:**
- FsCheck.Xunit 3.3.2 for property-based testing
- Verify invariants across generated inputs
- Examples: ConfigurationPersistencePropertyTests, FileGroupingConsistencyPropertyTests

**Integration Tests:**
- Test multiple services together
- Real dependencies where appropriate
- Examples: CoreServicesIntegrationTests

**UI Tests:**
- ViewModels tested in isolation
- Commands tested via Execute methods
- Examples: StartCommandTests, StopCommandTests

**Smoke Tests:**
- Light verification of service construction
- Examples: FileWatcherServiceSmokeTests, MonitoringOrchestratorConstructionTests

## Common Patterns

**Async Testing:**
```csharp
[Fact]
public async Task StartAsync_ValidConfig_StartsMonitoring()
{
    // Arrange
    var service = new FileWatcherService(...);

    // Act
    await service.StartAsync(default);

    // Assert
    Assert.True(service.IsMonitoring);
}
```

**Error Testing:**
```csharp
[Fact]
public void MatchFiles_NullInput_ThrowsArgumentNullException()
{
    Assert.Throws<ArgumentNullException>(() => _engine.MatchFiles(null!, _settings));
}

// Async error
[Fact]
public async Task StartAsync_InvalidConfig_ThrowsInvalidOperationException()
{
    await Assert.ThrowsAsync<InvalidOperationException>(() => _service.StartAsync(invalidConfig));
}
```

**Disposable Pattern:**
```csharp
public class FileOperationTests : IDisposable
{
    private readonly string _tempDir;

    public FileOperationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { /* Ignore cleanup failures */ }
        }
    }
}
```

---

*Testing analysis: 2026-01-16*
*Update when test patterns change*
