# Outlier Logic Analysis Requirements

## 1. Goal
Analyze and document the existing outlier detection and data filtering logic within the ChronoView application and its associated modules (NIR Camera 2).

## 2. Scope
- **Image Analysis**: `AbnormalDetectorService` and its application.
- **Spectral Analysis**: `NirSpectrumFilter` and its application in file processing.
- **Integration**: How these services are integrated into the main workflow (`MonitoringOrchestrator`, `Nir2CameraLauncher`).

## 3. Deliverables
- `02_research.md`: Detailed analysis of the logic, algorithms, and thresholds used.
