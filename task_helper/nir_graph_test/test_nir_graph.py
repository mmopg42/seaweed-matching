"""
NIR Graph Visualization Test Script
====================================
테스트 목적: NIR 스펙트럼 데이터를 그래프로 시각화하고 PNG로 저장
요구사항: requirements.md 및 Implementation Plan.md 기반
"""

import matplotlib.pyplot as plt
import numpy as np
import os

# 파일 경로
INPUT_FILE = r"C:\workspace\seaweed\data\20251204\2021_A014\with NIR\Nir\run_120251204T111028A.txt"
OUTPUT_DIR = r"C:\workspace\seaweed\gui_kiro\task_helper\nir_graph_test"
OUTPUT_FILE = os.path.join(OUTPUT_DIR, "nir_spectrum_graph.png")


def parse_nir_file(file_path):
    """
    NIR 스펙트럼 파일 파싱

    형식:
    - 헤더 라인: # 또는 한글로 시작 (스킵)
    - 데이터 라인: <wavelength> <intensity> (공백 구분)

    Returns:
        wavelengths (list): 파장 데이터
        intensities (list): 강도 데이터
    """
    wavelengths = []
    intensities = []

    with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
        for line in f:
            line = line.strip()

            # 헤더 라인 스킵 (# 또는 비숫자로 시작)
            if not line or line.startswith('#'):
                continue

            # 데이터 파싱 시도
            try:
                parts = line.split()
                if len(parts) >= 2:
                    wavelength = float(parts[0])
                    intensity = float(parts[1])
                    wavelengths.append(wavelength)
                    intensities.append(intensity)
            except (ValueError, IndexError):
                # 파싱 실패 시 스킵 (헤더 또는 잘못된 데이터)
                continue

    return wavelengths, intensities


def generate_thumbnail_graph(wavelengths, intensities, output_path, width=200, height=150):
    """
    썸네일 그래프 생성 (요구사항: 200x150 픽셀)

    스타일:
    - Minimalist (Thin line, high contrast)
    - No grid/axis labels (cleaner thumbnail)
    """
    dpi = 100
    figsize = (width / dpi, height / dpi)

    fig, ax = plt.subplots(figsize=figsize, dpi=dpi)

    # 그래프 그리기 (파란색 선, 얇은 선)
    ax.plot(wavelengths, intensities, color='#0078d4', linewidth=0.8)

    # Minimalist 스타일: 축 레이블/그리드 제거
    ax.set_xticks([])
    ax.set_yticks([])
    ax.spines['top'].set_visible(False)
    ax.spines['right'].set_visible(False)
    ax.spines['left'].set_visible(False)
    ax.spines['bottom'].set_visible(False)

    # 배경색 (밝은 회색)
    ax.set_facecolor('#f5f5f5')
    fig.patch.set_facecolor('#f5f5f5')

    # 여백 최소화
    plt.tight_layout(pad=0.1)

    # PNG 저장
    plt.savefig(output_path, dpi=dpi, bbox_inches='tight', pad_inches=0.05)
    plt.close()

    print(f"✅ Thumbnail graph saved: {output_path}")


def generate_detailed_graph(wavelengths, intensities, output_path, width=800, height=600):
    """
    상세 그래프 생성 (비교용)

    스타일:
    - 축 레이블 포함
    - 그리드 표시
    - 더 큰 해상도
    """
    dpi = 100
    figsize = (width / dpi, height / dpi)

    fig, ax = plt.subplots(figsize=figsize, dpi=dpi)

    # 그래프 그리기
    ax.plot(wavelengths, intensities, color='#0078d4', linewidth=1.5, label='NIR Spectrum')

    # 축 레이블
    ax.set_xlabel('Wavelength (cm⁻¹)', fontsize=12)
    ax.set_ylabel('Intensity', fontsize=12)
    ax.set_title('NIR Spectrum Visualization', fontsize=14, fontweight='bold')

    # 그리드
    ax.grid(True, alpha=0.3, linestyle='--')

    # 범례
    ax.legend(loc='upper right')

    # 배경색
    ax.set_facecolor('#ffffff')
    fig.patch.set_facecolor('#ffffff')

    # 여백
    plt.tight_layout()

    # PNG 저장
    plt.savefig(output_path, dpi=dpi, bbox_inches='tight')
    plt.close()

    print(f"✅ Detailed graph saved: {output_path}")


def main():
    print("=" * 60)
    print("NIR Graph Visualization Test")
    print("=" * 60)

    # 1. 파일 존재 확인
    if not os.path.exists(INPUT_FILE):
        print(f"❌ Error: File not found: {INPUT_FILE}")
        return

    print(f"📁 Input file: {INPUT_FILE}")

    # 2. 데이터 파싱
    print("\n📊 Parsing NIR data...")
    wavelengths, intensities = parse_nir_file(INPUT_FILE)

    print(f"   - Data points: {len(wavelengths)}")
    print(f"   - Wavelength range: {min(wavelengths):.2f} - {max(wavelengths):.2f} cm⁻¹")
    print(f"   - Intensity range: {min(intensities):.6f} - {max(intensities):.6f}")

    # 3. 유효성 검증 (최소 10개 데이터 포인트)
    if len(wavelengths) < 10:
        print("❌ Error: Insufficient data points (< 10)")
        return

    print("✅ Data validation passed")

    # 4. 썸네일 그래프 생성 (200x150 픽셀)
    print("\n🎨 Generating thumbnail graph (200x150 px)...")
    thumbnail_path = os.path.join(OUTPUT_DIR, "nir_thumbnail_200x150.png")
    generate_thumbnail_graph(wavelengths, intensities, thumbnail_path, width=200, height=150)

    # 5. 상세 그래프 생성 (비교용, 800x600 픽셀)
    print("\n🎨 Generating detailed graph (800x600 px)...")
    detailed_path = os.path.join(OUTPUT_DIR, "nir_detailed_800x600.png")
    generate_detailed_graph(wavelengths, intensities, detailed_path, width=800, height=600)

    # 6. 통계 정보
    print("\n" + "=" * 60)
    print("📈 Statistics:")
    print(f"   - Total data points: {len(wavelengths)}")
    print(f"   - Wavelength (X-axis):")
    print(f"     - Min: {min(wavelengths):.2f} cm⁻¹")
    print(f"     - Max: {max(wavelengths):.2f} cm⁻¹")
    print(f"     - Range: {max(wavelengths) - min(wavelengths):.2f} cm⁻¹")
    print(f"   - Intensity (Y-axis):")
    print(f"     - Min: {min(intensities):.6f}")
    print(f"     - Max: {max(intensities):.6f}")
    print(f"     - Mean: {np.mean(intensities):.6f}")
    print(f"     - Std Dev: {np.std(intensities):.6f}")

    print("\n" + "=" * 60)
    print("✅ Test completed successfully!")
    print(f"📁 Output directory: {OUTPUT_DIR}")
    print("=" * 60)


if __name__ == "__main__":
    main()
