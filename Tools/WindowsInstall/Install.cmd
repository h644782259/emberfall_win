@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-Install.ps1" %*
if errorlevel 1 (
    echo Installation failed. See the error above and Logs\windows-install.log.
    pause
    exit /b 1
)
exit /b 0
