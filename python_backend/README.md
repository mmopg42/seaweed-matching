# Python Backend

Python 사이드카 서버로 Rust/Tauri 앱과 JSON-RPC를 통해 통신합니다.

## 설치

```bash
# 의존성 설치
pip install -r requirements.txt
```

## 사용법

### 개발 모드에서 Python 통합 활성화

```bash
# 환경 변수 설정
export ENABLE_PYTHON=1

# Tauri 앱 실행
npm run tauri dev
```

### 직접 테스트

```bash
# Python 서버 실행
python server.py

# JSON-RPC 요청 보내기
echo '{"method": "ping", "params": {}}' | python server.py
```

## 구조

- `server.py` - JSON-RPC 서버 메인 파일
- `file_matcher.py` - 파일 매칭 로직 (구현 필요)
- `file_operations.py` - 파일 작업 로직 (구현 필요)
- `requirements.txt` - Python 의존성

## 기존 Python 코드 통합

기존 Python 로직이 있다면:

1. `file_matcher.py`와 `file_operations.py`를 실제 구현으로 교체
2. `server.py`의 메서드들을 실제 로직과 연결
3. 필요한 의존성을 `requirements.txt`에 추가
