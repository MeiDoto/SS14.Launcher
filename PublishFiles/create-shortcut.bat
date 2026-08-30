@echo off
setlocal
cd /d "%~dp0"
echo ======================================================
echo  Creating Space Station 14 Launcher Shortcuts
echo ======================================================

set "TARGET_EXE=%CD%\Space Station 14 Launcher.exe"
if not exist "%TARGET_EXE%" (
    set "TARGET_EXE=%CD%\bin_x64\SS14.Launcher.exe"
)
if not exist "%TARGET_EXE%" (
    set "TARGET_EXE=%CD%\SS14.Launcher.exe"
)

if not exist "%TARGET_EXE%" (
    echo Error: Launcher executable not found in %CD%
    pause
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$ws = New-Object -ComObject WScript.Shell; $desktop = [Environment]::GetFolderPath('Desktop'); if (Test-Path $desktop) { $sc = $ws.CreateShortcut((Join-Path $desktop 'Space Station 14 Launcher.lnk')); $sc.TargetPath = $env:TARGET_EXE; $sc.WorkingDirectory = $env:CD; $sc.Description = 'Space Station 14 Launcher'; $sc.IconLocation = ($env:TARGET_EXE + ',0'); $sc.Save() }; $programs = [Environment]::GetFolderPath('Programs'); if (Test-Path $programs) { $sc2 = $ws.CreateShortcut((Join-Path $programs 'Space Station 14 Launcher.lnk')); $sc2.TargetPath = $env:TARGET_EXE; $sc2.WorkingDirectory = $env:CD; $sc2.Description = 'Space Station 14 Launcher'; $sc2.IconLocation = ($env:TARGET_EXE + ',0'); $sc2.Save() }"

echo [OK] Shortcuts created on Desktop and in Start Menu!
echo.
timeout /t 3 >nul
