@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build-Install-Windows.ps1" %*
exit /b %errorlevel%
