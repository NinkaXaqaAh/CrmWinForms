@echo off
rem Double-click this file to regenerate icon.ico from Icon.jpg.
rem Run this when you replace Icon.jpg, then run publish.cmd to put the new icon
rem into the exe and shortcut.

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\make-icon.ps1"

echo.
pause
