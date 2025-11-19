use std::fs;
use std::path::Path;
use tauri::Emitter;

#[derive(serde::Serialize)]
pub struct DeleteLog {
    pub timestamp: i64,
    pub deleted_files: Vec<String>,
    pub moved_to: String,
}

#[tauri::command]
pub async fn safe_delete_files(
    paths: Vec<String>,
    delete_folder: String,
    app_handle: tauri::AppHandle,
) -> Result<DeleteLog, String> {
    let delete_dir = Path::new(&delete_folder);

    if !delete_dir.exists() {
        fs::create_dir_all(delete_dir)
            .map_err(|e| format!("Failed to create delete folder: {}", e))?;
    }

    let mut deleted_files = Vec::new();

    for path in paths {
        let src = Path::new(&path);
        if !src.exists() {
            continue;
        }

        let filename = src.file_name()
            .ok_or("Invalid filename")?
            .to_string_lossy()
            .to_string();

        let dest = delete_dir.join(&filename);

        // 충돌 처리
        if dest.exists() {
            app_handle.emit("delete-conflict", &filename).ok();
            continue;
        }

        fs::rename(src, &dest)
            .map_err(|e| format!("Failed to move file: {}", e))?;

        deleted_files.push(path.clone());

        // 진행률 전송
        app_handle.emit("delete-progress", &path).ok();
    }

    let log = DeleteLog {
        timestamp: chrono::Utc::now().timestamp(),
        deleted_files,
        moved_to: delete_folder,
    };

    Ok(log)
}

#[tauri::command]
pub async fn get_delete_history() -> Result<Vec<DeleteLog>, String> {
    // TODO: 삭제 이력 조회 구현
    Ok(Vec::new())
}
