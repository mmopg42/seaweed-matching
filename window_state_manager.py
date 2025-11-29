# window_state_manager.py

from PySide6.QtCore import QByteArray


class WindowStateManager:
    """
    윈도우 상태(위치, 크기) 저장 및 복원을 관리하는 클래스
    """
    
    def save_window_bounds(self, window, config_manager):
        """
        윈도우 위치/크기 저장
        
        Args:
            window: QMainWindow 객체
            config_manager: ConfigManager 인스턴스
        """
        settings = config_manager.load()
        
        # QByteArray로 geometry 저장 (DPI 스케일링 고려)
        geo_ba = window.saveGeometry()
        geo_hex = geo_ba.toHex().data().decode("ascii")
        
        # geometry와 함께 x, y, w, h도 저장 (fallback용)
        geom = window.geometry()
        
        window_settings = settings.get("window", {})
        window_settings["geometry"] = geo_hex
        window_settings["x"] = geom.x()
        window_settings["y"] = geom.y()
        window_settings["w"] = geom.width()
        window_settings["h"] = geom.height()
        
        settings["window"] = window_settings
        config_manager.save(settings)
    
    def restore_window_bounds(self, window, config_manager):
        """
        윈도우 위치/크기 복원
        
        Args:
            window: QMainWindow 객체
            config_manager: ConfigManager 인스턴스
        """
        settings = config_manager.load()
        win = settings.get("window", {})
        geo_hex = win.get("geometry")
        restored = False
        
        if geo_hex:
            try:
                ba = QByteArray.fromHex(geo_hex.encode("ascii"))
                restored = window.restoreGeometry(ba)  # 성공 여부 리턴
            except Exception:
                restored = False
        
        # restoreGeometry 실패했을 때만 x,y,w,h 사용 (fallback)
        if not restored:
            x, y, w, h = (win.get("x"), win.get("y"), win.get("w"), win.get("h"))
            if all(v is not None for v in (x, y, w, h)):
                window.setGeometry(int(x), int(y), int(w), int(h))
