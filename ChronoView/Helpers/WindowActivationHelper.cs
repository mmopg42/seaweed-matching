using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ChronoView.Helpers;

/// <summary>
/// Win32 API를 사용하여 프로세스 창을 활성화하고 최상단으로 가져오는 헬퍼 클래스
/// </summary>
public static class WindowActivationHelper
{
    private const int SW_RESTORE = 9;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    /// <summary>
    /// 지정된 프로세스의 창이 현재 최상단(Foreground)에 있는지 확인
    /// </summary>
    /// <param name="process">확인할 프로세스</param>
    /// <returns>최상단에 있으면 true</returns>
    public static bool IsProcessWindowInForeground(Process process)
    {
        if (process == null || process.HasExited)
            return false;

        IntPtr handle = process.MainWindowHandle;
        if (handle == IntPtr.Zero)
            return false;

        IntPtr foregroundWindow = GetForegroundWindow();
        return handle == foregroundWindow;
    }

    /// <summary>
    /// 지정된 프로세스의 메인 창을 활성화하고 최상단으로 가져옴
    /// </summary>
    /// <param name="process">활성화할 프로세스</param>
    public static void ActivateProcessWindow(Process process)
    {
        if (process == null || process.HasExited)
            return;

        IntPtr handle = process.MainWindowHandle;
        if (handle == IntPtr.Zero)
            return;

        // 1. 최소화 상태면 복원
        if (IsIconic(handle))
        {
            ShowWindow(handle, SW_RESTORE);
        }

        // 2. TOPMOST 트릭: 잠시 최상단으로 올렸다가 해제
        SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

        // 3. 포커스 설정
        SetForegroundWindow(handle);
    }

    /// <summary>
    /// 경로를 기반으로 이미 실행 중인 프로세스를 찾음
    /// </summary>
    /// <param name="programPath">프로그램 실행 파일 경로</param>
    /// <returns>실행 중인 프로세스 또는 null</returns>
    public static Process? FindExistingProcess(string programPath)
    {
        if (string.IsNullOrWhiteSpace(programPath))
            return null;

        string fileName = System.IO.Path.GetFileNameWithoutExtension(programPath);
        Process[] processes = Process.GetProcessesByName(fileName);

        foreach (var p in processes)
        {
            try
            {
                // 경로까지 일치하는지 확인 (권한 문제 발생 가능)
                if (string.Equals(p.MainModule?.FileName, programPath, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            catch (Win32Exception)
            {
                // 권한 부족 시 창 핸들로 대체 판단
                if (p.MainWindowHandle != IntPtr.Zero)
                    return p;
            }
            catch (InvalidOperationException)
            {
                // 프로세스 접근 불가 시
                if (p.MainWindowHandle != IntPtr.Zero)
                    return p;
            }
        }

        return null;
    }
}
