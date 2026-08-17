[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "[INFO] Running exhaustive NovaOryn SDK validation."
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "Verify-NovaOrynSdkContract.ps1")
if ($LASTEXITCODE -ne 0) { throw "NovaOryn SDK contract validation failed with exit code $LASTEXITCODE." }
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "Build-NovaOrynDocumentation.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "NovaOryn documentation validation failed with exit code $LASTEXITCODE." }

$arguments = @(
    "-NoProfile", "-ExecutionPolicy", "Bypass",
    "-File", (Join-Path $root "Build-NovaOryn.ps1"),
    "-Configuration", $Configuration,
    "-Validate",
    "-NoRun"
)
if ($DryRun) { $arguments += "-DryRun" }
& powershell.exe @arguments
if ($LASTEXITCODE -ne 0) { throw "NovaOryn exhaustive validation failed with exit code $LASTEXITCODE." }

Write-Host "[ OK ] NovaOryn exhaustive SDK validation completed."
exit 0
