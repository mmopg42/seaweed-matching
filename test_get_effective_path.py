#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
get_effective_path 함수 단위 테스트
"""
import os
import sys
import tempfile

# 모듈 경로 추가
sys.path.insert(0, r"e:\workspace\prische\seaweed\program\matching_codex")

from path_utils import get_effective_path


def test_get_effective_path():
    """get_effective_path() 함수 테스트"""
    
    print("=" * 60)
    print("get_effective_path() 함수 테스트")
    print("=" * 60)
    
    # 테스트 1: 빈 경로
    print("\n[테스트 1] 빈 경로")
    result = get_effective_path("", True)
    assert result == "", f"실패: 예상='', 실제='{result}'"
    print(f"✅ 통과: get_effective_path('', True) = '{result}'")
    
    # 테스트 2: camera 하위폴더 사용 안 함
    print("\n[테스트 2] camera 하위폴더 사용 안 함")
    base_path = r"C:\data\normal"
    result = get_effective_path(base_path, False)
    assert result == base_path, f"실패: 예상='{base_path}', 실제='{result}'"
    print(f"✅ 통과: get_effective_path('{base_path}', False) = '{result}'")
    
    # 테스트 3: camera 하위폴더 사용 (폴더 없음)
    print("\n[테스트 3] camera 하위폴더 사용 (폴더 없음)")
    with tempfile.TemporaryDirectory() as tmpdir:
        result = get_effective_path(tmpdir, True)
        # camera 폴더가 없으면 base_path 반환
        assert result == tmpdir, f"실패: 예상='{tmpdir}', 실제='{result}'"
        print(f"✅ 통과: camera 폴더 없음 → base_path 반환")
    
    # 테스트 4: camera 하위폴더 사용 (폴더 있음)
    print("\n[테스트 4] camera 하위폴더 사용 (폴더 있음)")
    with tempfile.TemporaryDirectory() as tmpdir:
        camera_path = os.path.join(tmpdir, "camera")
        os.makedirs(camera_path)
        
        result = get_effective_path(tmpdir, True)
        assert result == camera_path, f"실패: 예상='{camera_path}', 실제='{result}'"
        print(f"✅ 통과: camera 폴더 있음 → camera 경로 반환")
    
    print("\n" + "=" * 60)
    print("✅ 모든 테스트 통과!")
    print("=" * 60)


if __name__ == "__main__":
    try:
        test_get_effective_path()
        sys.exit(0)
    except Exception as e:
        print(f"\n❌ 테스트 실패: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
