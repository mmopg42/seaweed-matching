use crate::models::config::AppConfig;
use std::fs;
use std::path::PathBuf;

pub fn get_config_path() -> PathBuf {
    let config_dir = dirs::config_dir()
        .expect("Failed to get config directory");

    let app_config = config_dir.join("prische-matching-codex");
    fs::create_dir_all(&app_config).ok();

    app_config.join("config.json")
}

pub fn load_config() -> AppConfig {
    let config_path = get_config_path();

    if config_path.exists() {
        let content = fs::read_to_string(config_path)
            .unwrap_or_default();

        serde_json::from_str(&content)
            .unwrap_or_default()
    } else {
        AppConfig::default()
    }
}

pub fn save_config(config: &AppConfig) -> Result<(), String> {
    let config_path = get_config_path();

    let content = serde_json::to_string_pretty(config)
        .map_err(|e| format!("Failed to serialize config: {}", e))?;

    fs::write(config_path, content)
        .map_err(|e| format!("Failed to write config: {}", e))?;

    Ok(())
}
