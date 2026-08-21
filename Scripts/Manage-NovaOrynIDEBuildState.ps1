[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('Invalidate','VerifyDependencies','StampDependencies','Stamp','Validate')]
    [string]$Action
)

$ErrorActionPreference = 'Stop'

$rootPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$versionPath = Join-Path $rootPath 'VERSION'
$rootManifestPath = Join-Path $rootPath 'JSON\package.json'
$electronManifestPath = Join-Path $rootPath 'applications\electron\package.json'
$extensionManifestPath = Join-Path $rootPath 'packages\novaoryn-ide\package.json'

foreach ($required in @($versionPath, $rootManifestPath, $electronManifestPath, $extensionManifestPath)) {
    if (-not (Test-Path -LiteralPath $required)) {
        Write-Host "[FAIL] Required NovaOryn IDE release input is missing: $required"
        exit 10
    }
}

$Version = (Get-Content -LiteralPath $versionPath -TotalCount 1).Trim()
if (-not $Version) {
    Write-Host '[FAIL] VERSION line 1 is empty.'
    exit 11
}

function Read-Json([string]$Path) {
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

try {
    $rootManifest = Read-Json $rootManifestPath
    $electronManifest = Read-Json $electronManifestPath
    $extensionManifest = Read-Json $extensionManifestPath
    $TheiaVersion = [string]$electronManifest.dependencies.'@theia/electron'
    $ElectronVersion = [string]$electronManifest.devDependencies.electron
}
catch {
    Write-Host '[FAIL] Could not read NovaOryn IDE dependency manifests.'
    exit 12
}

if (-not $TheiaVersion -or -not $ElectronVersion) {
    Write-Host '[FAIL] Theia/Electron version pins are missing from applications\electron\package.json.'
    exit 13
}

# Dependency validity deliberately excludes the NovaOryn IDE application version.
# A normal IDE release bump must not force a 1,000+ package reinstall when the
# dependency declarations themselves are unchanged.
function Add-DependencySection([System.Text.StringBuilder]$Builder, [string]$Label, $Object) {
    [void]$Builder.AppendLine("[$Label]")
    if ($null -eq $Object) { return }
    foreach ($p in ($Object.PSObject.Properties | Sort-Object Name)) {
        $value = [string]$p.Value
        if ($p.Name -like '@novaoryn/*') { $value = '<workspace>' }
        [void]$Builder.AppendLine("$($p.Name)=$value")
    }
}

$dependencyText = New-Object System.Text.StringBuilder
Add-DependencySection $dependencyText 'root.dependencies' $rootManifest.dependencies
Add-DependencySection $dependencyText 'root.devDependencies' $rootManifest.devDependencies
Add-DependencySection $dependencyText 'root.peerDependencies' $rootManifest.peerDependencies
Add-DependencySection $dependencyText 'electron.dependencies' $electronManifest.dependencies
Add-DependencySection $dependencyText 'electron.devDependencies' $electronManifest.devDependencies
Add-DependencySection $dependencyText 'electron.peerDependencies' $electronManifest.peerDependencies
Add-DependencySection $dependencyText 'extension.dependencies' $extensionManifest.dependencies
Add-DependencySection $dependencyText 'extension.devDependencies' $extensionManifest.devDependencies
Add-DependencySection $dependencyText 'extension.peerDependencies' $extensionManifest.peerDependencies
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $hashBytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($dependencyText.ToString()))
    $dependencyFingerprint = [BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant()
}
finally { $sha.Dispose() }

$toolchainState = Join-Path $rootPath '.toolchain\NovaOrynIDE-BuildState.json'
$dependencyState = Join-Path $rootPath '.toolchain\NovaOrynIDE-DependencyState.json'
$browserModules = Join-Path $rootPath '.browser_modules'
$npmWorkspace = Join-Path $rootPath '.toolchain\NpmWorkspace'
$npmModules = Join-Path $npmWorkspace 'node_modules'
$generatedLib = Join-Path $rootPath 'applications\electron\lib'
$generatedVersion = Join-Path $generatedLib '.novaoryn-build-version'
$generatedState = Join-Path $generatedLib '.novaoryn-build-state.json'
$sourceLock = Join-Path $rootPath 'JSON\package-lock.json'
$stagedLock = Join-Path $npmWorkspace 'package-lock.json'

