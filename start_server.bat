@echo off
chcp 65001 >nul
echo Starting Astrum Game Server...
echo Logs will be output to server.log file
echo.

cd /d "%~dp0AstrumServer\AstrumServer"

echo Current directory: %CD%
echo Building project...
dotnet build

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Build successful, starting server...
echo Server logs will be output to both console and file: %CD%\server.log
echo Press Ctrl+C to stop server
echo.

dotnet run --no-build

echo Server stopped
pause 