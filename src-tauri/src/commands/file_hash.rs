use sha2::{Sha256, Digest};
use std::fs::File;
use std::io::Read;

#[tauri::command]
pub async fn calculate_file_hash(path: String) -> Result<String, String> {
    let mut file = File::open(&path)
        .map_err(|e| format!("Failed to open file: {}", e))?;

    let mut hasher = Sha256::new();
    let mut buffer = [0; 8192];

    loop {
        let count = file.read(&mut buffer)
            .map_err(|e| format!("Failed to read file: {}", e))?;
        if count == 0 {
            break;
        }
        hasher.update(&buffer[..count]);
    }

    Ok(hex::encode(hasher.finalize()))
}

#[tauri::command]
pub async fn find_duplicates(directory: String) -> Result<Vec<Vec<String>>, String> {
    use std::collections::HashMap;
    use rayon::prelude::*;
    use walkdir::WalkDir;

    let files: Vec<_> = WalkDir::new(&directory)
        .into_iter()
        .filter_map(|e| e.ok())
        .filter(|e| e.file_type().is_file())
        .map(|e| e.path().to_string_lossy().to_string())
        .collect();

    let hashes: Vec<_> = files
        .par_iter()
        .filter_map(|path| {
            let hash = calculate_file_hash_sync(path).ok()?;
            Some((hash, path.clone()))
        })
        .collect();

    let mut hash_map: HashMap<String, Vec<String>> = HashMap::new();
    for (hash, path) in hashes {
        hash_map.entry(hash).or_insert_with(Vec::new).push(path);
    }

    let duplicates: Vec<Vec<String>> = hash_map
        .into_iter()
        .filter(|(_, paths)| paths.len() > 1)
        .map(|(_, paths)| paths)
        .collect();

    Ok(duplicates)
}

fn calculate_file_hash_sync(path: &str) -> Result<String, String> {
    let mut file = File::open(path)
        .map_err(|e| format!("Failed to open file: {}", e))?;

    let mut hasher = Sha256::new();
    let mut buffer = [0; 8192];

    loop {
        let count = file.read(&mut buffer)
            .map_err(|e| format!("Failed to read file: {}", e))?;
        if count == 0 {
            break;
        }
        hasher.update(&buffer[..count]);
    }

    Ok(hex::encode(hasher.finalize()))
}
