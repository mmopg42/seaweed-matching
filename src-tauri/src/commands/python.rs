use crate::python::PythonProcess;
use std::sync::Arc;
use tauri::State;

pub struct AppState {
    pub python: Arc<PythonProcess>,
}

#[tauri::command]
pub async fn python_match_files(
    state: State<'_, AppState>,
    params: serde_json::Value,
) -> Result<serde_json::Value, String> {
    state.python.call("match_files", params)
}

#[tauri::command]
pub async fn python_process_files(
    state: State<'_, AppState>,
    params: serde_json::Value,
) -> Result<serde_json::Value, String> {
    state.python.call("process_files", params)
}

#[tauri::command]
pub async fn python_ping(state: State<'_, AppState>) -> Result<String, String> {
    state.python.ping()
}
