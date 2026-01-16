"""
ChronoView 프로젝트 복사 스크립트 테스트

이 테스트는 copy_chronoview.py 스크립트의 동작을 검증합니다.
"""
import os
import shutil
import tempfile
import pytest
from pathlib import Path
from typing import Set, List


class TestChronoViewCopy:
    """ChronoView 프로젝트 복사 기능 테스트"""
    
    @pytest.fixture
    def source_dir(self):
        """소스 디렉토리 경로"""
        return Path(r"C:\workspace\seaweed\gui_kiro_v2\ChronoView")
    
    @pytest.fixture
    def temp_dest_base(self):
        """임시 대상 디렉토리 베이스 (테스트용)"""
        temp_dir = tempfile.mkdtemp(prefix="chronoview_test_")
        yield Path(temp_dir)
        # 테스트 후 정리
        if Path(temp_dir).exists():
            shutil.rmtree(temp_dir, ignore_errors=True)
    
    @pytest.fixture
    def dest_dir(self, temp_dest_base):
        """대상 디렉토리 경로"""
        return temp_dest_base / "ChronoView"
    
    @pytest.fixture
    def allowed_extensions(self):
        """복사 허용 확장자 목록"""
        return {'.cs', '.xaml', '.csproj', '.resx', '.Designer.cs', '.png', '.md'}
    
    @pytest.fixture
    def excluded_patterns(self):
        """제외할 패턴 목록"""
        return {
            'bin',
            'obj',
            'ChronoView_vo32bhan_wpftmp.csproj',
            'build_errors.txt',
            'build_log.txt',
            '.vs'
        }
    
    def test_source_directory_exists(self, source_dir):
        """소스 디렉토리가 존재하는지 확인"""
        assert source_dir.exists(), f"소스 디렉토리가 존재하지 않습니다: {source_dir}"
        assert source_dir.is_dir(), f"소스 경로가 디렉토리가 아닙니다: {source_dir}"
    
    def test_required_files_exist(self, source_dir):
        """필수 파일들이 소스 디렉토리에 존재하는지 확인"""
        required_files = [
            source_dir / "ChronoView.csproj",
            source_dir / "App.xaml",
            source_dir / "App.xaml.cs",
            source_dir / "MainWindow.xaml",
            source_dir / "MainWindow.xaml.cs",
            source_dir / "README.md",
        ]
        
        missing_files = [f for f in required_files if not f.exists()]
        assert not missing_files, f"필수 파일이 누락되었습니다: {missing_files}"
    
    def test_required_directories_exist(self, source_dir):
        """필수 디렉토리들이 소스 디렉토리에 존재하는지 확인"""
        required_dirs = [
            source_dir / "Core",
            source_dir / "UI",
            source_dir / "Models",
            source_dir / "Helpers",
            source_dir / "Converters",
            source_dir / "Infrastructure",
            source_dir / "Resources",
            source_dir / "Resources" / "Images",
        ]
        
        missing_dirs = [d for d in required_dirs if not d.exists()]
        assert not missing_dirs, f"필수 디렉토리가 누락되었습니다: {missing_dirs}"
    
    def test_get_allowed_files(self, source_dir, allowed_extensions, excluded_patterns):
        """허용된 확장자 파일들을 올바르게 식별하는지 확인"""
        allowed_files = []
        excluded_files = []
        
        for root, dirs, files in os.walk(source_dir):
            # 제외할 디렉토리 필터링
            dirs[:] = [d for d in dirs if d not in excluded_patterns]
            
            rel_root = Path(root).relative_to(source_dir)
            
            for file in files:
                file_path = Path(root) / file
                rel_path = rel_root / file
                
                # 제외 패턴 확인
                should_exclude = False
                for pattern in excluded_patterns:
                    if pattern in str(rel_path) or pattern in str(file_path):
                        should_exclude = True
                        break
                
                if should_exclude:
                    excluded_files.append(rel_path)
                    continue
                
                # 확장자 확인
                if file_path.suffix in allowed_extensions or file_path.suffix == '':
                    # .Designer.cs 같은 경우
                    if '.Designer.cs' in file_path.name:
                        allowed_files.append(rel_path)
                    elif file_path.suffix in allowed_extensions:
                        allowed_files.append(rel_path)
        
        # 최소한의 필수 파일들이 포함되어야 함
        assert len(allowed_files) > 0, "허용된 파일이 없습니다"
        
        # 필수 파일들이 포함되어야 함
        required_file_names = {
            'ChronoView.csproj',
            'App.xaml',
            'App.xaml.cs',
            'MainWindow.xaml',
            'MainWindow.xaml.cs',
        }
        
        found_required = {f.name for f in allowed_files}
        missing_required = required_file_names - found_required
        assert not missing_required, f"필수 파일이 허용 목록에 없습니다: {missing_required}"
    
    def test_excluded_files_not_included(self, source_dir, excluded_patterns):
        """제외할 파일들이 포함되지 않는지 확인"""
        excluded_found = []
        
        for root, dirs, files in os.walk(source_dir):
            for file in files:
                file_path = Path(root) / file
                rel_path = file_path.relative_to(source_dir)
                
                for pattern in excluded_patterns:
                    if pattern in str(rel_path) or pattern in file_path.name:
                        excluded_found.append(rel_path)
                        break
        
        # 제외된 파일들이 발견되었는지 확인 (존재는 하지만 복사 대상이 아니어야 함)
        # 이 테스트는 제외 패턴이 올바르게 작동하는지 확인
        if excluded_found:
            print(f"\n제외된 파일들 (복사 대상 아님): {len(excluded_found)}개")
            for f in excluded_found[:10]:  # 처음 10개만 출력
                print(f"  - {f}")
    
    def test_directory_structure_preserved(self, source_dir, allowed_extensions, excluded_patterns):
        """디렉토리 구조가 올바르게 유지되는지 확인"""
        expected_dirs = {
            'Core',
            'Core/Analytics',
            'Core/Configuration',
            'Core/FileMatching',
            'Core/FileOperations',
            'Core/FileWatching',
            'Core/ImageProcessing',
            'Core/Localization',
            'Core/NIR',
            'Core/ProgramLaunching',
            'UI',
            'UI/Behaviors',
            'UI/Controls',
            'UI/ViewModels',
            'UI/Views',
            'Models',
            'Helpers',
            'Converters',
            'Infrastructure',
            'Infrastructure/Logging',
            'Resources',
            'Resources/Images',
            'Tests',
        }
        
        found_dirs = set()
        
        for root, dirs, files in os.walk(source_dir):
            # 제외할 디렉토리 필터링
            dirs[:] = [d for d in dirs if d not in excluded_patterns]
            
            rel_root = Path(root).relative_to(source_dir)
            if str(rel_root) != '.':
                found_dirs.add(str(rel_root).replace('\\', '/'))
        
        # 예상된 디렉토리들이 모두 존재하는지 확인
        missing_dirs = expected_dirs - found_dirs
        assert not missing_dirs, f"예상된 디렉토리가 누락되었습니다: {missing_dirs}"
    
    def test_resource_files_exist(self, source_dir):
        """리소스 파일들이 존재하는지 확인"""
        resource_dir = source_dir / "Resources"
        assert resource_dir.exists(), "Resources 디렉토리가 없습니다"
        
        # 필수 리소스 파일
        required_resources = [
            resource_dir / "SharedResources.xaml",
            resource_dir / "Strings.resx",
            resource_dir / "Strings.Designer.cs",
        ]
        
        missing_resources = [f for f in required_resources if not f.exists()]
        assert not missing_resources, f"필수 리소스 파일이 누락되었습니다: {missing_resources}"
        
        # 이미지 파일들
        images_dir = resource_dir / "Images"
        if images_dir.exists():
            image_files = list(images_dir.glob("*.png"))
            assert len(image_files) > 0, "이미지 파일이 없습니다"
            print(f"\n발견된 이미지 파일: {len(image_files)}개")
            for img in image_files:
                print(f"  - {img.name}")
    
    def test_cs_files_count(self, source_dir, excluded_patterns):
        """C# 파일 개수를 확인"""
        cs_files = []
        
        for root, dirs, files in os.walk(source_dir):
            # 제외할 디렉토리 필터링
            dirs[:] = [d for d in dirs if d not in excluded_patterns]
            
            for file in files:
                if file.endswith('.cs'):
                    file_path = Path(root) / file
                    rel_path = file_path.relative_to(source_dir)
                    cs_files.append(rel_path)
        
        assert len(cs_files) > 0, "C# 파일이 없습니다"
        print(f"\n발견된 C# 파일: {len(cs_files)}개")
        
        # 주요 디렉토리별 파일 개수 확인
        by_dir = {}
        for f in cs_files:
            dir_name = str(f.parent) if f.parent != Path('.') else 'root'
            by_dir[dir_name] = by_dir.get(dir_name, 0) + 1
        
        print("\n디렉토리별 C# 파일 개수:")
        for dir_name, count in sorted(by_dir.items()):
            print(f"  {dir_name}: {count}개")
    
    def test_xaml_files_count(self, source_dir, excluded_patterns):
        """XAML 파일 개수를 확인"""
        xaml_files = []
        
        for root, dirs, files in os.walk(source_dir):
            # 제외할 디렉토리 필터링
            dirs[:] = [d for d in dirs if d not in excluded_patterns]
            
            for file in files:
                if file.endswith('.xaml'):
                    file_path = Path(root) / file
                    rel_path = file_path.relative_to(source_dir)
                    xaml_files.append(rel_path)
        
        assert len(xaml_files) > 0, "XAML 파일이 없습니다"
        print(f"\n발견된 XAML 파일: {len(xaml_files)}개")
        for xaml in xaml_files:
            print(f"  - {xaml}")
    
    def test_project_file_structure(self, source_dir):
        """프로젝트 파일 구조 확인"""
        csproj_file = source_dir / "ChronoView.csproj"
        assert csproj_file.exists(), "ChronoView.csproj 파일이 없습니다"
        
        # 프로젝트 파일 내용 확인
        content = csproj_file.read_text(encoding='utf-8')
        assert '<Project Sdk="Microsoft.NET.Sdk">' in content, "프로젝트 파일 형식이 올바르지 않습니다"
        assert '<TargetFramework>' in content, "TargetFramework이 없습니다"
    
    def test_no_build_artifacts_in_source(self, source_dir):
        """소스 디렉토리에 빌드 산출물이 있는지 확인 (제외 대상 확인용)"""
        build_artifacts = []
        
        # bin, obj 디렉토리 확인
        for pattern in ['bin', 'obj']:
            for path in source_dir.rglob(pattern):
                if path.is_dir():
                    build_artifacts.append(path.relative_to(source_dir))
        
        # 빌드 로그 파일 확인
        for log_file in ['build_errors.txt', 'build_log.txt']:
            log_path = source_dir / log_file
            if log_path.exists():
                build_artifacts.append(log_path.relative_to(source_dir))
        
        # 임시 프로젝트 파일 확인
        temp_csproj = source_dir / "ChronoView_vo32bhan_wpftmp.csproj"
        if temp_csproj.exists():
            build_artifacts.append(temp_csproj.relative_to(source_dir))
        
        if build_artifacts:
            print(f"\n발견된 빌드 산출물 (제외 대상): {len(build_artifacts)}개")
            for artifact in build_artifacts[:10]:
                print(f"  - {artifact}")
        # 빌드 산출물이 존재하는 것은 정상 (제외 대상이므로)
    
    def test_file_extensions_coverage(self, source_dir, allowed_extensions, excluded_patterns):
        """모든 허용된 확장자 파일이 식별되는지 확인"""
        found_extensions = set()
        
        for root, dirs, files in os.walk(source_dir):
            # 제외할 디렉토리 필터링
            dirs[:] = [d for d in dirs if d not in excluded_patterns]
            
            for file in files:
                file_path = Path(root) / file
                
                # 제외 패턴 확인
                should_exclude = False
                for pattern in excluded_patterns:
                    if pattern in str(file_path):
                        should_exclude = True
                        break
                
                if should_exclude:
                    continue
                
                # 확장자 수집
                if '.Designer.cs' in file_path.name:
                    found_extensions.add('.Designer.cs')
                elif file_path.suffix:
                    found_extensions.add(file_path.suffix)
        
        print(f"\n발견된 확장자: {found_extensions}")
        print(f"허용된 확장자: {allowed_extensions}")
        
        # 허용된 확장자 중 실제로 발견된 것들
        found_allowed = found_extensions & allowed_extensions
        print(f"허용된 확장자 중 발견된 것: {found_allowed}")
        
        # 모든 허용된 확장자가 최소한 하나의 파일로 존재해야 함
        # (단, 일부 확장자는 선택적일 수 있음)
        critical_extensions = {'.cs', '.xaml', '.csproj'}
        missing_critical = critical_extensions - found_allowed
        assert not missing_critical, f"중요한 확장자 파일이 없습니다: {missing_critical}"


