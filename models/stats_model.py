"""
통계 데이터를 관리하는 모델
QML과 Python 간 실시간 통계 바인딩
"""

from PySide6.QtCore import QObject, Signal, Property, Slot


class StatsModel(QObject):
    """
    파일 통계 정보를 관리하는 모델
    각 카메라 타입별 파일 개수를 추적
    """

    # 변경 시그널
    normalCountChanged = Signal()
    nirCountChanged = Signal()
    cam1CountChanged = Signal()
    cam2CountChanged = Signal()
    cam3CountChanged = Signal()
    normal2CountChanged = Signal()
    nir2CountChanged = Signal()
    cam4CountChanged = Signal()
    cam5CountChanged = Signal()
    cam6CountChanged = Signal()
    totalGroupsChanged = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._normal_count = 0
        self._nir_count = 0
        self._cam1_count = 0
        self._cam2_count = 0
        self._cam3_count = 0
        self._normal2_count = 0
        self._nir2_count = 0
        self._cam4_count = 0
        self._cam5_count = 0
        self._cam6_count = 0
        self._total_groups = 0

    # ===== Line 1 Properties =====

    @Property(int, notify=normalCountChanged)
    def normalCount(self) -> int:
        return self._normal_count

    @normalCount.setter
    def normalCount(self, value: int):
        if self._normal_count != value:
            self._normal_count = value
            self.normalCountChanged.emit()

    @Property(int, notify=nirCountChanged)
    def nirCount(self) -> int:
        return self._nir_count

    @nirCount.setter
    def nirCount(self, value: int):
        if self._nir_count != value:
            self._nir_count = value
            self.nirCountChanged.emit()

    @Property(int, notify=cam1CountChanged)
    def cam1Count(self) -> int:
        return self._cam1_count

    @cam1Count.setter
    def cam1Count(self, value: int):
        if self._cam1_count != value:
            self._cam1_count = value
            self.cam1CountChanged.emit()

    @Property(int, notify=cam2CountChanged)
    def cam2Count(self) -> int:
        return self._cam2_count

    @cam2Count.setter
    def cam2Count(self, value: int):
        if self._cam2_count != value:
            self._cam2_count = value
            self.cam2CountChanged.emit()

    @Property(int, notify=cam3CountChanged)
    def cam3Count(self) -> int:
        return self._cam3_count

    @cam3Count.setter
    def cam3Count(self, value: int):
        if self._cam3_count != value:
            self._cam3_count = value
            self.cam3CountChanged.emit()

    # ===== Line 2 Properties =====

    @Property(int, notify=normal2CountChanged)
    def normal2Count(self) -> int:
        return self._normal2_count

    @normal2Count.setter
    def normal2Count(self, value: int):
        if self._normal2_count != value:
            self._normal2_count = value
            self.normal2CountChanged.emit()

    @Property(int, notify=nir2CountChanged)
    def nir2Count(self) -> int:
        return self._nir2_count

    @nir2Count.setter
    def nir2Count(self, value: int):
        if self._nir2_count != value:
            self._nir2_count = value
            self.nir2CountChanged.emit()

    @Property(int, notify=cam4CountChanged)
    def cam4Count(self) -> int:
        return self._cam4_count

    @cam4Count.setter
    def cam4Count(self, value: int):
        if self._cam4_count != value:
            self._cam4_count = value
            self.cam4CountChanged.emit()

    @Property(int, notify=cam5CountChanged)
    def cam5Count(self) -> int:
        return self._cam5_count

    @cam5Count.setter
    def cam5Count(self, value: int):
        if self._cam5_count != value:
            self._cam5_count = value
            self.cam5CountChanged.emit()

    @Property(int, notify=cam6CountChanged)
    def cam6Count(self) -> int:
        return self._cam6_count

    @cam6Count.setter
    def cam6Count(self, value: int):
        if self._cam6_count != value:
            self._cam6_count = value
            self.cam6CountChanged.emit()

    # ===== Total Groups =====

    @Property(int, notify=totalGroupsChanged)
    def totalGroups(self) -> int:
        return self._total_groups

    @totalGroups.setter
    def totalGroups(self, value: int):
        if self._total_groups != value:
            self._total_groups = value
            self.totalGroupsChanged.emit()

    # ===== Utility Slots =====

    @Slot(dict)
    def updateStats(self, stats: dict):
        """
        전체 통계 한번에 업데이트
        stats: {
            'normal': int,
            'nir': int,
            'cam1': int,
            ...
        }
        """
        self.normalCount = stats.get('normal', 0)
        self.nirCount = stats.get('nir', 0)
        self.cam1Count = stats.get('cam1', 0)
        self.cam2Count = stats.get('cam2', 0)
        self.cam3Count = stats.get('cam3', 0)
        self.normal2Count = stats.get('normal2', 0)
        self.nir2Count = stats.get('nir2', 0)
        self.cam4Count = stats.get('cam4', 0)
        self.cam5Count = stats.get('cam5', 0)
        self.cam6Count = stats.get('cam6', 0)
        self.totalGroups = stats.get('total_groups', 0)

    @Slot()
    def reset(self):
        """모든 카운터 초기화"""
        self.normalCount = 0
        self.nirCount = 0
        self.cam1Count = 0
        self.cam2Count = 0
        self.cam3Count = 0
        self.normal2Count = 0
        self.nir2Count = 0
        self.cam4Count = 0
        self.cam5Count = 0
        self.cam6Count = 0
        self.totalGroups = 0

    @Slot(result=dict)
    def getStats(self) -> dict:
        """현재 통계 딕셔너리로 반환"""
        return {
            'normal': self._normal_count,
            'nir': self._nir_count,
            'cam1': self._cam1_count,
            'cam2': self._cam2_count,
            'cam3': self._cam3_count,
            'normal2': self._normal2_count,
            'nir2': self._nir2_count,
            'cam4': self._cam4_count,
            'cam5': self._cam5_count,
            'cam6': self._cam6_count,
            'total_groups': self._total_groups,
        }

    @Property(bool, notify=normalCountChanged)
    def hasLine1Data(self) -> bool:
        """라인1 데이터 존재 여부"""
        return (self._normal_count > 0 or self._nir_count > 0 or
                self._cam1_count > 0 or self._cam2_count > 0 or self._cam3_count > 0)

    @Property(bool, notify=normal2CountChanged)
    def hasLine2Data(self) -> bool:
        """라인2 데이터 존재 여부"""
        return (self._normal2_count > 0 or self._nir2_count > 0 or
                self._cam4_count > 0 or self._cam5_count > 0 or self._cam6_count > 0)
