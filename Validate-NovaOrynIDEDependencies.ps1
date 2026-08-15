[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootPackagePath = Join-Path $Root 'package.json'
$ElectronPackagePath = Join-Path $Root 'applications\electron\package.json'
$ExtensionPackagePath = Join-Path $Root 'packages\novaoryn-ide\package.json'

$ExpectedIdeVersion = '0.0.10'
$ExpectedTheiaVersion = '1.73.0'
$ExpectedElectronVersion = '39.8.7'
$ExpectedWindowsCaCertsVersion = '0.3.4'

function Fail([string]$Message) {
    Write-Host "[FAIL] $Message"
    exit 1
}

function Ok([string]$Message) {
    Write-Host "[ OK ] $Message"
}

try {
    $rootPackage = Get-Content -LiteralPath $RootPackagePath -Raw | ConvertFrom-Json
    $electronPackage = Get-Content -LiteralPath $ElectronPackagePath -Raw | ConvertFrom-Json
    $extensionPackage = Get-Content -LiteralPath $ExtensionPackagePath -Raw | ConvertFrom-Json

    if ([string]$rootPackage.version -ne $ExpectedIdeVersion) {
        Fail "Root package version is $($rootPackage.version); expected $ExpectedIdeVersion."
    }
    if ([string]$electronPackage.version -ne $ExpectedIdeVersion) {
        Fail "Electron application version is $($electronPackage.version); expected $ExpectedIdeVersion."
    }
    if ([string]$extensionPackage.version -ne $ExpectedIdeVersion) {
        Fail "NovaOryn extension version is $($extensionPackage.version); expected $ExpectedIdeVersion."
    }
    if ([string]$electronPackage.dependencies.'@novaoryn/ide-extension' -ne $ExpectedIdeVersion) {
        Fail "Electron application references @novaoryn/ide-extension $($electronPackage.dependencies.'@novaoryn/ide-extension'); expected $ExpectedIdeVersion."
    }

    $theiaVersions = @()
    foreach ($property in $electronPackage.dependencies.PSObject.Properties) {
        if ($property.Name -like '@theia/*') { $theiaVersions += [string]$property.Value }
    }
    foreach ($property in $electronPackage.devDependencies.PSObject.Properties) {
        if ($property.Name -like '@theia/*') { $theiaVersions += [string]$property.Value }
    }
    foreach ($property in $extensionPackage.dependencies.PSObject.Properties) {
        if ($property.Name -like '@theia/*') { $theiaVersions += [string]$property.Value }
    }
    foreach ($property in $extensionPackage.devDependencies.PSObject.Properties) {
        if ($property.Name -like '@theia/*') { $theiaVersions += [string]$property.Value }
    }

    $badTheiaVersions = $theiaVersions | Where-Object { $_ -ne $ExpectedTheiaVersion } | Select-Object -Unique
    if ($badTheiaVersions) {
        Fail "All Eclipse Theia packages must be pinned to $ExpectedTheiaVersion. Found: $($badTheiaVersions -join ', ')."
    }

    $actualElectron = [string]$electronPackage.devDependencies.electron
    if ($actualElectron -ne $ExpectedElectronVersion) {
        Fail "Eclipse Theia $ExpectedTheiaVersion requires the NovaOryn Electron devDependency to be $ExpectedElectronVersion; found $actualElectron."
    }

    $actualWindowsCaCerts = [string]$electronPackage.dependencies.'@vscode/windows-ca-certs'
    if ($actualWindowsCaCerts -ne $ExpectedWindowsCaCertsVersion) {
        Fail "NovaOryn IDE requires @vscode/windows-ca-certs $ExpectedWindowsCaCertsVersion for the Theia Node bundle on Windows; found $actualWindowsCaCerts."
    }

    Ok "NovaOryn IDE package versions are internally consistent at $ExpectedIdeVersion."
    Ok "Eclipse Theia $ExpectedTheiaVersion / Electron $ExpectedElectronVersion compatibility pair verified."
    Ok "Windows CA certificate bundle dependency @vscode/windows-ca-certs $ExpectedWindowsCaCertsVersion verified."
    exit 0
} catch {
    Fail $_.Exception.Message
}