function Read-State([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    try { return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json }
    catch { return $null }
}

function Dependency-State-Matches($State) {
    if ($null -eq $State) { return $false }
    $pinsMatch = ([string]$State.theiaVersion -eq $TheiaVersion -and
                  [string]$State.electronVersion -eq $ElectronVersion)
    if (-not $pinsMatch) { return $false }
    # Migrate pre-0.14.8 dependency markers without deleting a known-good tree.
    if (-not [string]$State.dependencyFingerprint) { return $true }
    return [string]$State.dependencyFingerprint -eq $dependencyFingerprint
}

function Build-State-Matches($State) {
    if ($null -eq $State) { return $false }
    return ([string]$State.novaOrynIdeVersion -eq $Version -and (Dependency-State-Matches $State))
}

function Write-Dependency-State {
    $toolchainDir = Split-Path -Parent $dependencyState
    if (-not (Test-Path -LiteralPath $toolchainDir)) {
        New-Item -ItemType Directory -Path $toolchainDir -Force | Out-Null
    }
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    $state = [ordered]@{
        novaOrynIdeVersion = $Version
        dependencyFingerprint = $dependencyFingerprint
        theiaVersion = $TheiaVersion
        electronVersion = $ElectronVersion
        generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
    }
    [IO.File]::WriteAllText($dependencyState, ($state | ConvertTo-Json), $utf8)
}

switch ($Action) {
    'Invalidate' {
        $state = Read-State $dependencyState
        $installed = Test-Path -LiteralPath $npmModules

        if (-not $installed) {
            Write-Host '[INFO] npm dependency tree is not installed yet.'
            exit 0
        }

        if ($null -eq $state) {
            Write-Host '[INFO] Installed npm tree has no dependency marker yet; preserving it for direct package verification instead of deleting it.'
            exit 0
        }

        if (Dependency-State-Matches $state) {
            Write-Host '[ OK ] Existing npm dependency state matches the current dependency manifests; reinstall is not required.'
            exit 0
        }

        Write-Host '[INFO] Removing stale npm dependency tree because dependency declarations changed.'
        Remove-Item -LiteralPath $npmModules -Recurse -Force
        if (Test-Path -LiteralPath $browserModules) {
            Write-Host '[INFO] Removing stale Theia native-module cache from the earlier dependency set.'
            Remove-Item -LiteralPath $browserModules -Recurse -Force
        }
        foreach ($path in @($dependencyState, $sourceLock, $stagedLock)) {
            if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
        }
        Write-Host '[ OK ] Stale dependency state invalidated.'
        exit 0
    }

    'VerifyDependencies' {
        $installedElectron = Join-Path $npmModules 'electron\package.json'
        $installedTheia = Join-Path $npmModules '@theia\electron\package.json'
        $installedTheiaCli = Join-Path $npmModules '@theia\cli\package.json'
        $installedWindowsCa = Join-Path $npmModules '@vscode\windows-ca-certs\package.json'
        $installedTypeScript = Join-Path $npmModules 'typescript\package.json'
        $installedReactTypes = Join-Path $npmModules '@types\react\package.json'
        foreach ($requiredPackage in @($installedElectron, $installedTheia, $installedTheiaCli, $installedWindowsCa, $installedTypeScript, $installedReactTypes)) {
            if (-not (Test-Path -LiteralPath $requiredPackage)) {
                Write-Host "[FAIL] Required installed package could not be located: $requiredPackage"
                exit 20
            }
        }
        try {
            $electronPackage = Read-Json $installedElectron
            $theiaPackage = Read-Json $installedTheia
            $theiaCliPackage = Read-Json $installedTheiaCli
            $windowsCaPackage = Read-Json $installedWindowsCa
            $typeScriptPackage = Read-Json $installedTypeScript
            $reactTypesPackage = Read-Json $installedReactTypes
            $peer = [string]$theiaPackage.peerDependencies.electron
            $expectedTheiaCli = [string]$rootManifest.devDependencies.'@theia/cli'
            $expectedWindowsCa = [string]$rootManifest.devDependencies.'@vscode/windows-ca-certs'
            Write-Host "[INFO] Installed Electron: $($electronPackage.version)"
            Write-Host "[INFO] @theia/electron: $($theiaPackage.version) requires Electron $peer"
            Write-Host "[INFO] @theia/cli: $($theiaCliPackage.version)"
            Write-Host "[INFO] TypeScript: $($typeScriptPackage.version)"
            Write-Host "[INFO] @types/react: $($reactTypesPackage.version)"
            if ([string]$electronPackage.version -ne $ElectronVersion) { throw "Installed Electron $($electronPackage.version) does not match $ElectronVersion." }
            if ([string]$theiaPackage.version -ne $TheiaVersion) { throw "Installed @theia/electron $($theiaPackage.version) does not match $TheiaVersion." }
            if ([string]$theiaCliPackage.version -ne $expectedTheiaCli) { throw "Installed @theia/cli $($theiaCliPackage.version) does not match $expectedTheiaCli." }
            if ([string]$windowsCaPackage.version -ne $expectedWindowsCa) { throw "Installed @vscode/windows-ca-certs $($windowsCaPackage.version) does not match $expectedWindowsCa." }
            if ($peer -ne $ElectronVersion) { throw "@theia/electron peer dependency $peer does not match $ElectronVersion." }
            Write-Dependency-State
            Write-Host '[ OK ] Installed Theia/Electron versions are synchronized.'
            Write-Host "[ OK ] Verified dependency-state marker: $Version"
            exit 0
        }
        catch {
            Write-Host "[FAIL] $($_.Exception.Message)"
            exit 21
        }
    }

    'StampDependencies' {
        Write-Dependency-State
        Write-Host "[ OK ] Verified dependency-state marker: $Version"
        exit 0
    }

    'Stamp' {
        if (-not (Test-Path -LiteralPath $generatedLib)) {
            New-Item -ItemType Directory -Path $generatedLib -Force | Out-Null
        }
        $toolchainDir = Split-Path -Parent $toolchainState
        if (-not (Test-Path -LiteralPath $toolchainDir)) {
            New-Item -ItemType Directory -Path $toolchainDir -Force | Out-Null
        }

        $utf8 = New-Object System.Text.UTF8Encoding($false)
        [IO.File]::WriteAllText($generatedVersion, $Version, $utf8)
        $state = [ordered]@{
            novaOrynIdeVersion = $Version
            dependencyFingerprint = $dependencyFingerprint
            theiaVersion = $TheiaVersion
            electronVersion = $ElectronVersion
            generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
        }
        $json = $state | ConvertTo-Json
        [IO.File]::WriteAllText($generatedState, $json, $utf8)
        [IO.File]::WriteAllText($toolchainState, $json, $utf8)
        [IO.File]::WriteAllText($dependencyState, $json, $utf8)
        Write-Host "[ OK ] Generated build-state marker: $Version"
        exit 0
    }

    'Validate' {
        if (-not (Test-Path -LiteralPath $generatedVersion)) { exit 2 }
        if (-not (Test-Path -LiteralPath $generatedState)) { exit 2 }
        try {
            $markerVersion = (Get-Content -LiteralPath $generatedVersion -Raw).Trim()
            $state = Read-State $generatedState
            if ($markerVersion -ne $Version -or -not (Build-State-Matches $state)) { exit 2 }
            exit 0
        }
        catch { exit 3 }
    }
}
