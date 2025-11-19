use crate::utils::file_watcher;

#[tauri::command]
pub async fn start_watching(
    path: String,
    app_handle: tauri::AppHandle,
) -> Result<(), String> {
    file_watcher::watch_directory(path, app_handle)
}
