@echo off

echo Build and install WinWidgetTime
set dstDir=e:\opt\bin

:: Kill widget is running, so it can be re-built
taskkill /IM WinWidgetTime.exe /F 2>nul
dotnet publish WinWidgetTime.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

:: Make directory to hold the exe and assets
mkdir %dstDir%\winwidgets 2>nul
xcopy /E /Y bin\Release\net8.0-windows\win-x64\publish\* %dstDir%\winwidgets\

echo start "" %dstDir%\winwidgets\WinWidgetTime.exe > %dstDir%\WinWidgetTime.bat