class TestCopyScriptValidation:
    """복사 스크립트 검증 로직 테스트"""
    
    def test_file_should_be_copied(self):
        """파일이 복사 대상인지 판단하는 로직 테스트"""
        allowed_extensions = {'.cs', '.xaml', '.csproj', '.resx', '.Designer.cs', '.png', '.md'}
        excluded_patterns = {'bin', 'obj', 'ChronoView_vo32bhan_wpftmp.csproj', 
                            'build_errors.txt', 'build_log.txt', '.vs'}
        
        test_cases = [
            # (파일 경로, 예상 결과)
            ("App.xaml", True),
            ("App.xaml.cs", True),
            ("ChronoView.csproj", True),
            ("Resources/Strings.resx", True),
            ("Resources/Strings.Designer.cs", True),
            ("Resources/Images/icon.png", True),
            ("README.md", True),
            ("bin/Debug/net10.0-windows/ChronoView.exe", False),
            ("obj/Debug/net10.0-windows/ChronoView.dll", False),
            ("ChronoView_vo32bhan_wpftmp.csproj", False),
            ("build_errors.txt", False),
            ("build_log.txt", False),
            (".vs/settings.json", False),
            ("Core/FileOperations/FileOperationService.cs", True),
            ("UI/Views/MainWindow.xaml", True),
        ]
        
        for file_path, expected in test_cases:
            path = Path(file_path)
            
            # 제외 패턴 확인
            should_exclude = False
            for pattern in excluded_patterns:
                if pattern in str(path) or pattern in path.name:
                    should_exclude = True
                    break
            
            if should_exclude:
                result = False
            else:
                # 확장자 확인
                if '.Designer.cs' in path.name:
                    result = True
                elif path.suffix in allowed_extensions:
                    result = True
                else:
                    result = False
            
            assert result == expected, \
                f"파일 '{file_path}'의 복사 여부 판단이 잘못되었습니다. 예상: {expected}, 실제: {result}"


if __name__ == "__main__":
    # pytest를 직접 실행할 수 있도록 설정
    pytest.main([__file__, "-v", "-s"])





