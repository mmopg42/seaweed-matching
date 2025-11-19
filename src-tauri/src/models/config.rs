use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AppConfig {
    // 폴더 경로 설정
    pub normal_folder: Option<String>,
    pub normal2_folder: Option<String>,
    pub nir_folder: Option<String>,
    pub nir2_folder: Option<String>,
    pub cam1_folder: Option<String>,
    pub cam2_folder: Option<String>,
    pub cam3_folder: Option<String>,
    pub cam4_folder: Option<String>,
    pub cam5_folder: Option<String>,
    pub cam6_folder: Option<String>,

    // 출력 경로
    pub output_folder: Option<String>,

    // 윈도우 설정
    pub window_x: Option<i32>,
    pub window_y: Option<i32>,
    pub window_width: Option<u32>,
    pub window_height: Option<u32>,
}

impl Default for AppConfig {
    fn default() -> Self {
        Self {
            normal_folder: None,
            normal2_folder: None,
            nir_folder: None,
            nir2_folder: None,
            cam1_folder: None,
            cam2_folder: None,
            cam3_folder: None,
            cam4_folder: None,
            cam5_folder: None,
            cam6_folder: None,
            output_folder: None,
            window_x: None,
            window_y: None,
            window_width: Some(1280),
            window_height: Some(800),
        }
    }
}
