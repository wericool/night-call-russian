@echo off
chcp 65001 >nul
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-NativePatchFR.ps1" -Mode Check %*
if errorlevel 1 echo Ошибка. Проверьте сообщение установщика.
pause
