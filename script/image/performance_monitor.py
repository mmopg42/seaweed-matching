# performance_monitor.py
"""
이미지 로딩 성능 모니터링 시스템

캐시 히트율, 로딩 시간, 에러율 등의 성능 메트릭을 수집하고 분석합니다.
"""

import time
import psutil
import os
from typing import Dict, Any, List, Optional
from collections import deque


class PerformanceMonitor:
    """
    이미지 로딩 성능 메트릭 수집 및 분석
    
    ✅ Task 11.1: PerformanceMonitor 클래스 구현
    - 캐시 히트/미스 카운트
    - 평균 로딩 시간 추적
    - 타임아웃/에러 카운트
    
    Requirements: 7.4, 7.5
    """
    
    def __init__(self, max_history: int = 1000):
        """
        성능 모니터 초기화
        
        Args:
            max_history: 유지할 최대 로딩 시간 기록 수 (메모리 절약)
        """
        # 캐시 통계
        self.cache_hits = 0
        self.cache_misses = 0
        
        # 로딩 시간 기록 (deque로 최근 N개만 유지)
        self.load_times = deque(maxlen=max_history)
        
        # 에러 통계
        self.timeout_count = 0
        self.error_count = 0
        self.permanent_failure_count = 0
        
        # 재시도 통계
        self.retry_count = 0
        self.retry_success_count = 0
        
        # 총 로딩 횟수
        self.total_loads = 0
        
        # 시작 시간
        self.start_time = time.time()
        
        # ✅ Task 12.1: 메모리 사용량 추적
        self.process = psutil.Process(os.getpid())
        self.peak_memory_mb = 0.0
        self.memory_warnings = 0
    
    def record_cache_hit(self):
        """
        캐시 히트 기록
        
        메모리 또는 디스크 캐시에서 이미지를 찾았을 때 호출
        """
        self.cache_hits += 1
    
    def record_cache_miss(self):
        """
        캐시 미스 기록
        
        캐시에 이미지가 없어서 새로 로드해야 할 때 호출
        """
        self.cache_misses += 1
    
    def record_load_time(self, duration_ms: float):
        """
        이미지 로딩 시간 기록
        
        Args:
            duration_ms: 로딩 소요 시간 (밀리초)
        """
        self.load_times.append(duration_ms)
        self.total_loads += 1
    
    def record_timeout(self):
        """타임아웃 발생 기록"""
        self.timeout_count += 1
        self.error_count += 1
    
    def record_error(self):
        """일반 에러 발생 기록"""
        self.error_count += 1
    
    def record_permanent_failure(self):
        """영구 실패 기록 (최대 재시도 초과)"""
        self.permanent_failure_count += 1
        self.error_count += 1
    
    def record_retry(self):
        """재시도 시도 기록"""
        self.retry_count += 1
    
    def record_retry_success(self):
        """재시도 성공 기록"""
        self.retry_success_count += 1
    
    def get_cache_hit_rate(self) -> float:
        """
        ✅ Task 11.2: 캐시 히트율 계산
        
        Returns:
            float: 캐시 히트율 (0.0 ~ 1.0), 데이터 없으면 0.0
        """
        total = self.cache_hits + self.cache_misses
        if total == 0:
            return 0.0
        return self.cache_hits / total
    
    def get_average_load_time(self) -> float:
        """
        ✅ Task 11.2: 평균 로딩 시간 계산
        
        Returns:
            float: 평균 로딩 시간 (밀리초), 데이터 없으면 0.0
        """
        if not self.load_times:
            return 0.0
        return sum(self.load_times) / len(self.load_times)
    
    def get_median_load_time(self) -> float:
        """
        ✅ Task 11.2: 중앙값 로딩 시간 계산
        
        Returns:
            float: 중앙값 로딩 시간 (밀리초), 데이터 없으면 0.0
        """
        if not self.load_times:
            return 0.0
        
        sorted_times = sorted(self.load_times)
        n = len(sorted_times)
        mid = n // 2
        
        if n % 2 == 0:
            return (sorted_times[mid - 1] + sorted_times[mid]) / 2
        else:
            return sorted_times[mid]
    
    def get_percentile_load_time(self, percentile: float) -> float:
        """
        ✅ Task 11.2: 백분위수 로딩 시간 계산
        
        Args:
            percentile: 백분위수 (0.0 ~ 1.0, 예: 0.95 = 95th percentile)
            
        Returns:
            float: 해당 백분위수의 로딩 시간 (밀리초), 데이터 없으면 0.0
        """
        if not self.load_times:
            return 0.0
        
        sorted_times = sorted(self.load_times)
        index = int(len(sorted_times) * percentile)
        index = min(index, len(sorted_times) - 1)
        return sorted_times[index]
    
    def get_error_rate(self) -> float:
        """
        에러율 계산
        
        Returns:
            float: 에러율 (0.0 ~ 1.0), 데이터 없으면 0.0
        """
        total = self.total_loads + self.error_count
        if total == 0:
            return 0.0
        return self.error_count / total
    
    def get_retry_success_rate(self) -> float:
        """
        재시도 성공률 계산
        
        Returns:
            float: 재시도 성공률 (0.0 ~ 1.0), 재시도 없으면 0.0
        """
        if self.retry_count == 0:
            return 0.0
        return self.retry_success_count / self.retry_count
    
    def get_uptime_seconds(self) -> float:
        """
        모니터 가동 시간 계산
        
        Returns:
            float: 가동 시간 (초)
        """
        return time.time() - self.start_time
    
    def get_statistics(self) -> Dict[str, Any]:
        """
        ✅ Task 11.2: 성능 통계 수집
        ✅ Task 12.1: 메모리 통계 포함
        
        모든 성능 메트릭을 포함하는 딕셔너리 반환
        
        Returns:
            dict: 성능 통계 정보
        """
        return {
            # 캐시 통계
            "cache_hits": self.cache_hits,
            "cache_misses": self.cache_misses,
            "cache_hit_rate": self.get_cache_hit_rate(),
            
            # 로딩 시간 통계
            "total_loads": self.total_loads,
            "avg_load_time_ms": self.get_average_load_time(),
            "median_load_time_ms": self.get_median_load_time(),
            "p95_load_time_ms": self.get_percentile_load_time(0.95),
            "min_load_time_ms": min(self.load_times) if self.load_times else 0.0,
            "max_load_time_ms": max(self.load_times) if self.load_times else 0.0,
            
            # 에러 통계
            "error_count": self.error_count,
            "timeout_count": self.timeout_count,
            "permanent_failure_count": self.permanent_failure_count,
            "error_rate": self.get_error_rate(),
            
            # 재시도 통계
            "retry_count": self.retry_count,
            "retry_success_count": self.retry_success_count,
            "retry_success_rate": self.get_retry_success_rate(),
            
            # 시스템 통계
            "uptime_seconds": self.get_uptime_seconds(),
            "loads_per_second": self.total_loads / max(self.get_uptime_seconds(), 1.0),
            
            # ✅ Task 12.1: 메모리 통계
            "current_memory_mb": self.get_current_memory_mb(),
            "peak_memory_mb": self.peak_memory_mb,
            "system_memory_percent": self.get_system_memory_percent(),
            "memory_warnings": self.memory_warnings
        }
    
    def get_summary_string(self) -> str:
        """
        ✅ Task 11.3: 성능 통계 표시용 문자열 생성
        ✅ Task 12.1: 메모리 정보 포함
        
        사용자에게 보여줄 간단한 요약 문자열 반환
        
        Returns:
            str: 성능 요약 문자열
        """
        stats = self.get_statistics()
        
        summary_lines = [
            f"캐시 히트율: {stats['cache_hit_rate']*100:.1f}%",
            f"평균 로딩 시간: {stats['avg_load_time_ms']:.0f}ms",
            f"중앙값 로딩 시간: {stats['median_load_time_ms']:.0f}ms",
            f"95th percentile: {stats['p95_load_time_ms']:.0f}ms",
            f"총 로딩 횟수: {stats['total_loads']}",
            f"에러율: {stats['error_rate']*100:.1f}%",
            f"타임아웃: {stats['timeout_count']}회",
            f"재시도 성공률: {stats['retry_success_rate']*100:.1f}%",
            f"메모리 사용량: {stats['current_memory_mb']:.1f} MB (Peak: {stats['peak_memory_mb']:.1f} MB)",
            f"시스템 메모리: {stats['system_memory_percent']:.1f}%"
        ]
        
        return "\n".join(summary_lines)
    
    def get_current_memory_mb(self) -> float:
        """
        ✅ Task 12.1: 현재 프로세스 메모리 사용량 반환 (MB)
        
        Returns:
            float: 현재 메모리 사용량 (MB)
        """
        try:
            mem_info = self.process.memory_info()
            current_mb = mem_info.rss / 1024 / 1024
            
            # Peak 메모리 업데이트
            if current_mb > self.peak_memory_mb:
                self.peak_memory_mb = current_mb
            
            return current_mb
        except Exception as e:
            print(f"[WARN] 메모리 사용량 조회 실패: {e}")
            return 0.0
    
    def get_system_memory_percent(self) -> float:
        """
        ✅ Task 12.1: 시스템 전체 메모리 사용률 반환 (%)
        
        Returns:
            float: 시스템 메모리 사용률 (0.0 ~ 100.0)
        """
        try:
            sys_mem = psutil.virtual_memory()
            return sys_mem.percent
        except Exception as e:
            print(f"[WARN] 시스템 메모리 사용률 조회 실패: {e}")
            return 0.0
    
    def get_cache_memory_estimate_mb(self, cache_size: int, avg_pixmap_size_kb: float = 50.0) -> float:
        """
        ✅ Task 12.1: 캐시 메모리 사용량 추정 (MB)
        
        Args:
            cache_size: 캐시에 저장된 항목 수
            avg_pixmap_size_kb: 평균 pixmap 크기 (KB, 기본값: 50KB)
            
        Returns:
            float: 추정 캐시 메모리 사용량 (MB)
        """
        return (cache_size * avg_pixmap_size_kb) / 1024.0
    
    def record_memory_warning(self):
        """
        ✅ Task 12.3: 메모리 경고 기록
        """
        self.memory_warnings += 1
    
    def reset(self):
        """모든 통계 초기화"""
        self.cache_hits = 0
        self.cache_misses = 0
        self.load_times.clear()
        self.timeout_count = 0
        self.error_count = 0
        self.permanent_failure_count = 0
        self.retry_count = 0
        self.retry_success_count = 0
        self.total_loads = 0
        self.start_time = time.time()
        self.peak_memory_mb = 0.0
        self.memory_warnings = 0
