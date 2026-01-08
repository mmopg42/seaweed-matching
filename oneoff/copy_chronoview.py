#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ChronoView 프로젝트 복사 스크립트

C:\workspace\seaweed\gui_kiro_v2\ChronoView를 
C:\workspace\seaweed\gui_C2\ChronoView로 복사합니다.

프로그램 실행에 필수적인 소스 코드, 리소스, 프로젝트 파일만 복사하고
빌드 출력물은 제외합니다. 원본 파일은 그대로 유지됩니다.
"""

import os
import shutil
import sys
from pathlib import Path
from typing import Set, List, Tuple

# 경로 설정
SOURCE_DIR = Path(r"C:\workspace\seaweed\gui_kiro_v2\ChronoView")
TARGET_BASE_DIR = Path(r"C:\workspace\seaweed\gui_C2")
TARGET_DIR = TARGET_BASE_DIR / "ChronoView"

# 복사할 파일 확장자
ALLOWED_EXTENSIONS = {'.cs', '.xaml', '.csproj', '.resx', '.Designer.cs', '.png', '.md'}

# 제외할 디렉토리/파일 패턴
EXCLUDED_DIRS = {'bin', 'obj', '.vs'}
EXCLUDED_FILE_PATTERNS = ['_wpftmp.csproj', 'build_errors.txt', 'build_log.txt']

# 필수 파일 목록 (검증용)
REQUIRED_FILES = [
    'ChronoView.csproj',
    'App.xaml',
    'App.xaml.cs',
    'MainWindow.xaml',
    'MainWindow.xaml.cs'
]


def should_copy_file(file_path: Path) -> bool:
    """파일이 복사 대상인지 확인"""
    # 확장자 확인
    if file_path.suffix not in ALLOWED_EXTENSIONS:
        # .Designer.cs는 특별 처리
        if not file_path.name.endswith('.Designer.cs'):
            return False
    
    # 제외 패턴 확인
    file_name = file_path.name
    for pattern in EXCLUDED_FILE_PATTERNS:
        if pattern in file_name:
            return False
    
    return True


def should_copy_dir(dir_path: Path) -> bool:
    """디렉토리가 복사 대상인지 확인"""
    return dir_path.name not in EXCLUDED_DIRS


def validate_source_directory() -> Tuple[bool, List[str]]:
    """소스 디렉토리 유효성 검사"""
    errors = []
    
    if not SOURCE_DIR.exists():
        errors.append(f"소스 디렉토리가 존재하지 않습니다: {SOURCE_DIR}")
        return False, errors
    
    if not SOURCE_DIR.is_dir():
        errors.append(f"소스 경로가 디렉토리가 아닙니다: {SOURCE_DIR}")
        return False, errors
    
    # 필수 파일 확인
    for required_file in REQUIRED_FILES:
        file_path = SOURCE_DIR / required_file
        if not file_path.exists():
            errors.append(f"필수 파일이 없습니다: {required_file}")
    
    return len(errors) == 0, errors


def copy_project_files() -> Tuple[int, int, List[str], List[str]]:
    """프로젝트 파일 복사"""
    copied_files = []
    skipped_files = []
    copied_count = 0
    skipped_count = 0
    
    # 소스 디렉토리 순회
    for root, dirs, files in os.walk(SOURCE_DIR):
        root_path = Path(root)
        
        # 제외할 디렉토리 필터링
        dirs[:] = [d for d in dirs if should_copy_dir(root_path / d)]
        
        # 상대 경로 계산
        try:
            rel_path = root_path.relative_to(SOURCE_DIR)
        except ValueError:
            continue
        
        # 대상 디렉토리 생성
        target_root = TARGET_DIR / rel_path
        target_root.mkdir(parents=True, exist_ok=True)
        
        # 파일 복사
        for file_name in files:
            source_file = root_path / file_name
            target_file = target_root / file_name
            
            if should_copy_file(source_file):
                try:
                    shutil.copy2(source_file, target_file)
                    copied_files.append(str(source_file.relative_to(SOURCE_DIR)))
                    copied_count += 1
                except Exception as e:
                    skipped_files.append(f"{source_file.relative_to(SOURCE_DIR)} (오류: {e})")
                    skipped_count += 1
            else:
                skipped_files.append(str(source_file.relative_to(SOURCE_DIR)))
                skipped_count += 1
    
    return copied_count, skipped_count, copied_files, skipped_files


def main():
    """메인 함수"""
    print("=" * 70)
    print("ChronoView 프로젝트 복사 스크립트")
    print("=" * 70)
    print(f"소스: {SOURCE_DIR}")
    print(f"대상: {TARGET_DIR}")
    print()
    
    # 소스 디렉토리 검증
    print("소스 디렉토리 검증 중...")
    is_valid, errors = validate_source_directory()
    if not is_valid:
        print("오류: 소스 디렉토리 검증 실패")
        for error in errors:
            print(f"  - {error}")
        sys.exit(1)
    print("✓ 소스 디렉토리 검증 완료")
    print()
    
    # 대상 디렉토리 확인
    print("대상 디렉토리 확인 중...")
    if TARGET_DIR.exists():
        print(f"경고: 대상 디렉토리가 이미 존재합니다: {TARGET_DIR}")
        response = input("덮어쓰시겠습니까? (y/N): ").strip().lower()
        if response != 'y':
            print("작업이 취소되었습니다.")
            sys.exit(0)
        print("기존 디렉토리를 삭제합니다...")
        shutil.rmtree(TARGET_DIR)
    
    # 대상 기본 디렉토리 생성
    TARGET_BASE_DIR.mkdir(parents=True, exist_ok=True)
    print(f"✓ 대상 디렉토리 준비 완료: {TARGET_BASE_DIR}")
    print()
    
    # 파일 복사
    print("파일 복사 중...")
    copied_count, skipped_count, copied_files, skipped_files = copy_project_files()
    print()
    
    # 결과 출력
    print("=" * 70)
    print("복사 완료")
    print("=" * 70)
    print(f"복사된 파일: {copied_count}개")
    print(f"건너뛴 파일: {skipped_count}개")
    print()
    
    if copied_files:
        print("복사된 파일 목록 (처음 20개):")
        for file_path in copied_files[:20]:
            print(f"  ✓ {file_path}")
        if len(copied_files) > 20:
            print(f"  ... 외 {len(copied_files) - 20}개 파일")
        print()
    
    if skipped_files and len(skipped_files) <= 50:
        print("건너뛴 파일 목록:")
        for file_path in skipped_files[:50]:
            print(f"  - {file_path}")
        if len(skipped_files) > 50:
            print(f"  ... 외 {len(skipped_files) - 50}개 파일")
        print()
    
    print(f"대상 디렉토리: {TARGET_DIR}")
    print("=" * 70)


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n작업이 사용자에 의해 중단되었습니다.")
        sys.exit(1)
    except Exception as e:
        print(f"\n\n오류 발생: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)

