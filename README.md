# AutoClicker

A lightweight precision auto-clicker for Windows with a polished dark interface, global hotkeys, finite runs, live feedback, and a draggable target marker. It remains a single-file C# application with no runtime dependencies beyond the .NET Framework included with Windows.

![Windows](https://img.shields.io/badge/platform-Windows-blue) ![.NET Framework 4.0+](https://img.shields.io/badge/.NET%20Framework-4.0%2B-purple) [![Latest release](https://img.shields.io/github/v/release/TheChrisCross/AutoClicker)](https://github.com/TheChrisCross/AutoClicker/releases/latest)

## Download

**[Download AutoClicker.exe](https://github.com/TheChrisCross/AutoClicker/releases/latest/download/AutoClicker.exe)** from the latest public release, or build the current source locally using the instructions below.

> Windows SmartScreen may warn on first run because the executable is unsigned. You can choose **More info > Run anyway**, or build it from source.

## Highlights

- **Clear run state** with a prominent Start / Stop control, live status, and click counter
- **Global hotkeys** that work when the window is not focused:
  - `F6` starts or stops clicking
  - `F7` immediately captures the current mouse position
- **Fast interval presets** for 100 ms, 500 ms, 1 second, and 5 seconds
- **Configurable timing** from 10 ms to 10 minutes with optional random variance
- **Single or double click** support
- **Finite runs** using Repeat count, or `0` to run until stopped
- **Left, right, and middle mouse buttons**
- **Flexible target selection**:
  - Enter X and Y coordinates
  - Use the two-second capture button
  - Press `F7` for immediate capture
  - Drag the always-on-top target marker
- **Escape emergency stop** while the AutoClicker window is focused
- **Always-on-top option** for the main window
- **Hotkey conflict warnings** when F6 or F7 is already in use
- **Multi-monitor coordinate support** based on the current Windows virtual desktop
- **Professional multi-resolution icon** with 16, 20, 24, 32, 40, 48, 64, 128, and 256 px resources

## Usage

1. Run `AutoClicker.exe`.
2. Set the click interval and optional random variance.
3. Choose single or double click, mouse button, and repeat count.
4. Choose a target:
   - Press `F7` while the pointer is over the target.
   - Click **Capture position in 2 seconds**, then move the pointer.
   - Enter X and Y coordinates.
   - Show and drag the target marker.
5. Press `F6` or click **Start clicking**.
6. Press `F6`, click **Stop clicking**, or press `Escape` in the focused app window to stop.

Settings are locked while a run is active so each run uses a stable configuration. The target marker hides automatically before clicking begins.

## Building

The build uses the C# compiler bundled with the .NET Framework. Visual Studio and the modern .NET SDK are not required.

```powershell
.\Build.ps1
```

The script:

1. Regenerates the multi-resolution icon.
2. Compiles an optimized x64 executable into `dist\AutoClicker.exe`.
3. Copies the verified build to `OneDrive\Desktop\AutoClicker.exe` on this development machine.

To build only the local `dist` artifact:

```powershell
.\Build.ps1 -SkipDesktopCopy
```

## Technical notes

- Windows only
- Uses `RegisterHotKey`, `SetCursorPos`, and `SendInput` from `user32.dll`
- Uses a cancellation event for fast stops without blocking the UI thread
- Snapshots settings before each run to avoid cross-thread control access
- No installation, accounts, telemetry, or network access

## Support

If this tool saves you some clicks, you can support development here:

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-support-yellow?logo=buymeacoffee&logoColor=white)](https://buymeacoffee.com/TheChrisCross)
