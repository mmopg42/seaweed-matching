# statistics_calculator.py

class StatisticsCalculator:
    """
    그룹 데이터를 기반으로 통계를 계산하는 클래스
    """
    
    def calculate_stats(self, groups: list) -> dict:
        """
        통합 모드 통계 계산
        
        Args:
            groups: 그룹 리스트
            
        Returns:
            dict: 통계 정보 {
                'total': 전체 개수,
                'with_nir': NIR 있는 개수,
                'without_nir': NIR 없는 개수,
                'fail': 누락발생 개수,
                'abnormal': 이상치 개수
            }
        """
        total = sum(1 for g in groups if g.get("카메라"))
        with_nir = sum(1 for g in groups if g.get("NIR"))
        without_nir = max(total - with_nir, 0)
        fail = sum(1 for g in groups if g.get("type") == "누락발생" or not g.get("카메라"))
        
        # abnormal은 is_group_abnormal 함수가 필요하므로 외부에서 계산하거나 전달받음
        # 여기서는 0으로 초기화 (호출하는 쪽에서 별도 계산)
        abnormal = 0
        
        return {
            'total': total,
            'with_nir': with_nir,
            'without_nir': without_nir,
            'fail': fail,
            'abnormal': abnormal
        }
    
    def calculate_stats_separated(self, line1_groups: list, line2_groups: list) -> dict:
        """
        분리 모드 통계 계산 (라인별)
        
        Args:
            line1_groups: 라인1 그룹 리스트
            line2_groups: 라인2 그룹 리스트
            
        Returns:
            dict: 라인별 통계 정보 {
                'line1': {...},
                'line2': {...}
            }
        """
        # Line1 통계
        total_line1 = sum(1 for g in line1_groups if g.get("카메라"))
        with_nir_line1 = sum(1 for g in line1_groups if g.get("NIR"))
        without_nir_line1 = max(total_line1 - with_nir_line1, 0)
        fail_line1 = sum(1 for g in line1_groups if g.get("type") == "누락발생" or not g.get("카메라"))
        abnormal_line1 = 0
        
        # Line2 통계
        total_line2 = sum(1 for g in line2_groups if g.get("카메라"))
        with_nir_line2 = sum(1 for g in line2_groups if g.get("NIR"))
        without_nir_line2 = max(total_line2 - with_nir_line2, 0)
        fail_line2 = sum(1 for g in line2_groups if g.get("type") == "누락발생" or not g.get("카메라"))
        abnormal_line2 = 0
        
        return {
            'line1': {
                'total': total_line1,
                'with_nir': with_nir_line1,
                'without_nir': without_nir_line1,
                'fail': fail_line1,
                'abnormal': abnormal_line1
            },
            'line2': {
                'total': total_line2,
                'with_nir': with_nir_line2,
                'without_nir': without_nir_line2,
                'fail': fail_line2,
                'abnormal': abnormal_line2
            }
        }
