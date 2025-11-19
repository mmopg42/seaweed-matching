use crate::models::spectrum::{SpectrumAnalysisResult, SpectrumAction};
use crate::utils::spectrum_analyzer::{load_spectrum, find_y_variation_in_x_window};
use std::path::PathBuf;
use std::fs;
use tauri::Emitter;

#[tauri::command]
pub async fn analyze_nir_spectrum(
    file_path: String,
) -> Result<SpectrumAnalysisResult, String> {
    let data = load_spectrum(&file_path)?;
    let regions = find_y_variation_in_x_window(&data, 800.0, 50.0);

    let has_valid_regions = !regions.is_empty();
    let action = if has_valid_regions {
        SpectrumAction::Move
    } else {
        SpectrumAction::Delete
    };

    Ok(SpectrumAnalysisResult {
        file_path: file_path.clone(),
        has_valid_regions,
        regions,
        action,
    })
}

#[tauri::command]
pub async fn process_nir_file(
    txt_file_path: String,
    move_folder: String,
    app_handle: tauri::AppHandle,
) -> Result<SpectrumAnalysisResult, String> {
    let result = analyze_nir_spectrum(txt_file_path.clone()).await?;

    let txt_path = PathBuf::from(&txt_file_path);
    let folder_path = txt_path.parent().ok_or("Invalid path")?;
    let filename = txt_path.file_name().ok_or("Invalid filename")?;
    let file_base = txt_path.file_stem().ok_or("Invalid stem")?
        .to_string_lossy();

    // .spc 파일 경로
    let spc_base = if file_base.to_uppercase().ends_with("A") {
        &file_base[..file_base.len()-1]
    } else {
        file_base.as_ref()
    };
    let spc_path = folder_path.join(format!("{}.spc", spc_base));
    let has_spc = spc_path.exists();

    match result.action {
        SpectrumAction::Move => {
            // 조건 만족 - move_folder로 이동
            let dest_dir = PathBuf::from(&move_folder);
            fs::create_dir_all(&dest_dir)
                .map_err(|e| format!("Create dir failed: {}", e))?;

            fs::rename(&txt_path, dest_dir.join(filename))
                .map_err(|e| format!("Move failed: {}", e))?;

            app_handle.emit("nir-file-moved", &txt_file_path).ok();

            if has_spc {
                let spc_name = spc_path.file_name().unwrap();
                fs::rename(&spc_path, dest_dir.join(spc_name))
                    .map_err(|e| format!("Move spc failed: {}", e))?;
                app_handle.emit("nir-spc-moved", spc_path.to_string_lossy()).ok();
            }
        }
        SpectrumAction::Delete => {
            // 조건 불만족 - 삭제
            fs::remove_file(&txt_path)
                .map_err(|e| format!("Delete failed: {}", e))?;
            app_handle.emit("nir-file-deleted", &txt_file_path).ok();

            if has_spc {
                fs::remove_file(&spc_path)
                    .map_err(|e| format!("Delete spc failed: {}", e))?;
                app_handle.emit("nir-spc-deleted", spc_path.to_string_lossy()).ok();
            }
        }
    }

    Ok(result)
}

/// NIR 모니터링 시작 (5.1 file_watcher와 연동)
#[tauri::command]
pub async fn start_nir_monitoring(
    _monitor_path: String,
    _move_path: String,
    _app_handle: tauri::AppHandle,
) -> Result<(), String> {
    // .txt 파일 생성 감지 시 process_nir_file 자동 호출
    // 5.1의 start_watching과 통합
    Ok(())
}
