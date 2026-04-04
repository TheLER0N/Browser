@echo off
chcp 65001 > nul
echo Диагностика запуска GhostBrowser...
echo.
set COMPlus_ThrowUnobservedThreadExceptions=1
set DOTNET_EnableWriteXorExecute=0
set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0
GhostBrowser.exe
pause
