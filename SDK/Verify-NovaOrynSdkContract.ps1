[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
function Fail([string]$Message) { Write-Host "[FAIL] $Message"; exit 1 }
$manifestPath = Join-Path $root 'NovaOryn.SdkManifest.json'
$contractPath = Join-Path $root 'NovaOryn.ApiContract.json'
if (!(Test-Path $manifestPath)) { Fail 'NovaOryn.SdkManifest.json is missing.' }
if (!(Test-Path $contractPath)) { Fail 'NovaOryn.ApiContract.json is missing.' }
if (!(Test-Path (Join-Path $root 'Compare-NovaOrynApiCompatibility.ps1'))) { Fail 'Compare-NovaOrynApiCompatibility.ps1 is missing.' }
$m = Get-Content $manifestPath -Raw | ConvertFrom-Json
$c = Get-Content $contractPath -Raw | ConvertFrom-Json
if ([string]$m.sdkVersion -ne '0.41.0') { Fail "SDK manifest version is $($m.sdkVersion); expected 0.41.0." }
if ([string]$m.apiVersion -ne [string]$c.apiVersion) { Fail 'SDK manifest API version does not match the API contract.' }
foreach ($name in @('kernel','driver','syscall','debug','crashDump','heapDiagnostics')) {
    if ([string]::IsNullOrWhiteSpace([string]$m.abi.$name)) { Fail "ABI version '$name' is missing." }
}
$core = Get-Content (Join-Path $root 'src\NovaOryn.Core\NovaOrynSdkContract.cs') -Raw
foreach ($value in @('SdkVersion = "0.41.0"','ApiVersion = "1.2"','DriverAbiVersion = "1.0"')) {
    if (!$core.Contains($value)) { Fail "NovaOrynSdkContract is missing $value." }
}
Write-Host '[ OK ] NovaOryn SDK 0.41.0 stable API/ABI contract manifest verified.'
exit 0
