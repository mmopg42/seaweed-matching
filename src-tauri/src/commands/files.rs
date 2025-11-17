use crate::models::file_info::FileInfo;
use std::fs;
use std::path::Path;

#[tauri::command]
pub async fn list_files(path: String) -> Result<Vec<FileInfo>, String> {
    let dir_path = Path::new(&path);

    if !dir_path.exists() {
        return Err(format!("Path does not exist: {}", path));
    }

    let mut files = Vec::new();

    match fs::read_dir(dir_path) {
        Ok(entries) => {
            for entry in entries {
                if let Ok(entry) = entry {
                    if let Ok(metadata) = entry.metadata() {
                        if metadata.is_file() {
                            let file_path = entry.path();
                            let is_image = matches!(
                                file_path.extension().and_then(|s| s.to_str()),
                                Some("jpg") | Some("jpeg") | Some("png") | Some("bmp")
                            );

                            files.push(FileInfo {
                                path: file_path.to_string_lossy().to_string(),
                                name: entry.file_name().to_string_lossy().to_string(),
                                size: metadata.len(),
                                modified: metadata
                                    .modified()
                                    .ok()
                                    .and_then(|t| t.duration_since(std::time::UNIX_EPOCH).ok())
                                    .map(|d| d.as_secs())
                                    .unwrap_or(0),
                                is_image,
                            });
                        }
                    }
                }
            }
        }
        Err(e) => return Err(format!("Failed to read directory: {}", e)),
    }

    Ok(files)
}

#[tauri::command]
pub async fn delete_files(paths: Vec<String>) -> Result<(), String> {
    for path in paths {
        if let Err(e) = fs::remove_file(&path) {
            return Err(format!("Failed to delete {}: {}", path, e));
        }
    }
    Ok(())
}
