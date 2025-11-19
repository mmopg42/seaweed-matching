use crate::models::config::AppConfig;
use crate::utils::config;

#[tauri::command]
pub async fn load_app_config() -> Result<AppConfig, String> {
    Ok(config::load_config())
}

#[tauri::command]
pub async fn save_app_config(config: AppConfig) -> Result<(), String> {
    config::save_config(&config)
}
