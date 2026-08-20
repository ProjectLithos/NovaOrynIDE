[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootPackagePath = Join-Path $Root 'package.json'
$ElectronPackagePath = Join-Path $Root 'applications\electron\package.json'
$ExtensionPackagePath = Join-Path $Root 'packages\novaoryn-ide\package.json'

$ExpectedIdeVersion = '0.10.3'
$ExpectedTheiaVersion = '1.74.0'
$ExpectedElectronVersion = '42.3.0'
$ExpectedWindowsCaCertsVersion = '0.3.4'
$ExpectedMonacoEditorCoreVersion = '1.108.201'
$ForbiddenRuntimePackages = @('@theia/plugin-ext', '@theia/plugin-ext-vscode', '@theia/vsx-registry')

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

    foreach ($pair in @(
        @{ Name='Root package'; Value=[string]$rootPackage.version },
        @{ Name='Electron application'; Value=[string]$electronPackage.version },
        @{ Name='NovaOryn extension'; Value=[string]$extensionPackage.version }
    )) {
        if ($pair.Value -ne $ExpectedIdeVersion) {
            Fail "$($pair.Name) version is $($pair.Value); expected $ExpectedIdeVersion."
        }
    }

    if ([string]$electronPackage.dependencies.'@novaoryn/ide-extension' -ne $ExpectedIdeVersion) {
        Fail "Electron application references @novaoryn/ide-extension $($electronPackage.dependencies.'@novaoryn/ide-extension'); expected $ExpectedIdeVersion."
    }

    $theiaVersions = @()
    foreach ($package in @($electronPackage, $extensionPackage)) {
        foreach ($groupName in @('dependencies','devDependencies')) {
            $group = $package.$groupName
            if ($null -ne $group) {
                foreach ($property in $group.PSObject.Properties) {
                    if ($property.Name -like '@theia/*' -and $property.Name -ne '@theia/monaco-editor-core') { $theiaVersions += [string]$property.Value }
                }
            }
        }
    }
    $badTheiaVersions = $theiaVersions | Where-Object { $_ -ne $ExpectedTheiaVersion } | Select-Object -Unique
    if ($badTheiaVersions) {
        Fail "All Eclipse Theia packages must be pinned to $ExpectedTheiaVersion. Found: $($badTheiaVersions -join ', ')."
    }

    $actualMonacoEditorCore = [string]$extensionPackage.dependencies.'@theia/monaco-editor-core'
    if ($actualMonacoEditorCore -ne $ExpectedMonacoEditorCoreVersion) {
        Fail "Eclipse Theia $ExpectedTheiaVersion requires NovaOryn's direct Monaco editor-core pin $ExpectedMonacoEditorCoreVersion; found $actualMonacoEditorCore."
    }

    $actualElectron = [string]$electronPackage.devDependencies.electron
    if ($actualElectron -ne $ExpectedElectronVersion) {
        Fail "Eclipse Theia $ExpectedTheiaVersion requires Electron $ExpectedElectronVersion; found $actualElectron."
    }

    $actualWindowsCaCerts = [string]$electronPackage.dependencies.'@vscode/windows-ca-certs'
    if ($actualWindowsCaCerts -ne $ExpectedWindowsCaCertsVersion) {
        Fail "NovaOryn IDE requires @vscode/windows-ca-certs $ExpectedWindowsCaCertsVersion; found $actualWindowsCaCerts."
    }

    foreach ($packageName in $ForbiddenRuntimePackages) {
        if ($electronPackage.dependencies.PSObject.Properties.Name -contains $packageName) {
            Fail "$packageName must not be a production dependency in 0.10.3. VS Code/Open VSX plugin loading is temporarily disabled to keep the vulnerable decompress chain out of the shipped runtime."
        }
    }

    Ok "NovaOryn IDE package versions are internally consistent at $ExpectedIdeVersion."
    Ok "Eclipse Theia $ExpectedTheiaVersion / Electron $ExpectedElectronVersion compatibility pair verified."
    Ok "Monaco editor core $ExpectedMonacoEditorCoreVersion syntax-highlighting dependency verified."
    Ok "Windows CA certificate dependency @vscode/windows-ca-certs $ExpectedWindowsCaCertsVersion verified."
    if ([string]$extensionPackage.dependencies.'@theia/debug' -ne $ExpectedTheiaVersion -or [string]$electronPackage.dependencies.'@theia/debug' -ne $ExpectedTheiaVersion) {
        Fail "NovaOryn source breakpoints require @theia/debug $ExpectedTheiaVersion in both the extension and Electron application."
    }
    Ok "Theia native debugger/breakpoint UI $ExpectedTheiaVersion verified."
    Ok 'VS Code/Open VSX runtime plugin packages are excluded from the shipped dependency set.'
    if ([string]$extensionPackage.dependencies.inversify -ne '6.2.2') {
        Fail "NovaOryn IDE requires inversify 6.2.2 for Theia 1.74.0 DI compatibility; found $($extensionPackage.dependencies.inversify)."
    }
    Ok 'Inversify 6.2.2 compatibility dependency verified.'
    if ([string]$extensionPackage.dependencies.'@lumino/widgets' -ne '2.7.5') {
        Fail "NovaOryn IDE requires @lumino/widgets 2.7.5 to match Theia 1.74.0; found $($extensionPackage.dependencies.'@lumino/widgets')."
    }
    if ([string]$extensionPackage.dependencies.'@lumino/messaging' -ne '^2.0.4') {
        Fail "NovaOryn IDE requires @lumino/messaging ^2.0.4 to match Theia 1.74.0; found $($extensionPackage.dependencies.'@lumino/messaging')."
    }
    Ok 'Lumino Widget 2.7.5 / Messaging 2.x compatibility dependencies verified.'
    $requiredRootCliPackages = @(
        '@theia/application-manager','@theia/cli','@theia/core','@theia/editor','@theia/electron',
        '@theia/debug','@theia/filesystem','@theia/monaco','@theia/navigator','@theia/output','@theia/preferences',
        '@theia/process','@theia/search-in-workspace','@theia/task','@theia/terminal','@theia/workspace'
    )
    foreach ($packageName in $requiredRootCliPackages) {
        if ([string]$rootPackage.devDependencies.$packageName -ne $ExpectedTheiaVersion) {
            Fail "Root Theia CLI build surface requires $packageName $ExpectedTheiaVersion; found $($rootPackage.devDependencies.$packageName)."
        }
    }
    if ([string]$rootPackage.devDependencies.electron -ne $ExpectedElectronVersion) {
        Fail "Root Theia CLI build surface requires Electron $ExpectedElectronVersion; found $($rootPackage.devDependencies.electron)."
    }
    Ok 'Root Theia CLI/runtime build surface is explicitly pinned and hoist-independent.'
    exit 0
} catch {
    Fail $_.Exception.Message
}

