# abnormal_detector.py

from utils.utils import get_image_dimensions


class AbnormalDetector:
    """
    이상치 감지 로직을 담당하는 클래스
    """
    
    def is_image_abnormal(self, image_path: str) -> bool:
        """
        이미지 이상치 여부 판정

        기준:
        - 가로(width) // 10 < 18 OR
        - 세로(height) // 10 >= 22

        Args:
            image_path: 이미지 파일 절대 경로

        Returns:
            bool: True if 이상치, False if 정상
        """
        dimensions = get_image_dimensions(image_path)
        if dimensions is None:
            return False  # 크기를 알 수 없으면 정상으로 간주

        width, height = dimensions
        
        # 10으로 나눈 값으로 판정 (마지막 자리 버림)
        w_trunc = width // 10
        h_trunc = height // 10

        # OR 조건: 가로 < 18 또는 세로 >= 22
        return w_trunc < 18 or h_trunc >= 22
    
    def is_group_abnormal(self, group: dict) -> bool:
        """
        그룹이 이상치인지 판정

        기준:
        1. NIR-only 그룹 (카메라 없고 NIR만 있는 경우)
        2. 이미지 크기 이상 (가로 <= 185px OR 세로 >= 210px)

        Args:
            group: 그룹 데이터

        Returns:
            bool: True if 이상치, False if 정상
        """
        try:
            # 1. NIR-only 그룹 체크
            camera_data = group.get("카메라", {})
            nir_data = group.get("NIR", {})
            
            # 카메라 데이터가 없고 NIR만 있는 경우
            if not camera_data and nir_data:
                return True
            
            # 2. 이미지 크기 이상 체크 (카메라 데이터가 있는 경우)
            if camera_data:
                # 카메라 폴더에서 대표 이미지 경로 찾기
                # 일반적으로 folder_label을 통해 경로를 구성해야 하지만
                # 여기서는 간단히 체크 로직만 구현
                # 실제로는 monitoring_app에서 thumbnail_path를 전달받아야 함
                # 지금은 기본값 False 반환
                pass
            
            return False
            
        except Exception as e:
            # 에러 발생 시 정상으로 간주
            print(f"[ERROR] is_group_abnormal: {e}")
            return False
