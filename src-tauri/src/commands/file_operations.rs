use rayon::prelude::*;
use walkdir::WalkDir;
use crate::models::file_info::FileInfo;
use std::fs;
use std::path::PathBuf;
use tauri::Emitter;

#[tauri::command]
pub async fn scan_directory_parallel(path: String) -> Result<Vec<FileInfo>, String> {
    let entries: Vec<_> = WalkDir::new(&path)
        .into_iter()
        .filter_map(|e| e.ok())
        .filter(|e| e.file_type().is_file())
        .collect();

    let files: Vec<FileInfo> = entries
        .par_iter()
        .filter_map(|entry| {
            let metadata = entry.metadata().ok()?;
            let path = entry.path();
            let is_image = matches!(
                path.extension()?.to_str()?,
                "jpg" | "jpeg" | "png" | "bmp" | "webp"
            );

            Some(FileInfo {
                path: path.to_string_lossy().to_string(),
                name: entry.file_name().to_string_lossy().to_string(),
                size: metadata.len(),
                modified: metadata
                    .modified()
                    .ok()?
                    .duration_since(std::time::UNIX_EPOCH)
                    .ok()?
                    .as_secs(),
                is_image,
            })
        })
        .collect();

    Ok(files)
}

#[tauri::command]
pub async fn match_files_by_pattern(
    path: String,
    pattern: String,
) -> Result<Vec<FileInfo>, String> {
    let regex = regex::Regex::new(&pattern)
        .map_err(|e| format!("Invalid pattern: {}", e))?;

    let entries: Vec<_> = WalkDir::new(&path)
        .into_iter()
        .filter_map(|e| e.ok())
        .filter(|e| e.file_type().is_file())
        .collect();

    let files: Vec<FileInfo> = entries
        .par_iter()
        .filter(|entry| {
            entry
                .file_name()
                .to_str()
                .map(|name| regex.is_match(name))
                .unwrap_or(false)
        })
        .filter_map(|entry| {
            let metadata = entry.metadata().ok()?;
            let path = entry.path();
            let is_image = matches!(
                path.extension()?.to_str()?,
                "jpg" | "jpeg" | "png" | "bmp" | "webp"
            );

            Some(FileInfo {
                path: path.to_string_lossy().to_string(),
                name: entry.file_name().to_string_lossy().to_string(),
                size: metadata.len(),
                modified: metadata
                    .modified()
                    .ok()?
                    .duration_since(std::time::UNIX_EPOCH)
                    .ok()?
                    .as_secs(),
                is_image,
            })
        })
        .collect();

    Ok(files)
}

// 파일 복사/이동 (Python file_operations.py 대체)
#[tauri::command]
pub async fn copy_files(
    sources: Vec<String>,
    destination: String,
    app_handle: tauri::AppHandle,
) -> Result<(), String> {
    let dest_path = PathBuf::from(&destination);
    if !dest_path.exists() {
        fs::create_dir_all(&dest_path)
            .map_err(|e| format!("Failed to create destination: {}", e))?;
    }

    sources.par_iter().try_for_each(|source| {
        let source_path = PathBuf::from(source);
        let file_name = source_path
            .file_name()
            .ok_or("Invalid source path")?;
        let dest_file = dest_path.join(file_name);

        // 충돌 처리
        if dest_file.exists() {
            app_handle.emit("file-conflict", &dest_file).ok();
            return Ok(());
        }

        fs::copy(source, &dest_file)
            .map_err(|e| format!("Failed to copy file: {}", e))?;

        // 진행률 전송
        app_handle.emit("copy-progress", source).ok();

        Ok::<_, String>(())
    })?;

    Ok(())
}

#[tauri::command]
pub async fn move_files(
    sources: Vec<String>,
    destination: String,
    app_handle: tauri::AppHandle,
) -> Result<Vec<(String, String)>, String> {
    let dest_path = PathBuf::from(&destination);
    if !dest_path.exists() {
        fs::create_dir_all(&dest_path)
            .map_err(|e| format!("Failed to create destination: {}", e))?;
    }

    let mut moved_files = Vec::new();

    for source in sources {
        let source_path = PathBuf::from(&source);
        let file_name = source_path
            .file_name()
            .ok_or("Invalid source path")?;
        let dest_file = dest_path.join(file_name);

        // 충돌 처리
        if dest_file.exists() {
            app_handle.emit("file-conflict", &dest_file).ok();
            continue;
        }

        // 이동 (롤백을 위해 기록)
        fs::rename(&source, &dest_file)
            .map_err(|e| format!("Failed to move file: {}", e))?;

        moved_files.push((source.clone(), dest_file.to_string_lossy().to_string()));

        // 진행률 전송
        app_handle.emit("move-progress", &source).ok();
    }

    Ok(moved_files)
}
