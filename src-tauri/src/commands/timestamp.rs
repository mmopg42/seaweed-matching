use crate::utils::timestamp;

#[tauri::command]
pub async fn parse_timestamp(s: String) -> Result<Option<i64>, String> {
    Ok(timestamp::extract_datetime_from_str(&s)
        .map(|dt| dt.timestamp()))
}

#[tauri::command]
pub async fn parse_yml_timestamp(path: String) -> Result<Option<i64>, String> {
    Ok(timestamp::get_timestamp_from_yml(&path)
        .map(|dt| dt.timestamp()))
}
