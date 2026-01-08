---
Task: ChronoView C# Module Documentation
Created: 2025-01-05
Status: Draft
Depends On: requirements.md
---

# ChronoView C# Module Documentation - Research Findings

## 1. Investigation Summary

Conducted comprehensive analysis of the ChronoView C# project structure, existing documentation patterns, and codebase organization to determine the optimal approach for creating structured module documentation.

## 2. Question Answers

### Q1: What is the complete list of all C# files in ChronoView that need documentation?
**Method**: PowerShell command to enumerate all .cs files excluding build artifacts
**Findings**: Identified 95 C# source files across 11 major subsystems
**Conclusion**: Complete file list obtained (see Section 3)
**Evidence**: PowerShell output showing full file paths

**File Count by Subsystem**:
- Core/ (48 files across 7 submodules)
- UI/ (28 files across 4 submodules)
- Models/ (7 files)
- Helpers/ (3 files)
- Converters/ (2 files)
- Infrastructure/ (1 file)
- Tests/ (1 file)
- Root level/ (5 files)

### Q2: What are the main subsystems/modules in ChronoView based on folder structure?
**Method**: Directory structure analysis
**Findings**: ChronoView follows a clean architecture pattern with clear separation of concerns
**Conclusion**: 11 major subsystems identified with hierarchical organization
**Evidence**: Directory listing showing folder structure

**Major Subsystems**:
1. **Core/** - Business logic and services
   - Analytics/ - Statistical analysis and anomaly detection
   - Configuration/ - Configuration management
   - FileMatching/ - File grouping and matching logic
   - FileOperations/ - File system operations
   - FileWatching/ - Real-time file monitoring
   - ImageProcessing/ - Image loading and caching
   - Localization/ - Multi-language support
   - NIR/ - NIR spectrum processing
   - ProgramLaunching/ - External program integration

2. **UI/** - User interface components
   - Behaviors/ - UI behaviors (drag selection)
   - Controls/ - Custom controls (DataGrid, Panels)
   - ViewModels/ - MVVM view models
   - Views/ - XAML views and code-behind

3. **Models/** - Data models and DTOs
4. **Helpers/** - Utility classes
5. **Converters/** - XAML value converters
6. **Infrastructure/** - Cross-cutting concerns (logging)
7. **Tests/** - Validation tests

### Q3: Are there existing documentation patterns or standards in the docs/ folder to follow?
**Method**: Examined docs/ folder structure and existing documentation
**Findings**: 
- docs/gui_c/ contains existing C# migration checklists
- docs/modules/ contains Python module documentation
- docs/architecture/ exists but is empty
- Existing pattern: function-level checklists with complexity ratings
**Conclusion**: Should follow architecture documentation template from guidelines, but can reference existing gui_c/ patterns for function-level detail
**Evidence**: docs/gui_c/module_function_checklist_complete.md shows detailed function documentation

**Existing Documentation Structure**:
```
docs/
├── gui_c/                    # C# migration documentation
│   ├── implementation_progress_checklist.md
│   ├── migration_analysis_checklist.md
│   └── module_function_checklist_complete.md
├── modules/                  # Python module documentation
├── architecture/             # Empty - target for new docs
└── README.md                 # Overview document
```

### Q4: What level of detail is needed for each function (signature, parameters, return types, exceptions)?
**Method**: Analyzed existing documentation and guidelines requirements
**Findings**: 
- Guidelines require: Inputs, Outputs, Errors/Exceptions, Side Effects
- Existing gui_c/ docs show: Function signature, C# equivalent, complexity rating
- Architecture docs need: Contracts, Dependencies, Impact zones
**Conclusion**: Document public APIs with full signatures, parameters, return types, and exceptions. Private methods can have brief descriptions.
**Evidence**: Architecture document template in guidelines.md

**Required Documentation Elements**:
- Function/Method name and signature
- Purpose/description (1-2 sentences)
- Parameters (name, type, description)
- Return type and description
- Exceptions thrown
- Side effects (file I/O, state changes)
- Dependencies (what it calls)
- Dependents (what calls it)

### Q5: Should we document private methods or only public APIs?
**Method**: Reviewed guidelines and existing patterns
**Findings**: 
- Guidelines focus on "Contracts" and "Dependents" - primarily public APIs
- Existing gui_c/ docs include all functions regardless of visibility
- Architecture docs emphasize impact analysis
**Conclusion**: Document all public APIs in detail. Include private methods with brief descriptions if they contain significant logic or are called by multiple public methods.
**Evidence**: Guidelines Part 6.1 - Architecture Document Template

**Documentation Scope**:
- ✅ Public classes, methods, properties, events (FULL detail)
- ✅ Internal classes/methods used across assemblies (FULL detail)
- ✅ Private methods with complex logic (BRIEF description)
- ❌ Auto-generated code (Designer.cs files)
- ❌ Simple getters/setters without logic

### Q6: Are there any critical dependencies between modules that should be highlighted?
**Method**: Code structure analysis and existing documentation review
**Findings**: 
- Core services follow dependency injection pattern
- UI ViewModels depend on Core services
- FileWatching orchestrates FileMatching and GroupManager
- Configuration is used across all modules
**Conclusion**: Document service dependencies, interface implementations, and cross-module calls
**Evidence**: Folder structure shows clear layering (Core → UI)

**Key Dependency Patterns**:
1. **Service Layer**: Core services implement interfaces (I* pattern)
2. **MVVM Pattern**: ViewModels depend on Core services
3. **Orchestration**: MonitoringOrchestrator coordinates multiple services
4. **Configuration**: ConfigurationManager used by all modules
5. **Logging**: UILoggerProvider used across application

### Q7: What naming conventions are used in the codebase for classes and methods?
**Method**: File name analysis and C# convention review
**Findings**: 
- Classes: PascalCase (e.g., FileGroupMatcherService)
- Interfaces: IPascalCase (e.g., IFileGroupMatcher)
- Methods: PascalCase (C# standard)
- Services: *Service suffix pattern
- ViewModels: *ViewModel suffix pattern
**Conclusion**: Standard C# naming conventions are followed consistently
**Evidence**: File listing shows consistent naming patterns

**Naming Conventions**:
- **Classes**: `PascalCase` (e.g., `AbnormalDetectorService`)
- **Interfaces**: `IPascalCase` (e.g., `IConfigurationManager`)
- **Methods**: `PascalCase` (e.g., `LoadConfiguration`)
- **Properties**: `PascalCase` (e.g., `IsEnabled`)
- **Private fields**: `_camelCase` (assumed, standard C# pattern)
- **Services**: `*Service` suffix
- **ViewModels**: `*ViewModel` suffix
- **Converters**: `*Converter` suffix

## 3. Code Analysis Results

### Relevant Existing Code

| File | Function/Class | Relevance |
|------|----------------|-----------|
| `Core/Configuration/ConfigurationManager.cs` | `ConfigurationManager` | Central configuration service used by all modules |
| `Core/FileWatching/MonitoringOrchestrator.cs` | `MonitoringOrchestrator` | Main workflow coordinator |
| `Core/FileMatching/FileMatchingEngine.cs` | `FileMatchingEngine` | Core business logic for file grouping |
| `UI/ViewModels/MainWindowViewModel.cs` | `MainWindowViewModel` | Main application entry point |
| `Models/FileGroup.cs` | `FileGroup` | Core data model |

### Dependencies Identified

<!-- VERIFY: grep -rn "using ChronoView" ChronoView/**/*.cs -->

