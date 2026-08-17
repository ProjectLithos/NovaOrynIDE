[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceManifest = Join-Path $root "src\NovaOryn.VisualStudio\source.extension.vsixmanifest"

if (-not (Test-Path -LiteralPath $sourceManifest -PathType Leaf)) {
    throw "VSIX source manifest was not found: $sourceManifest"
}
[xml]$sourceManifestXml = Get-Content -LiteralPath $sourceManifest -Raw
$identity = $sourceManifestXml.PackageManifest.Metadata.Identity
$extensionId = [string]$identity.Id
$version = [string]$identity.Version
if ([string]::IsNullOrWhiteSpace($extensionId)) { throw "VSIX source manifest does not define Metadata/Identity/@Id." }
if ([string]::IsNullOrWhiteSpace($version)) { throw "VSIX source manifest does not define Metadata/Identity/@Version." }

& (Join-Path $root "Build-NovaOrynVSIX.ps1") -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "NovaOryn VSIX build failed with exit code $LASTEXITCODE." }

$vsix = Join-Path $root "Artifacts\VisualStudio\NovaOryn.VisualStudio-$version.vsix"
if (-not (Test-Path -LiteralPath $vsix -PathType Leaf)) { throw "NovaOryn VSIX was not produced: $vsix" }

$templatePackageRoot = Join-Path $root "Artifacts\VisualStudio\ProjectTemplates"
$templatePackages = @(Get-ChildItem -LiteralPath $templatePackageRoot -Filter *.zip -File | Sort-Object Name)
$expectedTemplateNames = @(
    "NovaOrynKernel.zip",
    "NovaOrynKernelDriver.zip",
    "NovaOrynKernelLibrary.zip",
    "NovaOrynFilesystemFatFs.zip",
    "NovaOrynTestProject.zip",
    "NovaOrynUserlandApplication.zip",
    "NovaOrynUserlandDriver.zip",
    "NovaOrynUserlandLibrary.zip",
    "NovaOrynUserlandService.zip"
)
if ($templatePackages.Count -ne $expectedTemplateNames.Count) {
    throw "NovaOryn produced $($templatePackages.Count) project-template packages; expected $($expectedTemplateNames.Count)."
}
foreach ($expected in $expectedTemplateNames) {
    if (-not (Test-Path -LiteralPath (Join-Path $templatePackageRoot $expected) -PathType Leaf)) {
        throw "NovaOryn project-template package is missing: $expected"
    }
}

$runningVisualStudio = @(Get-Process -Name devenv -ErrorAction SilentlyContinue)
if ($runningVisualStudio.Count -gt 0) {
    throw "Visual Studio is running. Save your work, close every Visual Studio window, and run Install-NovaOrynVSIX.bat again."
}

$visualStudioInstances = @()
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path -LiteralPath $vswhere -PathType Leaf) {
    try {
        $instanceJson = & $vswhere -products * -requires Microsoft.VisualStudio.Component.CoreEditor -format json -utf8
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace(($instanceJson -join "`n"))) {
            $visualStudioInstances = @(($instanceJson -join "`n") | ConvertFrom-Json)
        }
    } catch {
        Write-Warning "vswhere could not enumerate Visual Studio instances: $($_.Exception.Message)"
    }
}
$visualStudioInstances = @($visualStudioInstances | Where-Object {
    $candidatePath = [string]$_.installationPath
    -not [string]::IsNullOrWhiteSpace($candidatePath) -and
    (Test-Path -LiteralPath (Join-Path $candidatePath "Common7\IDE\devenv.exe") -PathType Leaf)
})

if ($visualStudioInstances.Count -eq 0) {
    $fallbackRoots = @(
        (Join-Path $env:ProgramFiles "Microsoft Visual Studio\18"),
        (Join-Path $env:ProgramFiles "Microsoft Visual Studio\2026"),
        (Join-Path $env:ProgramFiles "Microsoft Visual Studio\2022")
    ) | Where-Object { Test-Path -LiteralPath $_ -PathType Container }

    foreach ($fallbackRoot in $fallbackRoots) {
        $devenvCandidate = Get-ChildItem -LiteralPath $fallbackRoot -Filter devenv.exe -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match '[\\/]Common7[\\/]IDE[\\/]devenv\.exe$' } |
            Select-Object -First 1
        if ($null -ne $devenvCandidate) {
            $visualStudioInstances += [pscustomobject]@{
                installationPath = (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $devenvCandidate.FullName)))
                installationVersion = if ($fallbackRoot -match '[\\/]18$|[\\/]2026$') { "18.0.0.0" } else { "17.0.0.0" }
                displayName = Split-Path -Leaf $fallbackRoot
            }
        }
    }
}
if ($visualStudioInstances.Count -eq 0) { throw "No supported Visual Studio installation was found." }

