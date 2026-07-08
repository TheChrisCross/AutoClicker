# AutoClicker

A lightweight auto-clicker for Windows with a dark-themed UI, global hotkeys, and a draggable on-screen target marker. Single C# source file, no dependencies beyond the .NET Framework that ships with Windows.

![Windows](https://img.shields.io/badge/platform-Windows-blue) ![.NET Framework 4.0+](https://img.shields.io/badge/.NET%20Framework-4.0%2B-purple)

## Features

- **Global hotkeys** — work even when the window isn't focused:
  - `F6` — start / stop clicking
  - `F7` — capture the current mouse position as the click target
- **Configurable click interval** with optional random variability (ms) to make timing less uniform
- **Left / Right / Middle** mouse button support
- **Click target options** — click at fixed X/Y coordinates, or use a draggable crosshair marker to place the target visually
- **Always-on-top target marker** rendered as a transparent overlay

## Usage

1. Run `AutoClicker.exe`
2. Set the delay between clicks and (optionally) random variability
3. Pick a mouse button and a target position — type coordinates, press `F7` at the mouse location you want, or drag the target marker
4. Press `F6` (or the Start button) to begin; `F6` again to stop

## Building

Requires only Windows — the build script uses the C# compiler bundled with the .NET Framework (no Visual Studio or SDK install needed).

```powershell
.\Build.ps1
```

This regenerates the icon (`GenerateIcon.ps1`) and compiles `Program.cs` to `AutoClicker.exe` on your Desktop.

## Notes

- Windows only (uses `user32.dll` — `RegisterHotKey`, `SetCursorPos`, `mouse_event`)
- No installation, no configuration files, no network access

## Support

If this tool saves you some clicks, you can support development here:

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-support-yellow?logo=buymeacoffee&logoColor=white)](https://buymeacoffee.com/TheChrisCross)
