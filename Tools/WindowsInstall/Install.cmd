@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Run-Install.ps1" %*
exit /b %errorlevel%
