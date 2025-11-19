use crate::models::nir_bundle::{NirBundle, NirFile};
use crate::utils::timestamp::extract_datetime_from_str;
use std::collections::HashMap;
use std::path::Path;
use tauri::Emitter;

#[tauri::command]
pub async fn prune_nir_files(
    target_groups: Vec<serde_json::Value>,
    keep_count: usize,
    delete_folder: String,
    app_handle: tauri::AppHandle,
) -> Result<usize, String> {
    if keep_count == 0 {
        return Ok(0);
    }

    // 1. NIR 묶음 수집
    let mut bundles = Vec::new();

    for group in target_groups {
        let nir_map = group.get("NIR")
            .and_then(|v| v.as_object())
            .ok_or("Invalid NIR map")?;

        let mut file_buckets: HashMap<String, Vec<NirFile>> = HashMap::new();

        for (fname, finfo) in nir_map {
            let base_key = extract_nir_base(fname);
            let path = finfo.get("absolute_path")
                .and_then(|p| p.as_str())
                .unwrap_or("");

            let file_type = Path::new(fname)
                .extension()
                .and_then(|e| e.to_str())
                .unwrap_or("unknown")
                .to_string();

            file_buckets.entry(base_key.clone()).or_insert_with(Vec::new).push(NirFile {
                name: fname.clone(),
                path: path.to_string(),
                file_type,
            });
        }

        // 각 묶음을 bundles에 추가
        for (base_key, files) in file_buckets {
            if let Some(timestamp) = extract_datetime_from_str(&base_key) {
                bundles.push(NirBundle {
                    base_key,
                    timestamp,
                    files,
                });
            }
        }
    }

    if bundles.len() <= keep_count {
        return Ok(0);
    }

    // 2. 오래된 순으로 정렬
    bundles.sort_by_key(|b| b.timestamp);

    // 3. keep_count 이후 삭제
    let to_delete = &bundles[keep_count..];
    let mut moved_count = 0;

    for bundle in to_delete {
        for file in &bundle.files {
            if Path::new(&file.path).exists() {
                let dest = Path::new(&delete_folder)
                    .join("NIR")
                    .join(&file.name);

                if let Some(parent) = dest.parent() {
                    std::fs::create_dir_all(parent).ok();
                }

                std::fs::rename(&file.path, &dest)
                    .map_err(|e| format!("Failed to move file: {}", e))?;

                moved_count += 1;

                // 진행률 전송
                app_handle.emit("nir-prune-progress", &file.name).ok();
            }
        }
    }

    Ok(moved_count)
}

fn extract_nir_base(filename: &str) -> String {
    // NIR 파일명에서 베이스 키 추출 로직
    // 예: "20240115_120000.spc" -> "20240115_120000"
    filename
        .trim_end_matches(".spc")
        .trim_end_matches("A.txt")
        .trim_end_matches("_A.txt")
        .to_string()
}
