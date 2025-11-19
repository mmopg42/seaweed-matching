use csv::ReaderBuilder;
use std::fs::File;
use crate::models::spectrum::{SpectrumData, YVariationRegion};

/// 스펙트럼 파일 로드 (.txt, 공백 구분)
pub fn load_spectrum(file_path: &str) -> Result<SpectrumData, String> {
    let file = File::open(file_path)
        .map_err(|e| format!("Failed to open file: {}", e))?;

    let mut reader = ReaderBuilder::new()
        .delimiter(b' ')
        .flexible(true)
        .comment(Some(b'#'))
        .has_headers(false)
        .from_reader(file);

    let mut x_values = Vec::new();
    let mut y_values = Vec::new();

    for result in reader.records() {
        let record = result.map_err(|e| format!("Parse error: {}", e))?;
        if record.len() >= 2 {
            if let (Ok(x), Ok(y)) = (
                record[0].trim().parse::<f64>(),
                record[1].trim().parse::<f64>()
            ) {
                x_values.push(x);
                y_values.push(y);
            }
        }
    }

    Ok(SpectrumData { x: x_values, y: y_values })
}

/// Y 변화 구간 찾기 (x 윈도우: 800, stride: 50)
/// 조건: 0.05 <= y_range <= 0.1
pub fn find_y_variation_in_x_window(
    data: &SpectrumData,
    x_window: f64,
    stride: f64,
) -> Vec<YVariationRegion> {
    let mut results = Vec::new();

    // x 범위 필터링: 4500 ~ 6500
    let filtered: Vec<(f64, f64)> = data.x.iter()
        .zip(data.y.iter())
        .filter(|(x, _)| **x >= 4500.0 && **x <= 6500.0)
        .map(|(x, y)| (*x, *y))
        .collect();

    if filtered.is_empty() {
        return results;
    }

    let x_min = filtered.iter().map(|(x, _)| x).fold(f64::INFINITY, |a, &b| a.min(b));
    let x_max = filtered.iter().map(|(x, _)| x).fold(f64::NEG_INFINITY, |a, &b| a.max(b));

    let mut current_x = x_min;
    while current_x + x_window <= x_max {
        let window_y: Vec<f64> = filtered.iter()
            .filter(|(x, _)| *x >= current_x && *x <= current_x + x_window)
            .map(|(_, y)| *y)
            .collect();

        if !window_y.is_empty() {
            let y_min = window_y.iter().fold(f64::INFINITY, |a, &b| a.min(b));
            let y_max = window_y.iter().fold(f64::NEG_INFINITY, |a, &b| a.max(b));
            let y_range = y_max - y_min;

            if y_range >= 0.05 && y_range <= 0.1 {
                results.push(YVariationRegion {
                    x_start: current_x,
                    x_end: current_x + x_window,
                    y_range,
                });
            }
        }
        current_x += stride;
    }

    results
}
