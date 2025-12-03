# abnormal_detector.py
# services/abnormal_detector.py로 이동됨 (Phase 5)

from typing import Optional
from utils.utils import get_image_dimensions


class AbnormalDetector:
    """
    통계적 z-score 기반 이상치 감지 클래스
    
    슬라이딩 윈도우 방식으로 최근 데이터의 평균/표준편차를 기준으로
    새 데이터가 얼마나 튀었는지 판정합니다.
    """
    
    def __init__(self, window_size=100, min_samples=10, threshold=3.0):
        """
        Args:
            window_size: 슬라이딩 윈도우 크기 (기본 100)
            min_samples: 최소 샘플 수, 이 개수 미만이면 판정 보류 (기본 10)
            threshold: z-score 임계값 (기본 3.0 = 3σ)
        """
        self.window_size = window_size
        self.min_samples = min_samples
        self.threshold = threshold
        
        # 슬라이딩 윈도우 버퍼
        self.width_buffer = []
        self.height_buffer = []
    
    def add_and_check_image(self, width: int, height: int) -> tuple[bool, Optional[float], Optional[float]]:
        """
        이미지 크기를 버퍼에 추가하고 이상치 여부 판정
        
        Args:
            width: 이미지 가로 크기 (픽셀)
            height: 이미지 세로 크기 (픽셀)
        
        Returns:
            tuple: (is_abnormal, z_width, z_height)
                - is_abnormal: True (이상치), False (정상 또는 판정 보류)
                - z_width: 가로 z-score (None이면 판정 보류)
                - z_height: 세로 z-score (None이면 판정 보류)
        """
        # 최소 샘플 수 미만이면 판정 보류 (현재 값 추가 전에 체크)
        if len(self.width_buffer) < self.min_samples:
            # 버퍼에 추가만 하고 판정은 보류
            self.width_buffer.append(width)
            self.height_buffer.append(height)
            return False, None, None
        
        # 이전 데이터를 기준으로 z-score 계산 (현재 값 제외)
        z_width = self._calculate_z_score(width, self.width_buffer)
        z_height = self._calculate_z_score(height, self.height_buffer)
        
        # z-score를 계산할 수 없으면 (표준편차=0 등) 정상으로 간주
        if z_width is None or z_height is None:
            # 버퍼에 추가하고 정상으로 간주
            self.width_buffer.append(width)
            self.height_buffer.append(height)
            # 윈도우 크기 유지
            if len(self.width_buffer) > self.window_size:
                self.width_buffer.pop(0)
                self.height_buffer.pop(0)
            return False, None, None
        
        # 이상치 여부 판정
        is_abnormal = abs(z_width) > self.threshold or abs(z_height) > self.threshold
        
        # 버퍼에 추가 (판정 후 추가)
        self.width_buffer.append(width)
        self.height_buffer.append(height)
        
        # 윈도우 크기 유지 (오래된 데이터 제거)
        if len(self.width_buffer) > self.window_size:
            self.width_buffer.pop(0)
            self.height_buffer.pop(0)
        
        return is_abnormal, z_width, z_height

    
    def _calculate_z_score(self, value: float, values_list: list) -> Optional[float]:
        """
        z-score 계산: (value - mean) / std
        
        Args:
            value: 판정할 값
            values_list: 기준이 되는 값들의 리스트
        
        Returns:
            float: z-score, 계산 불가능하면 None
        """
        if not values_list or len(values_list) < 2:
            return None
        
        # 평균 계산
        mean = sum(values_list) / len(values_list)
        
        # 분산 계산
        variance = sum((x - mean) ** 2 for x in values_list) / len(values_list)
        
        # 표준편차 계산
        if variance == 0:
            return None  # 모든 값이 동일하면 z-score 계산 불가
        
        std = variance ** 0.5
        
        # z-score 계산
        z_score = (value - mean) / std
        
        return z_score
    
    def is_image_abnormal(self, image_path: str) -> bool:
        """
        이미지 이상치 여부 판정 (레거시 호환성 메서드)
        
        내부적으로 이미지 크기를 추출한 후 add_and_check_image()를 호출합니다.
        
        Args:
            image_path: 이미지 파일 절대 경로
        
        Returns:
            bool: True (이상치), False (정상)
        """
        dimensions = get_image_dimensions(image_path)
        if dimensions is None:
            return False  # 크기를 알 수 없으면 정상으로 간주
        
        width, height = dimensions
        return self.add_and_check_image(width, height)
    
    def is_group_abnormal(self, group: dict) -> bool:
        """
        그룹이 이상치인지 판정
        
        기준:
        1. NIR-only 그룹 (카메라 없고 NIR만 있는 경우)
        
        Args:
            group: 그룹 데이터
        
        Returns:
            bool: True (이상치), False (정상)
        """
        try:
            # 1. NIR-only 그룹 체크
            camera_data = group.get("카메라", {})
            nir_data = group.get("NIR", {})
            
            # 카메라 데이터가 없고 NIR만 있는 경우
            if not camera_data and nir_data:
                return True
            
            return False
            
        except Exception as e:
            # 에러 발생 시 정상으로 간주
            print(f"[ERROR] is_group_abnormal: {e}")
            return False
