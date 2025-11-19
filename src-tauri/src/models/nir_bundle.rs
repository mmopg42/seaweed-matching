use serde::{Deserialize, Serialize};
use chrono::{DateTime, Utc};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NirBundle {
    pub base_key: String,
    pub timestamp: DateTime<Utc>,
    pub files: Vec<NirFile>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NirFile {
    pub name: String,
    pub path: String,
    pub file_type: String,  // .spc, A.txt, etc
}
