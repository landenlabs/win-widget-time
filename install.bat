@echo off

echo Build and install WinWidgetTime

:: Kill widget is running, so it can be re-built
taskkill /IM WinWidgetTime.exe /F 2>nul
dotnet publish WinWidgetTime.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

:: Make directory to hold the exe and assets
mkdir c:\opt\bin\winwidgets 2>nul
xcopy /E /Y bin\Release\net8.0-windows\win-x64\publish\* c:\opt\bin\winwidgets\

echo start "" c:\opt\bin\winwidgets\WinWidgetTime.exe > c:\opt\bin\WinWidgetTime.bat

