use crate::models::matcher::{FileMatcher, FileMatch};
use std::sync::Mutex;
use tauri::State;

pub struct MatcherState {
    pub matcher: Mutex<FileMatcher>,
}

#[tauri::command]
pub async fn add_file_to_matcher(
    state: State<'_, MatcherState>,
    folder_type: String,
    path: String,
) -> Result<(), String> {
    let mut matcher = state.matcher.lock()
        .map_err(|e| format!("Failed to lock matcher: {}", e))?;

    matcher.add_file(&folder_type, path);
    Ok(())
}

#[tauri::command]
pub async fn match_files_by_timestamp(
    state: State<'_, MatcherState>,
) -> Result<Vec<FileMatch>, String> {
    let mut matcher = state.matcher.lock()
        .map_err(|e| format!("Failed to lock matcher: {}", e))?;

    Ok(matcher.match_by_timestamp())
}

#[tauri::command]
pub async fn reset_matcher(
    state: State<'_, MatcherState>,
) -> Result<(), String> {
    let mut matcher = state.matcher.lock()
        .map_err(|e| format!("Failed to lock matcher: {}", e))?;

    matcher.reset_state();
    Ok(())
}
