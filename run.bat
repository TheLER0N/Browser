@echo off
chcp 65001 > nul
echo ========================================
echo    GhostBrowser - Быстрый запуск
echo ========================================
echo.

:: Check if .NET is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ОШИБКА] .NET SDK не установлен!
    echo Скачайте с: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo [1/3] Восстановление зависимостей...
call dotnet restore
if %errorlevel% neq 0 (
    echo [ОШИБКА] Не удалось восстановить зависимости
    pause
    exit /b 1
)

echo [2/3] Сборка проекта...
call dotnet build --no-restore
if %errorlevel% neq 0 (
    echo [ОШИБКА] Ошибка сборки
    pause
    exit /b 1
)

echo [3/3] Запуск браузера...
echo.
echo ========================================
echo    Горячие клавиши:
echo    Ctrl+T        - Новая вкладка
echo    Ctrl+W        - Закрыть вкладку
echo    Ctrl+Shift+H  - Режим невидимости
echo    Ctrl+L        - Адресная строка
echo ========================================
echo.

call dotnet run

pause
