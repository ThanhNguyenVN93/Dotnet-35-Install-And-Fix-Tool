@echo off
title .NET 3.5 Setup - Windows 7 RTM Bootstrap
color 0A

echo ============================================================
echo   .NET 3.5 Setup and Fix Tool - Windows 7 RTM Bootstrap
echo ============================================================
echo.

:: Check Windows 7 RTM (Build 7600)
ver | find "6.1.7600" > nul
if %errorlevel% NEQ 0 (
    echo [OK] This system does not require the bootstrap.
    echo      Launching main tool...
    goto LAUNCH
)

echo [!] Windows 7 RTM (Build 7600) detected.
echo     SP1 is required before running the main tool.
echo.
echo ============================================================
echo   OFFLINE INSTALLATION INSTRUCTIONS
echo ============================================================
echo.
echo  You need 2 update files. Copy them to this folder:
echo.
echo    %~dp0
echo.
echo  Required files:
echo    1. KB4474419-x64.msu   (or KB4474419-x86.msu for 32-bit)
echo       - SHA-2 Code Signing Support Update
echo.
echo    2. KB4490628.msu
echo       - Servicing Stack Update
echo.
echo  Download from another PC at:
echo    https://www.microsoft.com/en-us/download/details.aspx?id=56571
echo    https://support.microsoft.com/en-us/topic/kb4490628
echo.
echo ============================================================
echo.

:: Detect architecture
if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    set KB1=KB4474419-x64.msu
) else (
    set KB1=KB4474419-x86.msu
)
set KB2=KB4490628.msu

:: Check if files exist in same folder as .bat
if not exist "%~dp0%KB1%" (
    echo [X] File not found: %KB1%
    echo     Please copy the file to: %~dp0
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0%KB2%" (
    echo [X] File not found: %KB2%
    echo     Please copy the file to: %~dp0
    echo.
    pause
    exit /b 1
)

echo [*] Installing %KB1% ...
wusa.exe "%~dp0%KB1%" /quiet /norestart
echo     Done. (Exit code: %errorlevel%)
echo.

echo [*] Installing %KB2% ...
wusa.exe "%~dp0%KB2%" /quiet /norestart
echo     Done. (Exit code: %errorlevel%)
echo.

echo ============================================================
echo  [OK] Updates installed successfully.
echo       The system will restart in 15 seconds to apply changes.
echo       After restart, run frmnet3.5fix.exe to install .NET 3.5
echo ============================================================
echo.
shutdown /r /t 15 /c "Restarting to complete Windows 7 SP1 prerequisite updates."
pause
exit /b 0

:LAUNCH
start "" "%~dp0frmnet3.5fix.exe"
exit /b 0
