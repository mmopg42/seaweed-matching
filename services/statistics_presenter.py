"""
통계 표시 프레젠터 서비스

monitoring_app.py의 통계 계산 및 표시 로직을 분리
"""


class StatisticsPresenter:
    """
    통계 데이터를 계산하고 UI 표시 포맷으로 변환하는 프레젠터
    
    StatisticsCalculator가 계산한 통계를 받아서 UI에 표시할 문자열로 변환합니다.
    """
    
    def __init__(self, statistics_calculator=None):
        """
        Args:
            statistics_calculator: StatisticsCalculator 인스턴스 (옵션)
        """
        self.statistics_calculator = statistics_calculator
    
    def format_file_counts(self, nir_count, nir2_count, normal_count, normal2_count,
                          cam1_count, cam2_count, cam3_count, 
                          cam4_count, cam5_count, cam6_count) -> dict:
        """
        파일 카운트를 UI 표시용 문자열로 변환
        
        Args:
            각 폴더별 파일 개수
            
        Returns:
            dict: 레이블별 표시 문자열
                {
                    "lbl_nir_count": "10",
                    "lbl_nir2_count": "5",
                    ...
                }
        """
        return {
            "lbl_nir_count": str(nir_count),
            "lbl_nir2_count": str(nir2_count),
            "lbl_normal_count": str(normal_count),
            "lbl_normal2_count": str(normal2_count),
            "lbl_cam1_count": str(cam1_count),
            "lbl_cam2_count": str(cam2_count),
            "lbl_cam3_count": str(cam3_count),
            "lbl_cam4_count": str(cam4_count),
            "lbl_cam5_count": str(cam5_count),
            "lbl_cam6_count": str(cam6_count),
        }
    
    def format_matching_stats(self, total, with_nir, without_nir, fail) -> dict:
        """
        통합 모드 매칭 통계를 UI 표시용으로 변환
        
        Args:
            total: 전체 그룹 수
            with_nir: NIR 있는 그룹 수
            without_nir: NIR 없는 그룹 수
            fail: 실패 그룹 수
            
        Returns:
            dict: 레이블별 표시 문자열
        """
        return {
            "lbl_total": str(total),
            "lbl_with_nir": str(with_nir),
            "lbl_without_nir": str(without_nir),
            "lbl_fail": str(fail),
        }
    
    def format_separated_stats(self, total_line1, with_nir_line1, without_nir_line1, fail_line1,
                               total_line2, with_nir_line2, without_nir_line2, fail_line2) -> dict:
        """
        분리 모드 매칭 통계를 UI 표시용으로 변환
        
        Args:
            라인1과 라인2의 각 통계 값
            
        Returns:
            dict: 레이블별 표시 문자열
        """
        return {
            # 라인1
            "lbl_total_line1": str(total_line1),
            "lbl_with_nir_line1": str(with_nir_line1),
            "lbl_without_nir_line1": str(without_nir_line1),
            "lbl_fail_line1": str(fail_line1),
            # 라인2
            "lbl_total_line2": str(total_line2),
            "lbl_with_nir_line2": str(with_nir_line2),
            "lbl_without_nir_line2": str(without_nir_line2),
            "lbl_fail_line2": str(fail_line2),
        }
    
    def calculate_group_statistics(self, groups: list, is_separated_mode: bool = False) -> dict:
        """
        그룹 리스트에서 통계를 계산하고 UI 표시용으로 변환
        
        Args:
            groups: 그룹 리스트
            is_separated_mode: 분리 모드 여부
            
        Returns:
            dict: format_matching_stats 또는 format_separated_stats의 결과
        """
        if not self.statistics_calculator:
            # StatisticsCalculator가 없으면 기본 계산
            return self._calculate_basic_stats(groups, is_separated_mode)
        
        # StatisticsCalculator를 사용한 고급 통계
        if is_separated_mode:
            stats_line1 = self._count_groups_by_line(groups, line=1)
            stats_line2 = self._count_groups_by_line(groups, line=2)
            
            return self.format_separated_stats(
                stats_line1["total"], stats_line1["with_nir"], 
                stats_line1["without_nir"], stats_line1["fail"],
                stats_line2["total"], stats_line2["with_nir"],
                stats_line2["without_nir"], stats_line2["fail"]
            )
        else:
            stats = self._count_all_groups(groups)
            return self.format_matching_stats(
                stats["total"], stats["with_nir"], 
                stats["without_nir"], stats["fail"]
            )
    
    def _calculate_basic_stats(self, groups: list, is_separated_mode: bool) -> dict:
        """기본 통계 계산 (StatisticsCalculator 없을 때)"""
        if is_separated_mode:
            stats_line1 = self._count_groups_by_line(groups, line=1)
            stats_line2 = self._count_groups_by_line(groups, line=2)
            
            return self.format_separated_stats(
                stats_line1["total"], stats_line1["with_nir"], 
                stats_line1["without_nir"], stats_line1["fail"],
                stats_line2["total"], stats_line2["with_nir"],
                stats_line2["without_nir"], stats_line2["fail"]
            )
        else:
            stats = self._count_all_groups(groups)
            return self.format_matching_stats(
                stats["total"], stats["with_nir"], 
                stats["without_nir"], stats["fail"]
            )
    
    def _count_all_groups(self, groups: list) -> dict:
        """전체 그룹 통계"""
        total = len(groups)
        with_nir = sum(1 for g in groups if g.get("NIR"))
        without_nir = total - with_nir
        fail = 0  # 실패 그룹 계산 로직은 추후 확장
        
        return {
            "total": total,
            "with_nir": with_nir,
            "without_nir": without_nir,
            "fail": fail
        }
    
    def _count_groups_by_line(self, groups: list, line: int) -> dict:
        """라인별 그룹 통계"""
        line_groups = [g for g in groups if g.get("line") == line]
        return self._count_all_groups(line_groups)
