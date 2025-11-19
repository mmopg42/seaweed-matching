use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SpectrumData {
    pub x: Vec<f64>,
    pub y: Vec<f64>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct YVariationRegion {
    pub x_start: f64,
    pub x_end: f64,
    pub y_range: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct SpectrumAnalysisResult {
    pub file_path: String,
    pub has_valid_regions: bool,
    pub regions: Vec<YVariationRegion>,
    pub action: SpectrumAction,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum SpectrumAction {
    Move,    // 김 검출 - 이동
    Delete,  // 김 미검출 - 삭제
}
