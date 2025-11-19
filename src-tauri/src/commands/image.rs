use crate::models::image_info::ImageMetadata;
use std::fs;
use image::imageops::FilterType;

// ImageCacheState 정의
pub struct ImageCacheState {
    pub cache: crate::utils::image_cache::ImageCache,
}

#[tauri::command]
pub async fn get_image_metadata(path: String) -> Result<ImageMetadata, String> {
    let img = image::open(&path)
        .map_err(|e| format!("Failed to open image: {}", e))?;

    let metadata = fs::metadata(&path)
        .map_err(|e| format!("Failed to get file metadata: {}", e))?;

    Ok(ImageMetadata {
        width: img.width(),
        height: img.height(),
        format: format!("{:?}", img.color()),
        color_type: format!("{:?}", img.color()),
        file_size: metadata.len(),
    })
}

#[tauri::command]
pub async fn get_images_batch(paths: Vec<String>) -> Result<Vec<ImageMetadata>, String> {
    use rayon::prelude::*;

    let results: Vec<ImageMetadata> = paths
        .par_iter()
        .filter_map(|path| {
            let img = image::open(path).ok()?;
            let metadata = fs::metadata(path).ok()?;

            Some(ImageMetadata {
                width: img.width(),
                height: img.height(),
                format: format!("{:?}", img.color()),
                color_type: format!("{:?}", img.color()),
                file_size: metadata.len(),
            })
        })
        .collect();

    Ok(results)
}

#[tauri::command]
pub async fn generate_thumbnail(
    path: String,
    max_width: u32,
    max_height: u32,
) -> Result<Vec<u8>, String> {
    let img = image::open(&path)
        .map_err(|e| format!("Failed to open image: {}", e))?;

    let thumbnail = img.resize(max_width, max_height, FilterType::Lanczos3);

    let mut buffer = Vec::new();
    thumbnail
        .write_to(&mut std::io::Cursor::new(&mut buffer), image::ImageFormat::Jpeg)
        .map_err(|e| format!("Failed to encode thumbnail: {}", e))?;

    Ok(buffer)
}

#[tauri::command]
pub async fn get_cached_image(
    state: tauri::State<'_, ImageCacheState>,
    path: String,
) -> Result<Option<Vec<u8>>, String> {
    Ok(state.cache.get(&path))
}

#[tauri::command]
pub async fn cache_image(
    state: tauri::State<'_, ImageCacheState>,
    path: String,
    data: Vec<u8>,
) -> Result<(), String> {
    state.cache.put(path, data);
    Ok(())
}

#[tauri::command]
pub async fn clear_image_cache(
    state: tauri::State<'_, ImageCacheState>,
) -> Result<(), String> {
    state.cache.clear();
    Ok(())
}
