use serde::{Deserialize, Serialize};
use uuid::Uuid;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct FileGroup {
    pub id: String,
    pub timestamp: i64,
    pub normal_file: Option<String>,
    pub normal2_file: Option<String>,
    pub nir_files: Vec<String>,
    pub nir2_files: Vec<String>,
    pub cam_files: Vec<String>,
    pub metadata: GroupMetadata,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GroupMetadata {
    pub created_at: i64,
    pub modified_at: i64,
    pub status: GroupStatus,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub enum GroupStatus {
    Pending,
    Matched,
    Failed,
}

impl FileGroup {
    pub fn new(timestamp: i64) -> Self {
        let now = chrono::Utc::now().timestamp();
        Self {
            id: Uuid::new_v4().to_string(),
            timestamp,
            normal_file: None,
            normal2_file: None,
            nir_files: Vec::new(),
            nir2_files: Vec::new(),
            cam_files: Vec::new(),
            metadata: GroupMetadata {
                created_at: now,
                modified_at: now,
                status: GroupStatus::Pending,
            },
        }
    }
}
