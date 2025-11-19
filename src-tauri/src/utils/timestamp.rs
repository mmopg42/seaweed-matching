use chrono::{DateTime, NaiveDateTime, Utc};
use regex::Regex;
use std::fs;
use std::path::Path;

/// Python의 extract_datetime_from_str() 대체
pub fn extract_datetime_from_str(s: &str) -> Option<DateTime<Utc>> {
    // 패턴: YYYYMMDD_HHMMSS or YYYYMMDD-HHMMSS
    let re = Regex::new(r"(\d{8})[_-](\d{6})").ok()?;
    let caps = re.captures(s)?;

    let date_str = caps.get(1)?.as_str();
    let time_str = caps.get(2)?.as_str();

    let datetime_str = format!("{}{}", date_str, time_str);

    NaiveDateTime::parse_from_str(&datetime_str, "%Y%m%d%H%M%S")
        .ok()
        .map(|dt| DateTime::<Utc>::from_naive_utc_and_offset(dt, Utc))
}

/// Python의 get_timestamp_from_yml() 대체
pub fn get_timestamp_from_yml<P: AsRef<Path>>(path: P) -> Option<DateTime<Utc>> {
    let content = fs::read_to_string(path).ok()?;
    let value: serde_yaml::Value = serde_yaml::from_str(&content).ok()?;

    // YML에서 timestamp 필드 추출
    let timestamp = value.get("timestamp")?.as_str()?;

    extract_datetime_from_str(timestamp)
}

/// NIR 키에서 날짜/시간 추출
pub fn extract_datetime_from_nir_key(nir_key: &str) -> Option<DateTime<Utc>> {
    // NIR 키 형식에 맞춰 파싱
    extract_datetime_from_str(nir_key)
}
