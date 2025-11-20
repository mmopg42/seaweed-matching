mod commands;
mod models;
mod python;
mod utils;

use commands::files::{list_files, delete_files};
use commands::file_watcher::start_watching;
use commands::file_operations::{scan_directory_parallel, match_files_by_pattern, copy_files, move_files};
use commands::image::{get_image_metadata, get_images_batch, generate_thumbnail, get_cached_image, cache_image, clear_image_cache, ImageCacheState};
use commands::file_hash::{calculate_file_hash, find_duplicates};
use commands::matcher::{MatcherState, add_file_to_matcher, match_files_by_timestamp, reset_matcher};
use commands::timestamp::{parse_timestamp, parse_yml_timestamp};
use commands::config::{load_app_config, save_app_config};
use commands::delete_operations::{safe_delete_files, get_delete_history};
use commands::folder_operations::{create_subject_folders, auto_create_subject_folders};
use commands::python::{python_match_files, python_ping, python_process_files, AppState};
use commands::nir_spectrum::{analyze_nir_spectrum, process_nir_file, start_nir_monitoring};
use commands::nir_operations::prune_nir_files;
use commands::group::{create_group, delete_group, update_group_metadata};
use commands::file_stats::get_folder_stats;
use commands::dialog::{select_folder, select_file, open_folder_in_explorer};
use models::matcher::FileMatcher;
use utils::image_cache::ImageCache;
use std::sync::Mutex;
use python::PythonProcess;
use std::sync::Arc;
use tauri::Manager;

fn get_python_script_path() -> String {
    // In development, use relative path from project root
    // In production, Python script should be bundled with the app
    #[cfg(debug_assertions)]
    {
        "./python_backend/server.py".to_string()
    }
    #[cfg(not(debug_assertions))]
    {
        // TODO: Update this path for production builds
        // The Python script should be included in the app bundle
        "./python_backend/server.py".to_string()
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    // Initialize Python process (optional - can be enabled when needed)
    let python_enabled = std::env::var("ENABLE_PYTHON").is_ok();

    let builder = tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .setup(|app| {
            // Setup window close event handler
            let windows = app.webview_windows();
            for (_, window) in windows {
                window.on_window_event(move |event| {
                    if let tauri::WindowEvent::CloseRequested { .. } = event {
                        println!("Application closing - cleaning up resources...");
                        // Note: File watcher threads will be cleaned up when process terminates
                        // Image cache and other resources are automatically dropped
                    }
                });
            }
            Ok(())
        });

    if python_enabled {
        // Initialize Python process
        let python_script = get_python_script_path();
        match PythonProcess::new(&python_script) {
            Ok(python) => {
                let app_state = AppState {
                    python: Arc::new(python),
                };

                builder
                    .manage(app_state)
                    .manage(MatcherState {
                        matcher: Mutex::new(FileMatcher::new()),
                    })
                    .manage(ImageCacheState {
                        cache: ImageCache::new(100),
                    })
                    .invoke_handler(tauri::generate_handler![
                        list_files,
                        delete_files,
                        start_watching,
                        scan_directory_parallel,
                        match_files_by_pattern,
                        copy_files,
                        move_files,
                        get_image_metadata,
                        get_images_batch,
                        generate_thumbnail,
                        get_cached_image,
                        cache_image,
                        clear_image_cache,
                        calculate_file_hash,
                        find_duplicates,
                        add_file_to_matcher,
                        match_files_by_timestamp,
                        reset_matcher,
                        parse_timestamp,
                        parse_yml_timestamp,
                        load_app_config,
                        save_app_config,
                        safe_delete_files,
                        get_delete_history,
                        create_subject_folders,
                        auto_create_subject_folders,
                        python_match_files,
                        python_process_files,
                        python_ping,
                        analyze_nir_spectrum,
                        process_nir_file,
                        start_nir_monitoring,
                        prune_nir_files,
                        create_group,
                        delete_group,
                        update_group_metadata,
                        get_folder_stats,
                        select_folder,
                        select_file,
                        open_folder_in_explorer,
                    ])
                    .run(tauri::generate_context!())
                    .expect("error while running tauri application");
            }
            Err(e) => {
                eprintln!("Warning: Failed to start Python process: {}", e);
                eprintln!("Running without Python integration");

                builder
                    .manage(MatcherState {
                        matcher: Mutex::new(FileMatcher::new()),
                    })
                    .manage(ImageCacheState {
                        cache: ImageCache::new(100),
                    })
                    .invoke_handler(tauri::generate_handler![
                        list_files,
                        delete_files,
                        start_watching,
                        scan_directory_parallel,
                        match_files_by_pattern,
                        copy_files,
                        move_files,
                        get_image_metadata,
                        get_images_batch,
                        generate_thumbnail,
                        get_cached_image,
                        cache_image,
                        clear_image_cache,
                        calculate_file_hash,
                        find_duplicates,
                        add_file_to_matcher,
                        match_files_by_timestamp,
                        reset_matcher,
                        parse_timestamp,
                        parse_yml_timestamp,
                        load_app_config,
                        save_app_config,
                        safe_delete_files,
                        get_delete_history,
                        create_subject_folders,
                        auto_create_subject_folders,
                        analyze_nir_spectrum,
                        process_nir_file,
                        start_nir_monitoring,
                        prune_nir_files,
                        create_group,
                        delete_group,
                        update_group_metadata,
                        get_folder_stats,
                        select_folder,
                        select_file,
                        open_folder_in_explorer,
                    ])
                    .run(tauri::generate_context!())
                    .expect("error while running tauri application");
            }
        }
    } else {
        // Run without Python integration
        builder
            .manage(MatcherState {
                matcher: Mutex::new(FileMatcher::new()),
            })
            .manage(ImageCacheState {
                cache: ImageCache::new(100),
            })
            .invoke_handler(tauri::generate_handler![
                list_files,
                delete_files,
                start_watching,
                scan_directory_parallel,
                match_files_by_pattern,
                copy_files,
                move_files,
                get_image_metadata,
                get_images_batch,
                generate_thumbnail,
                get_cached_image,
                cache_image,
                clear_image_cache,
                calculate_file_hash,
                find_duplicates,
                add_file_to_matcher,
                match_files_by_timestamp,
                reset_matcher,
                parse_timestamp,
                parse_yml_timestamp,
                load_app_config,
                save_app_config,
                safe_delete_files,
                get_delete_history,
                create_subject_folders,
                auto_create_subject_folders,
                analyze_nir_spectrum,
                process_nir_file,
                start_nir_monitoring,
                prune_nir_files,
                create_group,
                delete_group,
                update_group_metadata,
                get_folder_stats,
                select_folder,
                select_file,
                open_folder_in_explorer,
            ])
            .run(tauri::generate_context!())
            .expect("error while running tauri application");
    }
}
