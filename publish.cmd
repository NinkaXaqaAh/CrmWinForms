@echo off
rem Double-click this file to publish a fresh exe and recreate the desktop shortcut.
rem It just runs publish.ps1 with PowerShell, bypassing the default execution policy.

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\publish.ps1"

echo.
pause
