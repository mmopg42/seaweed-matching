"""
모듈 문서 검증 스크립트

docs/modules/ 문서와 script/ 모듈의 실제 구현이 일치하는지 자동으로 검증
"""

import os
import ast
from pathlib import Path
import re
from datetime import datetime
from typing import Dict, List, Optional


class ModuleDocVerifier:
    """모듈 문서 검증기"""
    
    def __init__(self, script_dir: str, docs_dir: str):
        self.script_dir = Path(script_dir)
        self.docs_dir = Path(docs_dir)
        self.results = []
    
    def extract_module_info(self, py_file: Path) -> Optional[Dict]:
        """Python 파일에서 클래스/함수 추출"""
        try:
            with open(py_file, 'r', encoding='utf-8') as f:
                content = f.read()
                tree = ast.parse(content)
        except Exception as e:
            print(f"[ERROR] 파싱 실패: {py_file.name} - {e}")
            return None
        
        info = {
            'file': str(py_file.relative_to(self.script_dir.parent)),
            'classes': [],
            'functions': [],
            'imports': [],
            'lines': len(content.split('\n'))
        }
        
        # 클래스 및 메서드 추출
        for node in ast.walk(tree):
            if isinstance(node, ast.ClassDef):
                methods = []
                for item in node.body:
                    if isinstance(item, ast.FunctionDef):
                        # public 메서드만 (__ 제외)
                        if not item.name.startswith('__') or item.name in ['__init__']:
                            methods.append(item.name)
                
                info['classes'].append({
                    'name': node.name,
                    'methods': methods,
                    'line': node.lineno
                })
            elif isinstance(node, ast.FunctionDef) and node.col_offset == 0:
                # 모듈 레벨 함수만
                if not node.name.startswith('_'):
                    info['functions'].append({
                        'name': node.name,
                        'line': node.lineno
                    })
        
        # import 추출 (상위 5개만)
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                for alias in node.names:
                    if len(info['imports']) < 5:
                        info['imports'].append(alias.name)
            elif isinstance(node, ast.ImportFrom):
                if node.module and len(info['imports']) < 5:
                    info['imports'].append(node.module)
        
        return info
    
    def extract_doc_info(self, md_file: Path) -> Dict:
        """마크다운 문서에서 정보 추출"""
        try:
            with open(md_file, 'r', encoding='utf-8') as f:
                content = f.read()
        except Exception as e:
            print(f"[ERROR] 문서 읽기 실패: {md_file.name} - {e}")
            return {
                'file_path': None,
                'lines': None,
                'classes': [],
                'functions': []
            }
        
        info = {
            'file_path': None,
            'lines': None,
            'classes': [],
            'functions': []
        }
        
        # 파일 경로 추출
        path_patterns = [
            r'\*\*파일 경로\*\*:\s*`([^`]+)`',
            r'\*\*파일\*\*:\s*`([^`]+)`',
            r'파일:\s*`([^`]+)`',
            r'Location:\s*`([^`]+)`',
        ]
        for pattern in path_patterns:
            match = re.search(pattern, content)
            if match:
                info['file_path'] = match.group(1)
                break
        
        # 라인 수 추출
        lines_patterns = [
            r'(\d+)\s*라인',
            r'(\d+)\s*lines',
            r'총\s*(\d+)줄',
        ]
        for pattern in lines_patterns:
            match = re.search(pattern, content, re.IGNORECASE)
            if match:
                info['lines'] = int(match.group(1))
                break
        
        # 클래스 추출 (여러 패턴)
        class_patterns = [
            r'###?\s+(?:클래스:\s*)?`?([A-Z][a-zA-Z0-9_]+)`?(?:\s|$)',
            r'class\s+([A-Z][a-zA-Z0-9_]+)',
            r'##\s+([A-Z][a-zA-Z0-9_]+)\s*클래스',
        ]
        for pattern in class_patterns:
            matches = re.findall(pattern, content)
            info['classes'].extend(matches)
        
        # 중복 제거
        info['classes'] = list(set(info['classes']))
        
        # 함수 추출
        func_patterns = [
            r'####?\s+`?def\s+(\w+)',
            r'###?\s+`(\w+)\([^)]*\)`',
            r'def\s+(\w+)\(',
        ]
        for pattern in func_patterns:
            matches = re.findall(pattern, content)
            # _로 시작하는 내부 함수 제외
            info['functions'].extend([f for f in matches if not f.startswith('_')])
        
        # 중복 제거
        info['functions'] = list(set(info['functions']))
        
        return info
    
    def verify_module(self, py_file: Path, md_file: Optional[Path]) -> Dict:
        """모듈 파일과 문서 비교"""
        result = {
            'module': py_file.name,
            'path': str(py_file.relative_to(self.script_dir)),
            'doc': md_file.name if md_file and md_file.exists() else None,
            'status': 'OK',
            'issues': []
        }
        
        # 실제 코드 분석
        code_info = self.extract_module_info(py_file)
        if not code_info:
            result['status'] = 'ERROR'
            result['issues'].append('코드 파싱 실패')
            return result
        
        # 문서 없음
        if not md_file or not md_file.exists():
            result['status'] = 'MISSING_DOC'
            result['issues'].append('문서 없음')
            result['code_info'] = code_info
            return result
        
        # 문서 분석
        doc_info = self.extract_doc_info(md_file)
        
        # 경로 검증
        expected_path = str(py_file.relative_to(self.script_dir))
        if doc_info['file_path']:
            # 정규화 (역슬래시 → 슬래시)
            doc_path = doc_info['file_path'].replace('\\', '/')
            expected = expected_path.replace('\\', '/')
            
            if doc_path != expected:
                result['status'] = 'OUTDATED'
                result['issues'].append(
                    f"경로 불일치: 문서({doc_path}) ≠ 실제({expected})"
                )
        
        # 라인 수 검증 (±20% 허용)
        if doc_info['lines'] and code_info['lines'] > 0:
            diff_percent = abs(doc_info['lines'] - code_info['lines']) / code_info['lines'] * 100
            if diff_percent > 20:
                if result['status'] == 'OK':
                    result['status'] = 'OUTDATED'
                result['issues'].append(
                    f"라인 수 차이 {diff_percent:.0f}%: "
                    f"문서({doc_info['lines']}) vs 실제({code_info['lines']})"
                )
        
        # 클래스 검증
        code_classes = [c['name'] for c in code_info['classes']]
        doc_classes = doc_info['classes']
        
        missing_classes = set(code_classes) - set(doc_classes)
        extra_classes = set(doc_classes) - set(code_classes)
        
        if missing_classes:
            if result['status'] == 'OK':
                result['status'] = 'INCOMPLETE'
            result['issues'].append(
                f"문서화되지 않은 클래스: {', '.join(sorted(missing_classes))}"
            )
        
        if extra_classes:
            if result['status'] == 'OK':
                result['status'] = 'OUTDATED'
            result['issues'].append(
                f"더 이상 존재하지 않는 클래스: {', '.join(sorted(extra_classes))}"
            )
        
        # 함수 검증 (모듈 레벨 함수만)
        code_funcs = [f['name'] for f in code_info['functions']]
        doc_funcs = doc_info['functions']
        
        missing_funcs = set(code_funcs) - set(doc_funcs)
        # 함수가 3개 이상 누락되었을 때만 문제로 판단
        if missing_funcs and len(missing_funcs) >= 3:
            if result['status'] == 'OK':
                result['status'] = 'INCOMPLETE'
            result['issues'].append(
                f"문서화되지 않은 함수: {', '.join(sorted(list(missing_funcs)[:5]))}"
            )
        
        # 통계 정보 추가
        result['stats'] = {
            'code_lines': code_info['lines'],
            'code_classes': len(code_classes),
            'code_functions': len(code_funcs),
            'doc_lines': doc_info['lines'],
            'doc_classes': len(doc_classes),
            'doc_functions': len(doc_funcs),
        }
        
        return result
    
    def find_module_mapping(self) -> List[tuple]:
        """모듈 파일과 문서 파일 매핑"""
        mappings = []
        
        # script/ 디렉토리의 모든 .py 파일
        py_files = list(self.script_dir.rglob('*.py'))
        py_files = [
            f for f in py_files 
            if '__pycache__' not in str(f) 
            and '__init__' not in f.name
            and 'test_' not in f.name
        ]
        
        for py_file in py_files:
            # 대응하는 문서 파일 찾기
            module_name = py_file.stem
            md_file = self.docs_dir / f"{module_name}.md"
            
            mappings.append((py_file, md_file))
        
        return mappings
    
    def verify_all(self):
        """모든 모듈 검증"""
        mappings = self.find_module_mapping()
        
        print(f"📋 총 {len(mappings)}개 모듈 검증 시작...")
        print()
        
        for py_file, md_file in mappings:
            result = self.verify_module(py_file, md_file)
            self.results.append(result)
            
            # 진행 상황 출력
            status_emoji = {
                'OK': '✅',
                'OUTDATED': '⚠️',
                'INCOMPLETE': '🔶',
                'MISSING_DOC': '❌',
                'ERROR': '🔴'
            }
            emoji = status_emoji.get(result['status'], '⚪')
            print(f"{emoji} {result['module']:30s} [{result['status']}]")
        
        print()
        print("✅ 검증 완료!")
        return self.results
    
    def generate_report(self) -> str:
        """검증 결과 리포트 생성"""
        lines = []
        lines.append("# 모듈 문서 검증 결과\n")
        lines.append(f"**검증일:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
        lines.append(f"**총 모듈 수:** {len(self.results)}개\n\n")
        
        # 통계
        status_count = {}
        for r in self.results:
            status_count[r['status']] = status_count.get(r['status'], 0) + 1
        
        total = len(self.results)
        lines.append("## 📊 통계\n\n")
        
        status_order = ['OK', 'OUTDATED', 'INCOMPLETE', 'MISSING_DOC', 'ERROR']
        emoji_map = {
            'OK': '✅',
            'OUTDATED': '⚠️',
            'INCOMPLETE': '🔶',
            'MISSING_DOC': '❌',
            'ERROR': '🔴'
        }
        
        for status in status_order:
            count = status_count.get(status, 0)
            percent = (count / total * 100) if total > 0 else 0
            emoji = emoji_map.get(status, '⚪')
            lines.append(f"- {emoji} **{status}**: {count}개 ({percent:.1f}%)\n")
        
        lines.append("\n---\n\n")
        
        # 상세 결과 (문제 있는 것만)
        lines.append("## 📋 상세 결과\n\n")
        lines.append("*문제가 있는 모듈만 표시합니다.*\n\n")
        
        problem_results = [r for r in self.results if r['status'] != 'OK']
        problem_results.sort(key=lambda x: (
            status_order.index(x['status']) if x['status'] in status_order else 99,
            x['module']
        ))
        
        if not problem_results:
            lines.append("🎉 **모든 모듈이 정상입니다!**\n\n")
        else:
            for r in problem_results:
                lines.append(f"### {r['module']}\n\n")
                lines.append(f"- **경로:** `{r['path']}`\n")
                lines.append(f"- **상태:** {r['status']}\n")
                lines.append(f"- **문서:** {r['doc'] or '없음'}\n")
                
                if 'stats' in r:
                    stats = r['stats']
                    lines.append(f"- **통계:**\n")
                    lines.append(f"  - 코드: {stats['code_lines']}줄, {stats['code_classes']}개 클래스, {stats['code_functions']}개 함수\n")
                    if stats['doc_lines']:
                        lines.append(f"  - 문서: {stats['doc_lines']}줄, {stats['doc_classes']}개 클래스, {stats['doc_functions']}개 함수\n")
                
                if r['issues']:
                    lines.append(f"- **문제:**\n")
                    for issue in r['issues']:
                        lines.append(f"  - {issue}\n")
                
                lines.append("\n")
        
        lines.append("---\n\n")
        
        # 요약 및 권장사항
        lines.append("## 🎯 권장 조치\n\n")
        
        if status_count.get('MISSING_DOC', 0) > 0:
            lines.append(f"### 1. 문서 없음 ({status_count['MISSING_DOC']}개)\n\n")
            missing = [r for r in self.results if r['status'] == 'MISSING_DOC']
            for r in missing[:10]:  # 최대 10개만
                lines.append(f"- `{r['module']}` - 신규 문서 작성 필요\n")
            lines.append("\n")
        
        if status_count.get('OUTDATED', 0) > 0:
            lines.append(f"### 2. 업데이트 필요 ({status_count['OUTDATED']}개)\n\n")
            outdated = [r for r in self.results if r['status'] == 'OUTDATED']
            for r in outdated[:10]:
                lines.append(f"- `{r['module']}` - 경로 또는 클래스 정보 업데이트\n")
            lines.append("\n")
        
        if status_count.get('INCOMPLETE', 0) > 0:
            lines.append(f"### 3. 불완전한 문서 ({status_count['INCOMPLETE']}개)\n\n")
            incomplete = [r for r in self.results if r['status'] == 'INCOMPLETE']
            for r in incomplete[:10]:
                lines.append(f"- `{r['module']}` - 누락된 클래스/함수 추가\n")
            lines.append("\n")
        
        return ''.join(lines)
    
    def save_report(self, output_path: str):
        """리포트를 파일로 저장"""
        report = self.generate_report()
        
        output_file = Path(output_path)
        output_file.parent.mkdir(parents=True, exist_ok=True)
        
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(report)
        
        print(f"📄 리포트 저장: {output_file}")
        return output_file


def main():
    """메인 실행 함수"""
    # 경로 설정
    base_dir = Path(__file__).parent.parent
    script_dir = base_dir / 'script'
    docs_dir = base_dir / 'docs' / 'modules'
    output_dir = base_dir / 'docs' / 'task' / 'module_check'
    
    print("=" * 80)
    print("🔍 모듈 문서 검증 스크립트")
    print("=" * 80)
    print()
    print(f"📁 스크립트 디렉토리: {script_dir}")
    print(f"📁 문서 디렉토리: {docs_dir}")
    print(f"📁 출력 디렉토리: {output_dir}")
    print()
    print("=" * 80)
    print()
    
    # 검증 실행
    verifier = ModuleDocVerifier(
        script_dir=str(script_dir),
        docs_dir=str(docs_dir)
    )
    
    results = verifier.verify_all()
    
    # 리포트 저장
    print()
    report_path = output_dir / 'verification_report.md'
    verifier.save_report(str(report_path))
    
    print()
    print("=" * 80)
    print("✅ Phase 0 완료!")
    print("=" * 80)
    
    # 간단한 통계 출력
    status_count = {}
    for r in results:
        status_count[r['status']] = status_count.get(r['status'], 0) + 1
    
    print()
    print("📊 최종 통계:")
    for status, count in sorted(status_count.items()):
        print(f"  - {status}: {count}개")
    print()


if __name__ == '__main__':
    main()


