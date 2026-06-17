@echo off
setlocal
set "SCRIPT_DIR=%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT_DIR%CloneUnauthorizedPassenger.ps1" %*

echo.
pause
