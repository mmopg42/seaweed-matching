mod commands;
mod models;
mod python;

use commands::files::{list_files, delete_files};
use commands::python::{python_match_files, python_ping, python_process_files, AppState};
use python::PythonProcess;
use std::sync::Arc;

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
        .plugin(tauri_plugin_opener::init());

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
                    .invoke_handler(tauri::generate_handler![
                        list_files,
                        delete_files,
                        python_match_files,
                        python_process_files,
                        python_ping,
                    ])
                    .run(tauri::generate_context!())
                    .expect("error while running tauri application");
            }
            Err(e) => {
                eprintln!("Warning: Failed to start Python process: {}", e);
                eprintln!("Running without Python integration");

                builder
                    .invoke_handler(tauri::generate_handler![
                        list_files,
                        delete_files,
                    ])
                    .run(tauri::generate_context!())
                    .expect("error while running tauri application");
            }
        }
    } else {
        // Run without Python integration
        builder
            .invoke_handler(tauri::generate_handler![
                list_files,
                delete_files,
            ])
            .run(tauri::generate_context!())
            .expect("error while running tauri application");
    }
}
