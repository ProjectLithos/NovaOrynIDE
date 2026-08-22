@echo off
setlocal
set "DOTNET=%~dp0.toolchain\DotNet\dotnet.exe"
if not exist "%DOTNET%" (
  echo [FAIL] NovaOryn pinned .NET toolchain was not found: %DOTNET%
  exit /b 1
)
"%DOTNET%" run --project "%~dp0src\NovaOryn.PackageManager\NovaOryn.PackageManager.csproj" --configuration Release -- %*
exit /b %ERRORLEVEL%
