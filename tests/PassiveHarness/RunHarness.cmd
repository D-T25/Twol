@echo off
setlocal
cd /d "%~dp0"
if exist "%~dp0PassiveHarness.exe" goto packaged
where dotnet >nul 2>nul
if errorlevel 1 (
  echo .NET SDK is required for the source version. The packaged Windows download does not need it.
  pause
  exit /b 2
)
dotnet run --configuration Release --project "%~dp0PassiveHarness.csproj" -- --output "%~dp0harness-output" %*
set "harnessExit=%errorlevel%"
goto finished
:packaged
"%~dp0PassiveHarness.exe" --output "%~dp0harness-output" %*
set "harnessExit=%errorlevel%"
:finished
if exist "%~dp0harness-output\index.html" start "" "%~dp0harness-output\index.html"
echo.
echo Harness exit code: %harnessExit% (0=all checks passed, 1=failed checks, 2=setup error).
echo The calibration branch currently has known failed improvement checks. Review the report.
pause
exit /b %harnessExit%