**Internal Dependencies**:
- UI layer depends on Core services
- Core services depend on Models
- All modules depend on Configuration
- ViewModels depend on multiple Core services

**External Dependencies** (from .csproj):
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- System.Text.Json
- WPF framework assemblies

### Glossary Check

**Existing related terms in glossary**: None (glossary.md doesn't exist yet)

**New terms to add**:
- `FileGroup` - Core data structure representing matched files
- `UnmatchedFiles` - Collection of files awaiting matching
- `DataSequenceSettings` - Configuration for file sequence patterns
- `ImageMetadata` - Image file metadata structure
- `NirSpectrum` - NIR spectroscopy data structure
- `MonitoringOrchestrator` - Service coordinating file monitoring workflow
- `FileMatchingEngine` - Service for matching and grouping files
- `ConfigurationManager` - Service for application configuration

## 4. Recommendations

### Documentation Structure
1. Create `docs/c_module/` as the root documentation folder
2. Mirror the ChronoView source folder structure
3. Create one .md file per C# source file
4. Create index files for each subsystem folder
5. Create a master index at `docs/c_module/README.md`

### Documentation Format
1. Follow architecture document template from guidelines
2. Include function-level detail similar to existing gui_c/ docs
3. Add cross-references between related modules
4. Include code examples for complex APIs
5. Document interfaces separately from implementations

### Prioritization
1. **Phase 1**: Core services (Configuration, FileMatching, FileWatching)
2. **Phase 2**: Models and data structures
3. **Phase 3**: UI ViewModels and main application logic
4. **Phase 4**: Helpers, Converters, and utility classes
5. **Phase 5**: UI Views (code-behind only)

### Tooling
1. Use automated tools to extract method signatures
2. Generate initial documentation templates
3. Manual review and enhancement for each module
4. Cross-reference validation

## 5. Unanswered Questions

- **Q**: Should we include XAML structure documentation or only code-behind?
  - **Status**: Decided - Code-behind only per requirements
  
- **Q**: How to handle auto-generated Designer.cs files?
  - **Status**: Decided - Exclude per requirements

- **Q**: Should we document test files?
  - **Status**: Decided - Include Tests/ folder but lower priority

---
**Status**: [ ] Approved
