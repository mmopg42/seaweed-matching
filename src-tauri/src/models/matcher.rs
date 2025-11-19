use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FileMatch {
    pub timestamp: i64,  // Unix timestamp
    pub normal_file: Option<String>,
    pub normal2_file: Option<String>,
    pub nir_file: Option<String>,
    pub nir2_file: Option<String>,
    pub cam_files: Vec<String>,
}

#[derive(Debug, Clone)]
pub struct FileMatcher {
    unmatched_files: HashMap<String, Vec<String>>,
    consumed_nir_keys: Vec<String>,
    matches: Vec<FileMatch>,
}

impl FileMatcher {
    pub fn new() -> Self {
        Self {
            unmatched_files: HashMap::new(),
            consumed_nir_keys: Vec::new(),
            matches: Vec::new(),
        }
    }

    pub fn add_file(&mut self, folder_type: &str, path: String) {
        self.unmatched_files
            .entry(folder_type.to_string())
            .or_insert_with(Vec::new)
            .push(path);
    }

    pub fn match_by_timestamp(&mut self) -> Vec<FileMatch> {
        // 타임스탬프 기반 매칭 로직
        // TODO: Python의 FileMatcher.match_files() 구현 필요
        // 지금은 빈 결과 반환
        Vec::new()
    }

    pub fn reset_state(&mut self) {
        self.unmatched_files.clear();
        self.consumed_nir_keys.clear();
        self.matches.clear();
    }

    pub fn get_matches(&self) -> &Vec<FileMatch> {
        &self.matches
    }
}

impl Default for FileMatcher {
    fn default() -> Self {
        Self::new()
    }
}
