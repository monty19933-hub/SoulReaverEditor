@echo off
setlocal
set "APPDIR=%~dp0"
if not exist "%APPDIR%SoulReaverEditor.exe" if exist "%APPDIR%bin\SoulReaverEditor.exe" set "APPDIR=%APPDIR%bin\"
cd /d "%APPDIR%"
echo Starting Soul Reaver Editor...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$app = Join-Path $env:APPDIR 'SoulReaverEditor.exe'; $log = Join-Path $env:APPDIR 'SoulReaverEditor.log'; if (Test-Path $log) { Remove-Item -LiteralPath $log -Force }; $p = Start-Process -FilePath $app -WorkingDirectory $env:APPDIR -PassThru -Wait; if (Test-Path $log) { Write-Host ''; Write-Host 'SoulReaverEditor.log:'; Get-Content -LiteralPath $log -Tail 120 }; Write-Host ''; Read-Host 'Press Enter to close this window'"
