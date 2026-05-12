param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root "src"
$bin = Join-Path $root "bin"
New-Item -ItemType Directory -Force -Path $bin | Out-Null

$candidates = @(
    "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)

$csc = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $csc) {
    throw "Could not find csc.exe. Install Visual Studio Build Tools or the .NET Framework compiler."
}

$sources = Get-ChildItem -LiteralPath $src -Filter *.cs | Sort-Object Name | ForEach-Object { $_.FullName }
$out = Join-Path $bin "SoulReaverEditor.exe"

$refs = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll"
)

$args = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/out:$out",
    "/win32manifest:$root\app.manifest"
)

if ($Configuration -ieq "Debug") {
    $args += "/debug+"
    $args += "/optimize-"
} else {
    $args += "/debug:pdbonly"
    $args += "/optimize+"
}

foreach ($ref in $refs) {
    $args += "/reference:$ref"
}

$args += $sources

Write-Host "Compiling SoulReaverEditor with $csc"
& $csc @args
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Built $out"

$launcherSource = Join-Path $root "Launch SoulReaverEditor.bat"
$launcherTarget = Join-Path $bin "Launch SoulReaverEditor.bat"
if (Test-Path -LiteralPath $launcherSource) {
    Copy-Item -LiteralPath $launcherSource -Destination $launcherTarget -Force
    Write-Host "Copied $launcherTarget"
}
