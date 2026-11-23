@echo off
setlocal

:: Usage: build.bat [destination-bin-dir]
::   Destination defaults to the BINDIR environment variable.
::   Installs exe to <dest>\winwidgets\ and launcher to <dest>\WinWidgetTime.bat

set "DEST=%~1"
if "%DEST%"=="" set "DEST=%BINDIR%"

if "%DEST%"=="" (
    echo ERROR: No destination given and BINDIR environment variable is not set.
    goto :usage
)
:: Strip trailing backslash
if "%DEST:~-1%"=="\" set "DEST=%DEST:~0,-1%"
if not exist "%DEST%\" (
    echo ERROR: Destination "%DEST%" is not a valid directory.
    goto :usage
)

echo Build and install WinWidgetTime (portable, compressed single-file) to "%DEST%"

:: Kill widget if running, so it can be re-built
taskkill /IM WinWidgetTime.exe /F 2>nul

:: Drag / resize / on-screen geometry regression tests
:: Runs in-process (xUnit v3 self-running exe) rather than via "dotnet test", whose localhost
:: socket to the test host can be stalled for 90 s by endpoint-security / VPN software.
dotnet run --project tests\WinWidgetTime.Tests -c Release
if errorlevel 1 (
    echo ERROR: unit tests failed.
    exit /b 1
)

:: Portable self-contained single-file build:
::   --self-contained true                  bundle the .NET runtime (runs without an install)
::   PublishSingleFile                       emit one .exe
::   EnableCompressionInSingleFile           compress the bundled assemblies (smaller .exe)
::   IncludeNativeLibrariesForSelfExtract    pull native libraries inside the .exe
::   IncludeAllContentForSelfExtract         bundle Assets (logo) inside the .exe too
dotnet publish WinWidgetTime.csproj -c Release -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:IncludeAllContentForSelfExtract=true
if errorlevel 1 (
    echo ERROR: dotnet publish failed.
    exit /b 1
)

:: Make directory to hold the portable exe
if not exist "%DEST%\winwidgets\" mkdir "%DEST%\winwidgets"
xcopy /E /Y bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\* "%DEST%\winwidgets\"

echo start "" "%DEST%\winwidgets\WinWidgetTime.exe" > "%DEST%\WinWidgetTime.bat"
exit /b 0

:usage
echo.
echo Usage: build.bat ^<destination-bin-dir^>
echo    or set BINDIR to an existing directory and run build.bat with no arguments.
exit /b 1
