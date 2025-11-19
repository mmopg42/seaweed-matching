use std::path::Path;
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FolderStats {
    pub nir_count: usize,
    pub nir2_count: usize,
    pub normal_count: usize,
    pub normal2_count: usize,
    pub cam1_count: usize,
    pub cam2_count: usize,
    pub cam3_count: usize,
    pub cam4_count: usize,
    pub cam5_count: usize,
    pub cam6_count: usize,
}

#[tauri::command]
pub async fn get_folder_stats(
    nir_folder: Option<String>,
    nir2_folder: Option<String>,
    normal_folder: Option<String>,
    normal2_folder: Option<String>,
    cam1_folder: Option<String>,
    cam2_folder: Option<String>,
    cam3_folder: Option<String>,
    cam4_folder: Option<String>,
    cam5_folder: Option<String>,
    cam6_folder: Option<String>,
) -> Result<FolderStats, String> {
    Ok(FolderStats {
        nir_count: count_files(&nir_folder)?,
        nir2_count: count_files(&nir2_folder)?,
        normal_count: count_files(&normal_folder)?,
        normal2_count: count_files(&normal2_folder)?,
        cam1_count: count_files(&cam1_folder)?,
        cam2_count: count_files(&cam2_folder)?,
        cam3_count: count_files(&cam3_folder)?,
        cam4_count: count_files(&cam4_folder)?,
        cam5_count: count_files(&cam5_folder)?,
        cam6_count: count_files(&cam6_folder)?,
    })
}

fn count_files(folder: &Option<String>) -> Result<usize, String> {
    if let Some(path) = folder {
        let p = Path::new(path);
        if p.exists() && p.is_dir() {
            let count = std::fs::read_dir(p)
                .map_err(|e| format!("Failed to read directory: {}", e))?
                .filter_map(|e| e.ok())
                .filter(|e| e.path().is_file())
                .count();
            return Ok(count);
        }
    }
    Ok(0)
}
