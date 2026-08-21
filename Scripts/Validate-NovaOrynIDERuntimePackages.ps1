[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$rootPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$npmRoot = Join-Path $rootPath '.toolchain\NpmWorkspace'
$rootManifestPath = Join-Path $rootPath 'JSON\package.json'
$electronManifestPath = Join-Path $rootPath 'applications\electron\package.json'
$extensionManifestPath = Join-Path $rootPath 'packages\novaoryn-ide\package.json'

foreach ($manifestPath in @($rootManifestPath, $electronManifestPath, $extensionManifestPath)) {
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        Write-Host "[FAIL] NovaOryn package manifest is missing: $manifestPath"
        exit 2
    }
}

function Read-Json([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

try {
    $rootManifest = Read-Json $rootManifestPath
    $electronManifest = Read-Json $electronManifestPath
    $extensionManifest = Read-Json $extensionManifestPath
}
catch {
    Write-Host '[FAIL] Could not read NovaOryn package manifests.'
    exit 3
}

$requirements = @{}
function Add-Requirements($Object) {
    if ($null -eq $Object) { return }
    foreach ($property in $Object.PSObject.Properties) {
        if ($property.Name -like '@novaoryn/*') { continue }
        $requirements[$property.Name] = [string]$property.Value
    }
}

Add-Requirements $rootManifest.dependencies
Add-Requirements $rootManifest.devDependencies
Add-Requirements $electronManifest.dependencies
Add-Requirements $electronManifest.devDependencies
Add-Requirements $extensionManifest.dependencies
Add-Requirements $extensionManifest.devDependencies

function Package-Manifest-Path([string]$PackageName) {
    return Join-Path (Join-Path $npmRoot 'node_modules') (Join-Path $PackageName 'package.json')
}

$missing = @()
$mismatched = @()
foreach ($packageName in ($requirements.Keys | Sort-Object)) {
    $packagePath = Package-Manifest-Path $packageName
    if (-not (Test-Path -LiteralPath $packagePath)) {
        $missing += $packageName
        continue
    }

    try { $installed = Read-Json $packagePath }
    catch {
        $missing += $packageName
        continue
    }

    $expected = [string]$requirements[$packageName]
    # Exact pins are compared exactly. Range-based development dependencies are
    # required to exist; npm remains responsible for resolving the declared range.
    if ($expected -match '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
        if ([string]$installed.version -ne $expected) {
            $mismatched += "$packageName=$($installed.version) (expected $expected)"
        }
    }
}

if ($missing.Count -gt 0) {
    Write-Host "[FAIL] Required npm packages are missing: $($missing -join ', ')"
    exit 4
}
if ($mismatched.Count -gt 0) {
    Write-Host "[FAIL] Required npm package versions do not match: $($mismatched -join '; ')"
    exit 5
}

Write-Host "[ OK ] Required npm package manifests verified: $($requirements.Count) packages."
exit 0
