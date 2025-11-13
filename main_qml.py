"""
QML 기반 모니터링 앱 메인 진입점
PySide6 + QML로 현대적인 UI 구현
"""

import sys
import os
from pathlib import Path

from PySide6.QtGui import QGuiApplication, QIcon
from PySide6.QtQml import QQmlApplicationEngine
from PySide6.QtCore import QUrl, QObject, Slot, Signal, Property

from models.group_model import GroupModel
from models.stats_model import StatsModel


class AppController(QObject):
    """
    앱 전체 제어를 담당하는 컨트롤러
    QML과 Python 백엔드 간 브릿지
    """

    # 시그널
    monitoringStarted = Signal()
    monitoringStopped = Signal()
    logMessage = Signal(str, str)  # message, level (info/warning/error)

    # 프로퍼티 변경 시그널
    isRunningChanged = Signal()
    workDateChanged = Signal()
    sampleName1Changed = Signal()
    sampleName2Changed = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._is_running = False
        self._work_date = ""
        self._sample_name1 = ""
        self._sample_name2 = ""

    # ===== Properties =====

    @Property(bool, notify=isRunningChanged)
    def isRunning(self) -> bool:
        return self._is_running

    @isRunning.setter
    def isRunning(self, value: bool):
        if self._is_running != value:
            self._is_running = value
            self.isRunningChanged.emit()

    @Property(str, notify=workDateChanged)
    def workDate(self) -> str:
        return self._work_date

    @workDate.setter
    def workDate(self, value: str):
        if self._work_date != value:
            self._work_date = value
            self.workDateChanged.emit()

    @Property(str, notify=sampleName1Changed)
    def sampleName1(self) -> str:
        return self._sample_name1

    @sampleName1.setter
    def sampleName1(self, value: str):
        if self._sample_name1 != value:
            self._sample_name1 = value
            self.sampleName1Changed.emit()

    @Property(str, notify=sampleName2Changed)
    def sampleName2(self) -> str:
        return self._sample_name2

    @sampleName2.setter
    def sampleName2(self, value: str):
        if self._sample_name2 != value:
            self._sample_name2 = value
            self.sampleName2Changed.emit()

    # ===== Slots (QML에서 호출 가능) =====

    @Slot()
    def startMonitoring(self):
        """모니터링 시작"""
        self.isRunning = True
        self.monitoringStarted.emit()
        self.logMessage.emit("모니터링을 시작했습니다.", "info")
        print("✓ 모니터링 시작")

    @Slot()
    def stopMonitoring(self):
        """모니터링 중지"""
        self.isRunning = False
        self.monitoringStopped.emit()
        self.logMessage.emit("모니터링을 중지했습니다.", "warning")
        print("✓ 모니터링 중지")

    @Slot()
    def openSettings(self):
        """설정 다이얼로그 열기"""
        self.logMessage.emit("설정 창을 엽니다.", "info")
        print("✓ 설정 열기")

    @Slot(str)
    def openFolder(self, folder_type: str):
        """폴더 열기"""
        self.logMessage.emit(f"{folder_type} 폴더를 엽니다.", "info")
        print(f"✓ {folder_type} 폴더 열기")

    @Slot()
    def loadImages(self):
        """이미지 불러오기"""
        self.logMessage.emit("이미지를 불러옵니다...", "info")
        print("✓ 이미지 불러오기")

    @Slot()
    def selectAll(self):
        """전체 선택"""
        self.logMessage.emit("전체 선택", "info")
        print("✓ 전체 선택")

    @Slot()
    def deselectAll(self):
        """전체 선택 해제"""
        self.logMessage.emit("전체 선택 해제", "info")
        print("✓ 전체 선택 해제")

    @Slot()
    def deleteSelected(self):
        """선택 항목 삭제"""
        self.logMessage.emit("선택한 항목을 삭제합니다.", "warning")
        print("✓ 선택 항목 삭제")

    @Slot()
    def moveFiles(self):
        """파일 이동"""
        self.logMessage.emit("파일 이동을 시작합니다...", "info")
        print("✓ 파일 이동")

    @Slot(str)
    def createSampleFolder(self, sample_name: str):
        """시료 폴더 생성"""
        self.logMessage.emit(f"시료 폴더 생성: {sample_name}", "info")
        print(f"✓ 시료 폴더 생성: {sample_name}")

    @Slot(str)
    def setAutoPath(self, date: str):
        """경로 자동 설정"""
        self.logMessage.emit(f"경로 자동 설정: {date}", "info")
        print(f"✓ 경로 자동 설정: {date}")


def main():
    """메인 함수"""
    # 앱 생성
    app = QGuiApplication(sys.argv)
    app.setOrganizationName("prische")
    app.setOrganizationDomain("ebt.co.kr")
    app.setApplicationName("이비기술 MMS v2.0")

    # QML 엔진 생성
    engine = QQmlApplicationEngine()

    # 모델 생성
    group_model = GroupModel()
    stats_model = StatsModel()
    app_controller = AppController()

    # QML에 모델 노출
    engine.rootContext().setContextProperty("groupModel", group_model)
    engine.rootContext().setContextProperty("statsModel", stats_model)
    engine.rootContext().setContextProperty("appController", app_controller)

    # QML import path 추가
    qml_dir = Path(__file__).parent / "qml"
    engine.addImportPath(str(qml_dir))

    # 메인 QML 로드
    qml_file = qml_dir / "main.qml"
    engine.load(QUrl.fromLocalFile(str(qml_file)))

    # 로드 실패 체크
    if not engine.rootObjects():
        print("✗ QML 로드 실패!")
        return -1

    # 테스트 데이터 추가
    print("✓ QML 로드 성공!")
    print("✓ 테스트 데이터 추가 중...")

    # 샘플 통계 데이터
    stats_model.updateStats({
        'normal': 142,
        'nir': 138,
        'cam1': 142,
        'cam2': 140,
        'cam3': 141,
        'normal2': 0,
        'nir2': 0,
        'cam4': 0,
        'cam5': 0,
        'cam6': 0,
        'total_groups': 138,
    })

    # 샘플 그룹 데이터
    for i in range(1, 21):
        group_model.addGroup({
            'id': f'group_{i}',
            'group_number': i,
            'timestamp': f'2025-01-13 14:{30 + i}:45',
            'nir_image': f'run_120250113T14{30+i}45.spc' if i % 3 != 0 else '',
            'normal_image': f'C20250113T14{30+i}45_0/image.jpg' if i % 4 != 0 else '',
            'cam1_image': f'cam1_{i}.jpg' if i % 5 != 0 else '',
            'cam2_image': f'cam2_{i}.jpg',
            'cam3_image': f'cam3_{i}.jpg' if i % 6 != 0 else '',
            'is_checked': False,
        })

    print(f"✓ {group_model.count}개 그룹 추가 완료")
    print("=" * 50)
    print("앱 실행 중... (Ctrl+C로 종료)")
    print("=" * 50)

    # 앱 실행
    return app.exec()


if __name__ == "__main__":
    sys.exit(main())
