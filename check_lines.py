import os

files_to_check = [
    "MainWindowViewModel.cs",
    "FileGroupViewModel.cs",
    "FileOperationService.cs",
    "MoveService.cs",
    "DeleteService.cs",
    "FileGroupOperator.cs",
    "DashboardViewModel.cs",
    "FileOperationViewModel.cs",
    "SystemControlViewModel.cs",
    "FileGroupMediaLoader.cs"
]

root_dir = r"c:\workspace\seaweed\gui_kiro_v2\ChronoView"

print(f"{'File Name':<30} | {'Lines':<10}")
print("-" * 45)

for root, dirs, files in os.walk(root_dir):
    for file in files:
        if file in files_to_check:
            file_path = os.path.join(root, file)
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    lines = f.readlines()
                    print(f"{file:<30} | {len(lines):<10}")
            except Exception as e:
                print(f"{file:<30} | Error: {e}")
