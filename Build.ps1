$ErrorActionPreference = "Stop"

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $projectDir "Program.cs"
$iconPath = Join-Path $projectDir "AutoClicker.ico"
$desktopPath = Join-Path $env:USERPROFILE "OneDrive\Desktop\AutoClicker.exe"
$compilerPath = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $compilerPath)) {
    throw "C# compiler was not found at $compilerPath"
}

& (Join-Path $projectDir "GenerateIcon.ps1")

& $compilerPath `
    /nologo `
    /target:winexe `
    /platform:x64 `
    /win32icon:$iconPath `
    /out:$desktopPath `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    $sourcePath

if ($LASTEXITCODE -ne 0) {
    throw "C# compiler failed with exit code $LASTEXITCODE"
}

Write-Host "Built $desktopPath"
