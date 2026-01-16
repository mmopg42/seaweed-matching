# Screen Automation

화면 스크린샷 촬영 및 좌표 기반 클릭 자동화 도구입니다.

## 설치

```bash
pip install -r requirements.txt
```

## 사용법

### 1. 스크린샷 촬영

전체 화면 촬영:
```bash
python screenshot.py
```

출력 폴더 지정:
```bash
python screenshot.py -o ./my_screenshots
```

파일 이름 지정:
```bash
python screenshot.py -f my_screenshot
```

화면 크기 확인:
```bash
python screenshot.py --size
```

영역 촬영 (x, y, width, height):
```bash
python screenshot.py -r 100 100 500 300
```

### 2. 클릭 자동화

지정 좌표 클릭:
```bash
python clicker.py -x 500 -y 300
```

더블 클릭:
```bash
python clicker.py -x 500 -y 300 -c 2
```

우클릭:
```bash
python clicker.py -x 500 -y 300 -b right
```

클릭 전 대기:
```bash
python clicker.py -x 500 -y 300 -d 2.5
```

현재 마우스 위치 확인:
```bash
python clicker.py --position
```

### 3. Python 코드에서 사용

```python
from screen_automation import ScreenAutomation

# 초기화
auto = ScreenAutomation(output_dir="./screenshots")

# 스크린샷 촬영
auto.screenshot.capture_full_screen("my_screen")

# 클릭
auto.clicker.click_at(500, 300)

# 스크린샷 후 클릭
auto.capture_and_click(500, 300, capture_filename="before_click")

# 여러 좌표 순차 클릭
positions = [
    (100, 100),
    (200, 200),
    (300, 300)
]
auto.clicker.click_sequence(positions, delay_between=0.5)

# 클릭 위치 저장/불러오기
named_positions = {
    "button1": [100, 100],
    "button2": [200, 200],
    "close": [400, 300]
}
auto.save_click_positions(named_positions)
loaded = auto.load_click_positions()

# 저장된 위치로 클릭 재생
auto.replay_clicks(loaded)
```

## 안전 모드

- 기본적으로 안전 모드가 활성화되어 있습니다
- 마우스를 화면 모서리로 급격하게 움직이면 작업이 중단됩니다
- 안전 모드를 끄려면: `python clicker.py --no-safe -x 100 -y 100`

## 파일 구조

```
screen_automation/
├── __init__.py              # 패키지 초기화
├── screenshot.py            # 스크린샷 기능
├── clicker.py               # 클릭 기능
├── screen_automation.py     # 통합 기능
├── requirements.txt         # 의존성
└── README.md                # 설명서
```
