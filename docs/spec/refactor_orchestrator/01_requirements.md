# Refactor MonitoringOrchestrator - Requirements

## Overview
Reduce the complexity and size of `MonitoringOrchestrator.cs` by extracting responsibilities into separate, testable components. The target is to reduce the file size below 600 lines (currently 2,138 lines).

## Success Criteria
1. [x] Remove dead/legacy code (Phase 1).
2. [x] Extract `InitialScanner` (Phase 2).
3. [ ] Extract `GroupManager` (Phase 3).
    - Move file group lifecycle management (create, match, merge, remove) to `GroupManager`.
4. [ ] Extract `EventProcessor` (Phase 4).
    - Move real-time event handling and parallel worker logic.
5. [ ] Pass all build checks.
6. [ ] Maintain existing matching logic correctness (Match 1, 2, 3).

## Constraints
- Do not break existing functionality.
- Use dependency injection for new components.
- Follow the established C# coding standards in the project.
