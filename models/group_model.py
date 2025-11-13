"""
QML용 그룹 데이터 모델
Qt의 MVC 패턴에 따라 QAbstractListModel을 상속
"""

from PySide6.QtCore import (
    QAbstractListModel, QModelIndex, Qt, Signal, Slot, Property, QUrl
)
from typing import List, Dict, Any


class GroupModel(QAbstractListModel):
    """
    그룹 리스트를 관리하는 모델
    각 그룹은 NIR, 일반카메라, cam1-3 이미지 정보를 포함
    """

    # 커스텀 역할 정의
    GroupIdRole = Qt.UserRole + 1
    GroupNumberRole = Qt.UserRole + 2
    TimestampRole = Qt.UserRole + 3
    NirImageRole = Qt.UserRole + 4
    NormalImageRole = Qt.UserRole + 5
    Cam1ImageRole = Qt.UserRole + 6
    Cam2ImageRole = Qt.UserRole + 7
    Cam3ImageRole = Qt.UserRole + 8
    IsCheckedRole = Qt.UserRole + 9
    HasNirRole = Qt.UserRole + 10
    HasNormalRole = Qt.UserRole + 11
    HasCam1Role = Qt.UserRole + 12
    HasCam2Role = Qt.UserRole + 13
    HasCam3Role = Qt.UserRole + 14

    # 시그널 정의
    countChanged = Signal()
    dataUpdated = Signal()

    def __init__(self, parent=None):
        super().__init__(parent)
        self._groups: List[Dict[str, Any]] = []

    def rowCount(self, parent=QModelIndex()) -> int:
        """리스트 아이템 개수 반환"""
        return len(self._groups)

    def data(self, index: QModelIndex, role: int = Qt.DisplayRole) -> Any:
        """지정된 인덱스와 역할에 대한 데이터 반환"""
        if not index.isValid() or not (0 <= index.row() < len(self._groups)):
            return None

        group = self._groups[index.row()]

        if role == self.GroupIdRole:
            return group.get('id', '')
        elif role == self.GroupNumberRole:
            return group.get('group_number', 0)
        elif role == self.TimestampRole:
            return group.get('timestamp', '')
        elif role == self.NirImageRole:
            return group.get('nir_image', '')
        elif role == self.NormalImageRole:
            return group.get('normal_image', '')
        elif role == self.Cam1ImageRole:
            return group.get('cam1_image', '')
        elif role == self.Cam2ImageRole:
            return group.get('cam2_image', '')
        elif role == self.Cam3ImageRole:
            return group.get('cam3_image', '')
        elif role == self.IsCheckedRole:
            return group.get('is_checked', False)
        elif role == self.HasNirRole:
            return bool(group.get('nir_image', ''))
        elif role == self.HasNormalRole:
            return bool(group.get('normal_image', ''))
        elif role == self.HasCam1Role:
            return bool(group.get('cam1_image', ''))
        elif role == self.HasCam2Role:
            return bool(group.get('cam2_image', ''))
        elif role == self.HasCam3Role:
            return bool(group.get('cam3_image', ''))

        return None

    def setData(self, index: QModelIndex, value: Any, role: int = Qt.EditRole) -> bool:
        """데이터 업데이트"""
        if not index.isValid() or not (0 <= index.row() < len(self._groups)):
            return False

        group = self._groups[index.row()]

        if role == self.IsCheckedRole:
            group['is_checked'] = value
            self.dataChanged.emit(index, index, [role])
            return True

        return False

    def roleNames(self) -> Dict[int, bytes]:
        """QML에서 사용할 역할 이름 매핑"""
        return {
            self.GroupIdRole: b'groupId',
            self.GroupNumberRole: b'groupNumber',
            self.TimestampRole: b'timestamp',
            self.NirImageRole: b'nirImage',
            self.NormalImageRole: b'normalImage',
            self.Cam1ImageRole: b'cam1Image',
            self.Cam2ImageRole: b'cam2Image',
            self.Cam3ImageRole: b'cam3Image',
            self.IsCheckedRole: b'isChecked',
            self.HasNirRole: b'hasNir',
            self.HasNormalRole: b'hasNormal',
            self.HasCam1Role: b'hasCam1',
            self.HasCam2Role: b'hasCam2',
            self.HasCam3Role: b'hasCam3',
        }

    @Slot(dict)
    def addGroup(self, group_data: Dict[str, Any]):
        """새 그룹 추가"""
        row = len(self._groups)
        self.beginInsertRows(QModelIndex(), row, row)
        self._groups.append(group_data)
        self.endInsertRows()
        self.countChanged.emit()

    @Slot(int)
    def removeGroup(self, index: int):
        """그룹 제거"""
        if 0 <= index < len(self._groups):
            self.beginRemoveRows(QModelIndex(), index, index)
            self._groups.pop(index)
            self.endRemoveRows()
            self.countChanged.emit()

    @Slot()
    def clear(self):
        """모든 그룹 제거"""
        if self._groups:
            self.beginRemoveRows(QModelIndex(), 0, len(self._groups) - 1)
            self._groups.clear()
            self.endRemoveRows()
            self.countChanged.emit()

    @Slot(list)
    def setGroups(self, groups: List[Dict[str, Any]]):
        """전체 그룹 리스트 설정"""
        self.beginResetModel()
        self._groups = groups
        self.endResetModel()
        self.countChanged.emit()

    @Slot(int, bool)
    def setChecked(self, index: int, checked: bool):
        """특정 그룹의 체크 상태 변경"""
        if 0 <= index < len(self._groups):
            model_index = self.index(index, 0)
            self.setData(model_index, checked, self.IsCheckedRole)

    @Slot()
    def checkAll(self):
        """모든 그룹 체크"""
        for i in range(len(self._groups)):
            self.setChecked(i, True)

    @Slot()
    def uncheckAll(self):
        """모든 그룹 체크 해제"""
        for i in range(len(self._groups)):
            self.setChecked(i, False)

    @Slot(result=list)
    def getCheckedGroups(self) -> List[Dict[str, Any]]:
        """체크된 그룹 리스트 반환"""
        return [group for group in self._groups if group.get('is_checked', False)]

    @Slot(result=int)
    def getCheckedCount(self) -> int:
        """체크된 그룹 개수 반환"""
        return sum(1 for group in self._groups if group.get('is_checked', False))

    @Property(int, notify=countChanged)
    def count(self) -> int:
        """전체 그룹 개수"""
        return len(self._groups)

    @Slot(int, result='QVariant')
    def get(self, index: int) -> Dict[str, Any]:
        """특정 인덱스의 그룹 데이터 반환"""
        if 0 <= index < len(self._groups):
            return self._groups[index]
        return {}
