# Bring Window to Front Issue - Root Cause Analysis v2

## Current Status

- **종료 (Terminate)**: ✅ 정상 동작 (`Process.Kill(true)`)
- **맨 앞으로 (Bring to Front)**: ❌ 동작 안 함

---

## Problem Analysis

### 현재 구현 (`WindowActivationHelper.ActivateProcessWindow`)

```csharp
// 현재 시도 중인 방법들
AttachThreadInput(currentThread, targetThread, true);
ShowWindow(handle, SW_RESTORE);
SetForegroundWindow(handle);
SwitchToThisWindow(handle, true);
AttachThreadInput(currentThread, targetThread, false);
```

### "활성화(Activate)"와 "맨 앞으로(Bring to Front)"의 차이

| 용어 | Win32 개념 | 설명 |
|------|------------|------|
| **Activate** | Foreground Window | 키보드 입력을 받는 창 (Focus) |
| **Bring to Front** | Z-Order Top | 시각적으로 맨 앞에 보이는 창 |

**핵심 문제**: Windows에서 다른 프로세스의 창을 강제로 Foreground로 만드는 것은 **보안상 제한**됨.

---

## 시도된 방법들 (모두 실패)

| 방법 | 결과 | 이유 |
|------|------|------|
| `SetForegroundWindow` | ❌ | Windows Focus Stealing Prevention |
| `AttachThreadInput` + `SetForegroundWindow` | ❌ | 일부 환경에서만 작동 |
| `SwitchToThisWindow` | ❌ | 내부적으로 `SetForegroundWindow` 호출 |
| `ShowWindow(SW_RESTORE)` | ❌ | Z-Order만 변경, Focus 안 줌 |

---

## 근본적 제약

Windows Vista 이후, **다른 프로세스의 창을 강제로 앞으로 가져오는 것은 OS 레벨에서 차단**됩니다.

> "If the window is the foreground window, SetForegroundWindow will always succeed. Otherwise, there are restrictions..."
> — MSDN SetForegroundWindow Documentation

### OS가 `SetForegroundWindow`를 허용하는 조건

1. 호출 프로세스가 현재 Foreground 프로세스임
2. 호출 프로세스가 Foreground 프로세스에 의해 시작됨
3. 호출 프로세스가 마지막 입력 이벤트를 받음
4. Foreground 프로세스가 없음
5. Foreground Timeout이 만료됨
6. `AllowSetForegroundWindow`가 호출됨

**문제**: 우리 앱에서 버튼을 클릭하면 → 우리 앱이 Foreground가 됨 → 외부 프로그램은 Background가 됨 → 외부 프로그램을 Foreground로 만들 권한 없음.

---

## 가능한 해결책

### Option A: Taskbar Flash (현재 동작)
외부 창이 taskbar에서 깜빡이도록 함. 사용자가 직접 클릭해야 함.

**장점**: OS 제한 준수
**단점**: UX 불편

### Option B: SetWindowPos + HWND_TOPMOST
창을 강제로 Z-Order 최상단으로 이동 (Always on Top 트릭)

```csharp
[DllImport("user32.dll")]
static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, 
    int X, int Y, int cx, int cy, uint uFlags);

static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
const uint SWP_NOMOVE = 0x0002;
const uint SWP_NOSIZE = 0x0001;
const uint SWP_SHOWWINDOW = 0x0040;

// 잠시 TOPMOST로 만들었다가 해제
SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
```

**장점**: OS 제한 우회 가능 (Z-Order 변경은 허용됨)
**단점**: Hacky, 일부 앱에서 문제 가능성

### Option C: Minimize ChronoView First
ChronoView를 최소화 → 외부 창이 자동으로 앞으로 옴

```csharp
// ChronoView 최소화
Application.Current.MainWindow.WindowState = WindowState.Minimized;
// 잠시 후 복원 (optional)
```

**장점**: 확실히 동작
**단점**: UX 이상함 (앱이 최소화됨)

---

## 권장 사항

**Option B (SetWindowPos TOPMOST 트릭)** 를 먼저 시도.

이것도 안 되면, **현실적인 UX 타협**:
- "아니오" 클릭 시 → 로그에 "Activated (Taskbar에서 확인)" 표시
- 사용자가 Taskbar에서 직접 클릭하도록 유도
