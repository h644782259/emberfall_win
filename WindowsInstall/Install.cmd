@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Install-Windows.ps1" %*
if errorlevel 1 (
    echo Build or installation failed. See the message above.
    pause
    exit /b 1
)
pause