$selectedInstance = $visualStudioInstances |
    Sort-Object -Property @{ Expression = {
        try { [Version][string]$_.installationVersion } catch { [Version]"0.0" }
    }; Descending = $true } |
    Select-Object -First 1

$installationPath = [string]$selectedInstance.installationPath
if ([string]::IsNullOrWhiteSpace($installationPath) -or -not (Test-Path -LiteralPath $installationPath -PathType Container)) {
    throw "The selected Visual Studio instance has an invalid installation path: $installationPath"
}

$ideDirectory = Join-Path $installationPath "Common7\IDE"
$installer = Join-Path $ideDirectory "VSIXInstaller.exe"
$devenv = Join-Path $ideDirectory "devenv.exe"
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) {
    $sharedInstaller = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\resources\app\ServiceHub\Services\Microsoft.VisualStudio.Setup.Service\VSIXInstaller.exe"
    if (Test-Path -LiteralPath $sharedInstaller -PathType Leaf) {
        $installer = $sharedInstaller
    } else {
        throw "VSIXInstaller.exe was not found for the selected Visual Studio instance: $installationPath"
    }
}

$selectedVersion = try { [Version][string]$selectedInstance.installationVersion } catch { [Version]"0.0" }
Write-Host "[INFO] Selected Visual Studio: $([string]$selectedInstance.displayName) $selectedVersion"
Write-Host "[INFO] Visual Studio path: $installationPath"
Write-Host "[INFO] VSIX installer: $installer"
Write-Host "[INFO] Installing or upgrading $extensionId $version."

