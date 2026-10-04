@echo off
"%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe" /nologo /target:winexe /out:"%~dp0..\AutoBrightness.exe" "%~dp0SharedBrightnessApp.cs" "%~dp0MonitorBrightnessControl.cs"
if errorlevel 1 exit /b 1
echo Build completed.
