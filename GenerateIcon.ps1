$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$iconPath = Join-Path $projectDir "AutoClicker.ico"

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class NativeIcon {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);
}
'@

$size = 64
$bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

try {
    $rect = New-Object System.Drawing.RectangleF 5, 5, 54, 54
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = 13
    $diameter = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    $bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(255, 30, 31, 36)), ([System.Drawing.Color]::FromArgb(255, 8, 9, 11)), 45
    $border = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 80, 84, 96)), 1.4
    $graphics.FillPath($bg, $path)
    $graphics.DrawPath($border, $path)

    $red = [System.Drawing.Color]::FromArgb(255, 230, 48, 58)
    $redSoft = [System.Drawing.Color]::FromArgb(145, 230, 48, 58)
    $shadowPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(130, 0, 0, 0)), 4
    $crossPen = New-Object System.Drawing.Pen $red, 2.2
    $circlePen = New-Object System.Drawing.Pen $redSoft, 1.6

    $graphics.DrawEllipse($circlePen, 17, 17, 30, 30)
    $graphics.DrawLine($shadowPen, 32, 12, 32, 52)
    $graphics.DrawLine($shadowPen, 12, 32, 52, 32)
    $graphics.DrawLine($crossPen, 32, 12, 32, 52)
    $graphics.DrawLine($crossPen, 12, 32, 52, 32)

    $dot = New-Object System.Drawing.SolidBrush $red
    $graphics.FillEllipse($dot, 28.5, 28.5, 7, 7)

    $cursorPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $cursorPath.AddPolygon(@(
        (New-Object System.Drawing.PointF 38, 35),
        (New-Object System.Drawing.PointF 51, 56),
        (New-Object System.Drawing.PointF 44, 54),
        (New-Object System.Drawing.PointF 41, 62),
        (New-Object System.Drawing.PointF 36, 60),
        (New-Object System.Drawing.PointF 39, 52),
        (New-Object System.Drawing.PointF 32, 57)
    ))

    $cursorShadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(135, 0, 0, 0))
    $cursorBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 240, 241, 245))
    $cursorBorder = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 35, 36, 42)), 1.2
    $graphics.TranslateTransform(1.3, 1.8)
    $graphics.FillPath($cursorShadow, $cursorPath)
    $graphics.ResetTransform()
    $graphics.FillPath($cursorBrush, $cursorPath)
    $graphics.DrawPath($cursorBorder, $cursorPath)

    $handle = $bitmap.GetHicon()
    try {
        $icon = [System.Drawing.Icon]::FromHandle($handle)
        $stream = [System.IO.File]::Create($iconPath)
        try {
            $icon.Save($stream)
        }
        finally {
            $stream.Dispose()
            $icon.Dispose()
        }
    }
    finally {
        [NativeIcon]::DestroyIcon($handle) | Out-Null
    }
}
finally {
    if ($graphics) { $graphics.Dispose() }
    if ($bitmap) { $bitmap.Dispose() }
    if ($path) { $path.Dispose() }
    if ($bg) { $bg.Dispose() }
    if ($border) { $border.Dispose() }
    if ($shadowPen) { $shadowPen.Dispose() }
    if ($crossPen) { $crossPen.Dispose() }
    if ($circlePen) { $circlePen.Dispose() }
    if ($dot) { $dot.Dispose() }
    if ($cursorPath) { $cursorPath.Dispose() }
    if ($cursorShadow) { $cursorShadow.Dispose() }
    if ($cursorBrush) { $cursorBrush.Dispose() }
    if ($cursorBorder) { $cursorBorder.Dispose() }
}

Write-Host "Generated $iconPath"
