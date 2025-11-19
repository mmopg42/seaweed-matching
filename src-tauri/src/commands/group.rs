use crate::models::group::FileGroup;

#[tauri::command]
pub async fn create_group(timestamp: i64) -> Result<FileGroup, String> {
    Ok(FileGroup::new(timestamp))
}

#[tauri::command]
pub async fn delete_group(_group_id: String) -> Result<(), String> {
    // 그룹 삭제 로직
    Ok(())
}

#[tauri::command]
pub async fn update_group_metadata(
    _group_id: String,
    _metadata: serde_json::Value,
) -> Result<(), String> {
    // 메타데이터 업데이트
    Ok(())
}
