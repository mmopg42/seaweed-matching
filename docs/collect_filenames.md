# collect_filenames.py 간략 문서

## 개요
이동된 파일 이름을 수집하는 스크립트입니다. 작업날짜 폴더에서 실행하여 각 시료명별로 이동된 파일 이름을 JSON으로 수집합니다.

**크기**: 9KB (273 라인)

## 주요 함수

### collect_files_in_directory(dir_path) -> list
디렉토리 내의 모든 파일 이름을 재귀적으로 수집
- **Returns**: 파일 이름 목록 (상대 경로)

### collect_subject_files(subject_path) -> dict
시료명 폴더에서 with NIR / without NIR 구분하여 파일 수집
- **Returns**:
  ```python
  {
      "with_nir": {
          "Nir": [],
          "일반": [],
          "복합 카메라": {}
      },
      "without_nir": {...}
  }
  ```

### collect_all_files(work_date_folder) -> dict
작업날짜 폴더에서 모든 시료명의 파일 수집하고 시료명별로 JSON 저장
- **Returns**:
  ```python
  {
      "date": "20250129",
      "subject_count": 2,
      "total_files": 350,
      "saved_files": ["path1.json", "path2.json"]
  }
  ```

## 사용 방법
```bash
# 작업날짜 폴더 지정
python collect_filenames.py "D:/매칭결과/250114"

# 현재 디렉토리가 작업날짜 폴더인 경우
python collect_filenames.py
```

## 출력 파일
```
<작업날짜 폴더>/<시료명>_files.json
```

예: `D:/매칭결과/250114/sample1_files.json`
