use std::fs;
use std::path::Path;

#[tauri::command]
pub async fn create_subject_folders(
    output_root: String,
    subjects: Vec<String>,
) -> Result<usize, String> {
    if !Path::new(&output_root).exists() {
        return Err("Output folder does not exist".to_string());
    }

    let mut created_count = 0;

    for subject in subjects {
        if subject.trim().is_empty() {
            continue;
        }

        let subject_dir = Path::new(&output_root).join(&subject);

        // 주 폴더 생성
        if !subject_dir.exists() {
            fs::create_dir_all(&subject_dir)
                .map_err(|e| format!("Failed to create subject folder: {}", e))?;
            created_count += 1;
        }

        // with NIR / without NIR 하위 폴더 생성
        for sub in ["with NIR", "without NIR"] {
            let sub_dir = subject_dir.join(sub);
            if !sub_dir.exists() {
                fs::create_dir_all(&sub_dir)
                    .map_err(|e| format!("Failed to create subfolder: {}", e))?;
            }
        }
    }

    Ok(created_count)
}

#[tauri::command]
pub async fn auto_create_subject_folders(
    output_root: String,
    subject1: String,
    subject2: Option<String>,
) -> Result<bool, String> {
    let mut subjects = vec![subject1];

    if let Some(s2) = subject2 {
        if !s2.is_empty() && !subjects.contains(&s2) {
            subjects.push(s2);
        }
    }

    create_subject_folders(output_root, subjects).await?;
    Ok(true)
}
