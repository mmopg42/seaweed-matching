---
trigger: always_on
---

AI가 요구사항을 명확하게 이해하고 코드를 작성하거나 구조를 잡을 수 있도록, 정보를 논리적인 섹션(시스템 개요, 구성, 데이터 명세)으로 나누어 정리했습니다. 정규표현식(Regex)을 사용하여 파일 형식을 명확히 정의했습니다.

아래 내용을 복사해서 .md 파일로 저장하거나 AI에게 전달하시면 됩니다.

Project Specification: Monitoring Dashboard System
1. System Overview
The goal of this project is to develop a Monitoring Dashboard Program.

Core Function: The system monitors specific directories for newly generated data.

Action: When new data is detected, the system retrieves the corresponding image or data file and displays it on the dashboard for real-time monitoring.

2. Line Configuration
The system manages two distinct production lines, each consisting of specific sensors and cameras.

Line 1 Components: nir1, normal1, cam1, cam2, cam3

Line 2 Components: nir2, normal2, cam4, cam5, cam6

3. Data Processing & Visualization Rules
A. NIR Sensor (Focus: nir1)
File Generation: Data is generated as a pair of files with specific naming conventions.

SPC File: run_1(\d{8}T\d{6}).spc

TXT File: run_1(\d{8}T\d{6})A.txt

Data Integrity Rule: The .spc and .txt files are a set. Any file operation (Move/Delete) must be performed on both files simultaneously to maintain integrity.

Visualization Logic:

The dashboard does not display the raw files.

The system must parse the data from the .txt file and render a graph on the monitoring view.

B. Normal Camera Sensors (normal1, normal2)
Data Structure: Data is generated as a folder (directory), not a single file.

Directory Naming Convention:

normal1: C\d{6}T\d{6}_0

normal2: C\d{6}T\d{6}_1

Visualization Logic:

Inside the generated folder, there is an image file named stitched_original.png.

The dashboard must load and display this stitched_original.png file.

C. General Cameras (cam1 ~ cam6)
File Format: Bitmap image (.bmp).

Naming Convention: \d{8}_\d{6}_\d{3}.bmp

Visualization Logic:

The images should be displayed on the dashboard in sequential order based on their generation or numeric suffix.