$install = Start-Process -FilePath $installer -ArgumentList @('/quiet', '/force', $vsix) -Wait -PassThru
if ($install.ExitCode -eq -1073741510) {
    Write-Warning "Quiet VSIX installation was interrupted (Windows status 0xC000013A). Retrying with the installer UI."
    $install = Start-Process -FilePath $installer -ArgumentList @('/force', $vsix) -Wait -PassThru
}
if ($install.ExitCode -ne 0) {
    throw "VSIX installation failed with exit code $($install.ExitCode). VSIXInstaller logs are normally written under $env:TEMP."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$visualStudioFolderName = if ($selectedVersion.Major -ge 18) { "Visual Studio 18" } else { "Visual Studio 2022" }
$documents = [Environment]::GetFolderPath([Environment+SpecialFolder]::MyDocuments)
if ([string]::IsNullOrWhiteSpace($documents)) {
    throw "Unable to resolve the current user's Documents directory for Visual Studio templates."
}
$userTemplateRoot = Join-Path $documents "$visualStudioFolderName\Templates\ProjectTemplates"
New-Item -ItemType Directory -Path $userTemplateRoot -Force | Out-Null

# Remove the pre-NovaOryn brand and every older NovaOryn template package so the
# user template catalog has one copy of each current independent template.
$existingTemplateArchives = @(Get-ChildItem -LiteralPath $userTemplateRoot -Filter *.zip -File -Recurse -ErrorAction SilentlyContinue)
foreach ($archiveFile in $existingTemplateArchives) {
    $remove = $archiveFile.Name.StartsWith("NovaOryn", [StringComparison]::OrdinalIgnoreCase)
    if (-not $remove) {
        try {
            $legacyZip = [IO.Compression.ZipFile]::OpenRead($archiveFile.FullName)
            try {
                foreach ($entry in $legacyZip.Entries) {
                    if (-not $entry.FullName.EndsWith(".vstemplate", [StringComparison]::OrdinalIgnoreCase)) { continue }
                    $reader = [IO.StreamReader]::new($entry.Open())
                    try { $templateText = $reader.ReadToEnd() } finally { $reader.Dispose() }
                    if ($templateText.IndexOf("<Name>Oryn OS Project", [StringComparison]::OrdinalIgnoreCase) -ge 0 -or
                        $templateText.IndexOf("<TemplateID>Oryn.", [StringComparison]::OrdinalIgnoreCase) -ge 0) {
                        $remove = $true
                        break
                    }
                }
            } finally { $legacyZip.Dispose() }
        } catch { }
    }
    if ($remove) {
        Write-Host "[INFO] Removing obsolete project template: $($archiveFile.FullName)"
        Remove-Item -LiteralPath $archiveFile.FullName -Force
    }
}

$installedNames = [Collections.Generic.List[string]]::new()
foreach ($templatePackage in $templatePackages) {
    $destination = Join-Path $userTemplateRoot $templatePackage.Name
    Copy-Item -LiteralPath $templatePackage.FullName -Destination $destination -Force
    if (-not (Test-Path -LiteralPath $destination -PathType Leaf)) {
        throw "NovaOryn project template was not copied to the Visual Studio user template root: $destination"
    }

    $templateArchive = [IO.Compression.ZipFile]::OpenRead($destination)
    try {
        $rootTemplates = @($templateArchive.Entries | Where-Object {
            $normalized = $_.FullName.Replace("\", "/")
            $normalized.EndsWith(".vstemplate", [StringComparison]::OrdinalIgnoreCase) -and $normalized.IndexOf("/") -lt 0
        })
        if ($rootTemplates.Count -ne 1) {
            throw "Installed $($templatePackage.Name) must contain exactly one root .vstemplate; found $($rootTemplates.Count)."
        }

        $reader = [IO.StreamReader]::new($rootTemplates[0].Open())
        try { [xml]$templateXml = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $ns = New-Object System.Xml.XmlNamespaceManager($templateXml.NameTable)
        $ns.AddNamespace("vst", "http://schemas.microsoft.com/developer/vstemplate/2005")
        if ([string]$templateXml.VSTemplate.Type -ne "Project") {
            throw "Installed $($templatePackage.Name) is not a normal Visual Studio Project template."
        }

        $nameNode = $templateXml.SelectSingleNode("/vst:VSTemplate/vst:TemplateData/vst:Name", $ns)
        if ($null -eq $nameNode -or -not ([string]$nameNode.InnerText).StartsWith("NovaOryn", [StringComparison]::Ordinal)) {
            throw "Installed $($templatePackage.Name) does not expose a NovaOryn template name."
        }
        $hiddenNode = $templateXml.SelectSingleNode("/vst:VSTemplate/vst:TemplateData/vst:Hidden", $ns)
        if ($null -ne $hiddenNode -and [string]$hiddenNode.InnerText -eq "true") {
            throw "Installed $($templatePackage.Name) is hidden."
        }

        $installedNames.Add([string]$nameNode.InnerText)
    } finally {
        $templateArchive.Dispose()
    }

    $hash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    Write-Host "[ OK ] Project template installed: $destination"
    Write-Host "[ OK ] Template SHA256: $hash"
}

$majorPrefix = "$($selectedVersion.Major).0_*"
$cacheRoots = @(
    (Join-Path $env:APPDATA "Microsoft\VisualStudio"),
    (Join-Path $env:LOCALAPPDATA "Microsoft\VisualStudio")
) | Select-Object -Unique

foreach ($cacheRoot in $cacheRoots) {
    if (-not (Test-Path -LiteralPath $cacheRoot -PathType Container)) { continue }
    foreach ($instanceCache in Get-ChildItem -LiteralPath $cacheRoot -Directory -Filter $majorPrefix -ErrorAction SilentlyContinue) {
        foreach ($cacheName in @("ProjectTemplatesCache", "ItemTemplatesCache", "ComponentModelCache")) {
            $cachePath = Join-Path $instanceCache.FullName $cacheName
            if (Test-Path -LiteralPath $cachePath -PathType Container) {
                Write-Host "[INFO] Clearing Visual Studio cache: $cachePath"
                Remove-Item -LiteralPath $cachePath -Recurse -Force
            }
        }
        $installedTemplatesJson = Join-Path $instanceCache.FullName "InstalledTemplates.json"
        if (Test-Path -LiteralPath $installedTemplatesJson -PathType Leaf) {
            Write-Host "[INFO] Removing stale template catalogue: $installedTemplatesJson"
            Remove-Item -LiteralPath $installedTemplatesJson -Force
        }
    }
}

if (-not (Test-Path -LiteralPath $devenv -PathType Leaf)) {
    throw "devenv.exe was not found for the selected Visual Studio instance: $installationPath"
}

Write-Host "[INFO] Updating Visual Studio extension/template registration: $devenv /updateconfiguration"
$updateConfiguration = Start-Process -FilePath $devenv -ArgumentList @('/updateconfiguration') -Wait -PassThru
if ($updateConfiguration.ExitCode -ne 0) {
    throw "Visual Studio /updateconfiguration failed with exit code $($updateConfiguration.ExitCode)."
}
Write-Host "[ OK ] Visual Studio extension/template registration updated."

Write-Host "[INFO] Rebuilding Visual Studio project-template catalogue: $devenv /installvstemplates"
$templateCache = Start-Process -FilePath $devenv -ArgumentList @('/installvstemplates') -Wait -PassThru
if ($templateCache.ExitCode -ne 0) {
    throw "Visual Studio template-catalogue rebuild failed with exit code $($templateCache.ExitCode)."
}
Write-Host "[ OK ] Visual Studio project-template catalogue rebuilt."

Write-Host "[ OK ] $extensionId $version is installed for $([string]$selectedInstance.displayName)."
Write-Host "[ OK ] User template root used: $userTemplateRoot"
Write-Host "[ OK ] Installed NovaOryn templates:"
foreach ($name in $installedNames | Sort-Object) { Write-Host "       $name" }
Write-Host "[ OK ] Start Visual Studio, choose Create a new project or Add > New Project, and search for: NovaOryn"
