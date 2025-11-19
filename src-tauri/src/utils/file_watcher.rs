use notify::{Watcher, RecursiveMode};
use notify_debouncer_full::{new_debouncer, DebounceEventResult};
use serde::Serialize;
use std::path::Path;
use std::time::Duration;
use tauri::Emitter;

#[derive(Debug, Clone, Serialize)]
pub struct FileChangeEvent {
    pub path: String,
    pub kind: String,
}

pub fn watch_directory<P: AsRef<Path>>(
    path: P,
    app_handle: tauri::AppHandle,
) -> Result<(), String> {
    let (tx, rx) = std::sync::mpsc::channel();

    let mut debouncer = new_debouncer(
        Duration::from_millis(500),
        None,
        move |result: DebounceEventResult| {
            tx.send(result).unwrap();
        },
    )
    .map_err(|e| format!("Failed to create debouncer: {}", e))?;

    debouncer
        .watcher()
        .watch(path.as_ref(), RecursiveMode::Recursive)
        .map_err(|e| format!("Failed to watch directory: {}", e))?;

    // Emit events to frontend
    std::thread::spawn(move || {
        while let Ok(result) = rx.recv() {
            match result {
                Ok(events) => {
                    for event in events {
                        // Convert to serializable format
                        for path in &event.paths {
                            let change_event = FileChangeEvent {
                                path: path.display().to_string(),
                                kind: format!("{:?}", event.kind),
                            };
                            app_handle.emit("file-changed", &change_event).ok();
                        }
                    }
                }
                Err(errors) => {
                    eprintln!("Watch error: {:?}", errors);
                }
            }
        }
    });

    Ok(())
}
