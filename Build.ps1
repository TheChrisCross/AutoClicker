param(
    [switch]$SkipDesktopCopy
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $projectDir "Program.cs"
$iconPath = Join-Path $projectDir "AutoClicker.ico"
$distDir = Join-Path $projectDir "dist"
$distPath = Join-Path $distDir "AutoClicker.exe"
$tempPath = Join-Path $distDir "AutoClicker.building.exe"
$desktopPath = Join-Path $env:USERPROFILE "OneDrive\Desktop\AutoClicker.exe"
$compilerPath = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $compilerPath)) {
    throw "C# compiler was not found at $compilerPath"
}

New-Item -ItemType Directory -Force -Path $distDir | Out-Null
& (Join-Path $projectDir "GenerateIcon.ps1")

if (Test-Path $tempPath) { Remove-Item -Force $tempPath }

& $compilerPath `
    /nologo `
    /target:winexe `
    /platform:x64 `
    /optimize+ `
    /warn:4 `
    /win32icon:$iconPath `
    /out:$tempPath `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    $sourcePath

if ($LASTEXITCODE -ne 0) {
    if (Test-Path $tempPath) { Remove-Item -Force $tempPath }
    throw "C# compiler failed with exit code $LASTEXITCODE"
}

if (Test-Path $distPath) { Remove-Item -Force $distPath }
Move-Item -Force $tempPath $distPath
Write-Host "Built $distPath"

if (-not $SkipDesktopCopy) {
    Copy-Item -Force $distPath $desktopPath
    Write-Host "Updated $desktopPath"
}
