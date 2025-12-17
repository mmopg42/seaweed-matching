# Initial Scan Matching Debug - Tasks

## Phase 1: Configuration Logging
- [ ] Add DataSequenceSettings status log in `PerformInitialScanAsync`
- [ ] Add sequence order log showing all items
- [ ] Add MonitoringOrchestrator constructor config status log
- [ ] Build and test

## Phase 2: Timestamp Extraction Logging
- [ ] Add timestamp extraction success/fail logs in `CreateUnmatchedFilesForSingleFile`
- [ ] Add detailed timestamp logs for Normal folders
- [ ] Add detailed timestamp logs for Camera files
- [ ] Add detailed timestamp logs for NIR files
- [ ] Build and test

## Phase 3: Match 3 Logic Logging
- [ ] Add Match 3 start log with new file timestamp
- [ ] Add per-candidate comparison logs (time diff, range, temporal order)
- [ ] Add match result log (matched group or new group)
- [ ] Add helper method `GetPrimaryDataType`
- [ ] Build and test

## Phase 4: Sequential Scan Progress Logging
- [ ] Add per-data-type file count log
- [ ] Add per-file processing log (optional, debug level)
- [ ] Build and test

## Phase 5: Testing & Analysis
- [ ] Run application with test data
- [ ] Collect full log output
- [ ] Analyze logs to identify root cause
- [ ] Document findings

---
**Estimated Time**: 1-2 hours
**Status**: [ ] Ready to start
