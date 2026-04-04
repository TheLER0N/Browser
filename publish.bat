@echo off
chcp 65001 > nul
echo ========================================
echo    GhostBrowser - Публикация
echo ========================================
echo.
echo Создание standalone .exe файла...
echo.

dotnet publish -c Release -r win-x64 --self-contained -o ./publish

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo    Успешно!
    echo    Файл: publish/GhostBrowser.exe
    echo ========================================
    echo.
    dir publish\*.exe
) else (
    echo [ОШИБКА] Ошибка публикации
)

echo.
pause
