param(
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $OutputPath) { $OutputPath = Join-Path $projectDir "AutoClicker.ico" }
$previewPath = Join-Path $projectDir "AutoClicker-icon-preview.png"

Add-Type -AssemblyName System.Drawing

function New-RoundedPath([System.Drawing.RectangleF]$rect, [float]$radius) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap([int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    try {
        $margin = [Math]::Max(1.0, $size * 0.055)
        $tile = New-Object System.Drawing.RectangleF $margin, $margin, ($size - 2 * $margin), ($size - 2 * $margin)
        $radius = $size * 0.215
        $tilePath = New-RoundedPath $tile $radius

        $top = [System.Drawing.Color]::FromArgb(255, 32, 41, 50)
        $bottom = [System.Drawing.Color]::FromArgb(255, 11, 15, 19)
        $tileFill = New-Object System.Drawing.Drawing2D.LinearGradientBrush $tile, $top, $bottom, 68
        $graphics.FillPath($tileFill, $tilePath)

        $borderWidth = [Math]::Max(1.0, $size * 0.018)
        $border = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(205, 74, 90, 104)), $borderWidth
        $graphics.DrawPath($border, $tilePath)

        if ($size -ge 32) {
            $inset = $margin + $borderWidth * 1.55
            $innerRect = New-Object System.Drawing.RectangleF $inset, $inset, ($size - 2 * $inset), ($size - 2 * $inset)
            $innerPath = New-RoundedPath $innerRect ([Math]::Max(2.0, $radius - $borderWidth * 1.3))
            $highlight = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(62, 255, 255, 255)), ([Math]::Max(0.7, $size * 0.007))
            $graphics.DrawPath($highlight, $innerPath)
        }

        $accent = [System.Drawing.Color]::FromArgb(255, 73, 207, 193)
        $accentSoft = [System.Drawing.Color]::FromArgb(120, 73, 207, 193)
        $cx = $size * 0.44
        $cy = $size * 0.43
        $ringRadius = $size * 0.205
        $ringWidth = [Math]::Max(1.15, $size * 0.04)
        $ring = New-Object System.Drawing.Pen $accent, $ringWidth
        $ring.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $ring.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $ringRect = New-Object System.Drawing.RectangleF ($cx - $ringRadius), ($cy - $ringRadius), ($ringRadius * 2), ($ringRadius * 2)
        $graphics.DrawArc($ring, $ringRect, 24, 306)

        if ($size -ge 24) {
            $outerRadius = $size * 0.285
            $outer = New-Object System.Drawing.Pen $accentSoft, ([Math]::Max(0.8, $size * 0.014))
            $outerRect = New-Object System.Drawing.RectangleF ($cx - $outerRadius), ($cy - $outerRadius), ($outerRadius * 2), ($outerRadius * 2)
            $graphics.DrawArc($outer, $outerRect, 196, 226)
        }

        $dotRadius = [Math]::Max(1.15, $size * 0.055)
        $dot = New-Object System.Drawing.SolidBrush $accent
        $graphics.FillEllipse($dot, $cx - $dotRadius, $cy - $dotRadius, $dotRadius * 2, $dotRadius * 2)

        $cursor = New-Object System.Drawing.Drawing2D.GraphicsPath
        $cursor.AddPolygon(@(
            (New-Object System.Drawing.PointF ($size * 0.50), ($size * 0.45)),
            (New-Object System.Drawing.PointF ($size * 0.77), ($size * 0.79)),
            (New-Object System.Drawing.PointF ($size * 0.64), ($size * 0.75)),
            (New-Object System.Drawing.PointF ($size * 0.59), ($size * 0.87)),
            (New-Object System.Drawing.PointF ($size * 0.51), ($size * 0.83)),
            (New-Object System.Drawing.PointF ($size * 0.56), ($size * 0.71)),
            (New-Object System.Drawing.PointF ($size * 0.45), ($size * 0.75))
        ))
        $cursor.CloseFigure()

        if ($size -ge 24) {
            $shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(115, 0, 0, 0))
            $graphics.TranslateTransform($size * 0.018, $size * 0.024)
            $graphics.FillPath($shadow, $cursor)
            $graphics.ResetTransform()
        }

        $cursorFill = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 243, 247, 249))
        $cursorEdge = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 12, 17, 21)), ([Math]::Max(0.8, $size * 0.018))
        $cursorEdge.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $graphics.FillPath($cursorFill, $cursor)
        $graphics.DrawPath($cursorEdge, $cursor)

        return $bitmap
    }
    finally {
        $graphics.Dispose()
        if ($tilePath) { $tilePath.Dispose() }
        if ($tileFill) { $tileFill.Dispose() }
        if ($border) { $border.Dispose() }
        if ($innerPath) { $innerPath.Dispose() }
        if ($highlight) { $highlight.Dispose() }
        if ($ring) { $ring.Dispose() }
        if ($outer) { $outer.Dispose() }
        if ($dot) { $dot.Dispose() }
        if ($cursor) { $cursor.Dispose() }
        if ($shadow) { $shadow.Dispose() }
        if ($cursorFill) { $cursorFill.Dispose() }
        if ($cursorEdge) { $cursorEdge.Dispose() }
    }
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$frames = New-Object System.Collections.Generic.List[object]

try {
    foreach ($size in $sizes) {
        $bitmap = New-IconBitmap $size
        try {
            $stream = New-Object System.IO.MemoryStream
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            $frames.Add([PSCustomObject]@{ Size = $size; Bytes = $stream.ToArray() })
            $stream.Dispose()
        }
        finally {
            $bitmap.Dispose()
        }
    }

    $file = [System.IO.File]::Create($OutputPath)
    $writer = New-Object System.IO.BinaryWriter $file
    try {
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$frames.Count)

        $offset = 6 + (16 * $frames.Count)
        foreach ($frame in $frames) {
            $dimension = if ($frame.Size -ge 256) { 0 } else { $frame.Size }
            $writer.Write([Byte]$dimension)
            $writer.Write([Byte]$dimension)
            $writer.Write([Byte]0)
            $writer.Write([Byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$frame.Bytes.Length)
            $writer.Write([UInt32]$offset)
            $offset += $frame.Bytes.Length
        }

        foreach ($frame in $frames) {
            $writer.Write($frame.Bytes)
        }
    }
    finally {
        $writer.Dispose()
        $file.Dispose()
    }

    $preview = New-IconBitmap 512
    try { $preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $preview.Dispose() }
}
finally {
    $frames.Clear()
}

Write-Host "Generated multi-resolution icon: $OutputPath"
Write-Host "Generated preview: $previewPath"
