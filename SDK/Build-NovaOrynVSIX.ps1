[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# NovaOryn pins its own .NET SDK. Visual Studio MSBuild is required for VSSDK,
# but its SDK resolver must use the repository-pinned dotnet installation.
$globalJsonPath = Join-Path $root "global.json"
if (-not (Test-Path -LiteralPath $globalJsonPath -PathType Leaf)) {
    throw "NovaOryn global.json was not found: $globalJsonPath"
}
$globalJson = Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json
$requiredSdkVersion = [string]$globalJson.sdk.version
if ([string]::IsNullOrWhiteSpace($requiredSdkVersion)) {
    throw "NovaOryn global.json does not define sdk.version."
}

$dotnetRoot = Join-Path $root ".toolchain\DotNet"
$dotnet = Join-Path $dotnetRoot "dotnet.exe"
$requiredSdkRoot = Join-Path $dotnetRoot "sdk\$requiredSdkVersion"
$requiredSdkSdks = Join-Path $requiredSdkRoot "Sdks"

if (-not (Test-Path -LiteralPath $dotnet -PathType Leaf)) {
    throw "Repository-pinned dotnet.exe was not found: $dotnet. Run Install-NovaOrynToolchain.bat first."
}
if (-not (Test-Path -LiteralPath $requiredSdkRoot -PathType Container)) {
    throw "Repository-pinned .NET SDK $requiredSdkVersion was not found at: $requiredSdkRoot. Run Install-NovaOrynToolchain.bat first."
}
if (-not (Test-Path -LiteralPath (Join-Path $requiredSdkSdks "Microsoft.NET.Sdk\Sdk") -PathType Container)) {
    throw "Repository-pinned Microsoft.NET.Sdk payload was not found under: $requiredSdkSdks"
}

$installedPinnedSdks = @(& $dotnet --list-sdks)
if ($LASTEXITCODE -ne 0) { throw "Repository-pinned dotnet.exe failed while enumerating SDKs." }
$sdkFound = $false
foreach ($sdkLine in $installedPinnedSdks) {
    if ([string]$sdkLine -match ("^" + [Regex]::Escape($requiredSdkVersion) + "\s+\[")) {
        $sdkFound = $true
        break
    }
}
if (-not $sdkFound) {
    throw "Repository-pinned dotnet installation does not report required SDK $requiredSdkVersion. Run Install-NovaOrynToolchain.bat first."
}

Write-Host "[INFO] NovaOryn pinned dotnet     : $dotnet"
Write-Host "[INFO] NovaOryn pinned .NET SDK   : $requiredSdkVersion"
Write-Host "[INFO] NovaOryn pinned SDK payload: $requiredSdkRoot"

$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path -LiteralPath $vswhere -PathType Leaf)) {
    throw "vswhere.exe was not found. Install Visual Studio or the Visual Studio Installer."
}
$installationPath = (& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($installationPath)) {
    throw "No Visual Studio installation containing MSBuild was found."
}
$msbuild = Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe"
if (-not (Test-Path -LiteralPath $msbuild -PathType Leaf)) {
    throw "Visual Studio MSBuild.exe was not found: $msbuild"
}
Write-Host "[INFO] VSIX build Visual Studio: $installationPath"
Write-Host "[INFO] VSIX build MSBuild      : $msbuild"

$projectDirectory = Join-Path $root "src\NovaOryn.VisualStudio"
$project = Join-Path $projectDirectory "NovaOryn.VisualStudio.csproj"
$sourceManifest = Join-Path $projectDirectory "source.extension.vsixmanifest"
if (-not (Test-Path -LiteralPath $sourceManifest -PathType Leaf)) { throw "VSIX source manifest was not found: $sourceManifest" }

[xml]$sourceManifestXml = Get-Content -LiteralPath $sourceManifest -Raw
$expectedVersion = [string]$sourceManifestXml.PackageManifest.Metadata.Identity.Version
if ([string]::IsNullOrWhiteSpace($expectedVersion)) { throw "VSIX source manifest does not define Metadata/Identity/@Version." }

$templateRoot = Join-Path $projectDirectory "ProjectTemplates\CSharp\1033"
$packageRoot = Join-Path $root "Artifacts\VisualStudio\ProjectTemplates"

$templateDefinitions = @(
    [pscustomobject]@{ Id = "NovaOrynKernel"; Name = "NovaOryn Kernel"; TemplateId = "NovaOryn.Project.Kernel" },
    [pscustomobject]@{ Id = "NovaOrynKernelDriver"; Name = "NovaOryn Kernel Driver"; TemplateId = "NovaOryn.Project.KernelDriver" },
    [pscustomobject]@{ Id = "NovaOrynKernelLibrary"; Name = "NovaOryn Kernel Library"; TemplateId = "NovaOryn.Project.KernelLibrary" },
    [pscustomobject]@{ Id = "NovaOrynFilesystemFatFs"; Name = "NovaOryn Filesystem - FatFs"; TemplateId = "NovaOryn.Project.FileSystem.FatFs" },
    [pscustomobject]@{ Id = "NovaOrynUserlandApplication"; Name = "NovaOryn Userland Application"; TemplateId = "NovaOryn.Project.UserlandApplication" },
    [pscustomobject]@{ Id = "NovaOrynUserlandService"; Name = "NovaOryn Userland Service"; TemplateId = "NovaOryn.Project.UserlandService" },
    [pscustomobject]@{ Id = "NovaOrynUserlandDriver"; Name = "NovaOryn Userland Driver"; TemplateId = "NovaOryn.Project.UserlandDriver" },
    [pscustomobject]@{ Id = "NovaOrynUserlandLibrary"; Name = "NovaOryn Userland Library"; TemplateId = "NovaOryn.Project.UserlandLibrary" },
    [pscustomobject]@{ Id = "NovaOrynTestProject"; Name = "NovaOryn Test Project"; TemplateId = "NovaOryn.Project.Test" }
)

# A ChangedFiles update can overlay an older source tree. Remove only obsolete
# multi-project-template artifacts that must never be packaged again.
$obsoleteTemplatePaths = @(
    (Join-Path $projectDirectory "ProjectTemplates\NovaOryn.VisualStudio.Project.vstman"),
    (Join-Path $projectDirectory "ProjectTemplates\NovaOrynKernel.zip"),
    (Join-Path $templateRoot "NovaOrynKernel\KernelProject")
)
foreach ($obsoletePath in $obsoleteTemplatePaths) {
    if (Test-Path -LiteralPath $obsoletePath) {
        Write-Host "[INFO] Removing obsolete multi-project template artifact: $obsoletePath"
        Remove-Item -LiteralPath $obsoletePath -Recurse -Force
    }
}

if (Test-Path -LiteralPath $packageRoot) { Remove-Item -LiteralPath $packageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem

$templateIds = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($definition in $templateDefinitions) {
    $sourceDirectory = Join-Path $templateRoot $definition.Id
    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
        throw "NovaOryn template source directory was not found: $sourceDirectory"
    }

    $rootTemplates = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter *.vstemplate -File)
    if ($rootTemplates.Count -ne 1) {
        throw "$($definition.Id) must contain exactly one .vstemplate at its ZIP root; found $($rootTemplates.Count)."
    }

    [xml]$templateXml = Get-Content -LiteralPath $rootTemplates[0].FullName -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($templateXml.NameTable)
    $ns.AddNamespace("vst", "http://schemas.microsoft.com/developer/vstemplate/2005")

    if ([string]$templateXml.VSTemplate.Type -ne "Project") {
        throw "$($definition.Id) must be a normal Type='Project' template, not '$([string]$templateXml.VSTemplate.Type)'."
    }

    $nameNode = $templateXml.SelectSingleNode("/vst:VSTemplate/vst:TemplateData/vst:Name", $ns)
    if ($null -eq $nameNode -or [string]$nameNode.InnerText -ne [string]$definition.Name) {
        throw "$($definition.Id) template name must be '$($definition.Name)'."
    }

    $hiddenNode = $templateXml.SelectSingleNode("/vst:VSTemplate/vst:TemplateData/vst:Hidden", $ns)
    if ($null -ne $hiddenNode -and [string]$hiddenNode.InnerText -eq "true") {
        throw "$($definition.Id) is hidden; every independent NovaOryn project template must be visible."
    }

    $idNode = $templateXml.SelectSingleNode("/vst:VSTemplate/vst:TemplateData/vst:TemplateID", $ns)
    if ($null -eq $idNode -or [string]$idNode.InnerText -ne [string]$definition.TemplateId) {
        throw "$($definition.Id) must define unique TemplateID '$($definition.TemplateId)'."
    }
    if (-not $templateIds.Add([string]$idNode.InnerText)) {
        throw "Duplicate Visual Studio template ID: $([string]$idNode.InnerText)"
    }

    $projectNode = $templateXml.SelectSingleNode("/vst:VSTemplate/vst:TemplateContent/vst:Project", $ns)
    if ($null -eq $projectNode) { throw "$($definition.Id) does not contain TemplateContent/Project." }
    $projectFile = [string]$projectNode.File
    if ([string]::IsNullOrWhiteSpace($projectFile)) { throw "$($definition.Id) project template does not define Project/@File." }
    if (-not (Test-Path -LiteralPath (Join-Path $sourceDirectory $projectFile) -PathType Leaf)) {
        throw "$($definition.Id) project file is missing: $projectFile"
    }

    # Build from a disposable staging tree. The primary Project/@File remains a
    # real .csproj. Any additional nested .csproj is carried in the template ZIP
    # under a neutral ".template" suffix, while ProjectItem/@TargetFileName restores
    # the intended .csproj filename when Visual Studio instantiates the template.
    $stagingParent = Join-Path $packageRoot "_staging"
    $stagingDirectory = Join-Path $stagingParent $definition.Id
    if (Test-Path -LiteralPath $stagingDirectory) {
        Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $stagingDirectory -Force | Out-Null

    Get-ChildItem -LiteralPath $sourceDirectory -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $stagingDirectory -Recurse -Force
    }

    $stagedTemplatePath = Join-Path $stagingDirectory $rootTemplates[0].Name
    [xml]$stagedTemplateXml = Get-Content -LiteralPath $stagedTemplatePath -Raw
    $stagedNamespace = New-Object System.Xml.XmlNamespaceManager($stagedTemplateXml.NameTable)
    $stagedNamespace.AddNamespace("vst", "http://schemas.microsoft.com/developer/vstemplate/2005")

    $stagedProjectNode = $stagedTemplateXml.SelectSingleNode(
        "/vst:VSTemplate/vst:TemplateContent/vst:Project",
        $stagedNamespace)
    if ($null -eq $stagedProjectNode) {
        throw "$($definition.Id) staging template does not contain TemplateContent/Project."
    }

    $primaryProjectPath = Join-Path $stagingDirectory $projectFile
    $nestedProjectDescriptors = @(Get-ChildItem -LiteralPath $stagingDirectory -Filter *.csproj -File -Recurse |
        Where-Object {
            -not [string]::Equals(
                $_.FullName,
                $primaryProjectPath,
                [StringComparison]::OrdinalIgnoreCase)
        })

    $surrogateMappings = @()
    foreach ($nestedProjectDescriptor in $nestedProjectDescriptors) {
        $relativeProjectPath = $nestedProjectDescriptor.FullName.Substring($stagingDirectory.Length).TrimStart("\")
        $normalizedRelativeProjectPath = $relativeProjectPath.Replace("/", "\")
        $surrogateRelativePath = $normalizedRelativeProjectPath + ".template"
        $surrogateFullPath = Join-Path $stagingDirectory $surrogateRelativePath

        $matchingProjectItems = @($stagedTemplateXml.SelectNodes(
            "/vst:VSTemplate/vst:TemplateContent/vst:Project/vst:ProjectItem",
            $stagedNamespace) | Where-Object {
                ([string]$_.InnerText).Replace("/", "\") -eq $normalizedRelativeProjectPath
            })

        if ($matchingProjectItems.Count -ne 1) {
            throw "$($definition.Id) nested project descriptor '$normalizedRelativeProjectPath' must have exactly one ProjectItem entry; found $($matchingProjectItems.Count)."
        }

        $projectItem = $matchingProjectItems[0]
        $projectItem.InnerText = $surrogateRelativePath

        $targetFileName = [string]$projectItem.GetAttribute("TargetFileName")
        if ([string]::IsNullOrWhiteSpace($targetFileName)) {
            [void]$projectItem.SetAttribute("TargetFileName", $normalizedRelativeProjectPath)
        } elseif ($targetFileName.Replace("/", "\") -ne $normalizedRelativeProjectPath) {
            throw "$($definition.Id) nested project descriptor '$normalizedRelativeProjectPath' has unexpected TargetFileName '$targetFileName'."
        }

        Move-Item -LiteralPath $nestedProjectDescriptor.FullName -Destination $surrogateFullPath -Force

        $surrogateMappings += [pscustomobject]@{
            Target = $normalizedRelativeProjectPath
            Source = $surrogateRelativePath
        }
    }

    $stagedTemplateXml.Save($stagedTemplatePath)

    $zipPath = Join-Path $packageRoot ($definition.Id + ".zip")
    [IO.Compression.ZipFile]::CreateFromDirectory(
        $stagingDirectory,
        $zipPath,
        [IO.Compression.CompressionLevel]::Optimal,
        $false)

    $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $zipRootTemplates = @($archive.Entries | Where-Object {
            $normalized = $_.FullName.Replace("\", "/")
            $normalized.EndsWith(".vstemplate", [StringComparison]::OrdinalIgnoreCase) -and $normalized.IndexOf("/") -lt 0
        })
        if ($zipRootTemplates.Count -ne 1) {
            throw "$($definition.Id).zip must contain exactly one root .vstemplate; found $($zipRootTemplates.Count)."
        }

        $projectEntry = $archive.Entries | Where-Object {
            $_.FullName.Replace("\", "/") -eq $projectFile.Replace("\", "/")
        } | Select-Object -First 1
        if ($null -eq $projectEntry) {
            throw "$($definition.Id).zip is missing primary project file '$projectFile'."
        }

        $rawProjectEntries = @($archive.Entries | Where-Object {
            $_.FullName.EndsWith(".csproj", [StringComparison]::OrdinalIgnoreCase)
        })
        if ($rawProjectEntries.Count -ne 1 -or
            $rawProjectEntries[0].FullName.Replace("\", "/") -ne $projectFile.Replace("\", "/")) {
            $rawNames = ($rawProjectEntries | ForEach-Object { $_.FullName }) -join ", "
            throw "$($definition.Id).zip must contain exactly one raw .csproj (the primary project). Found: $rawNames"
        }

        foreach ($mapping in $surrogateMappings) {
            $surrogateEntry = $archive.Entries | Where-Object {
                $_.FullName.Replace("\", "/") -eq $mapping.Source.Replace("\", "/")
            } | Select-Object -First 1
            if ($null -eq $surrogateEntry -or $surrogateEntry.Length -le 0) {
                throw "$($definition.Id).zip is missing neutral nested-project payload '$($mapping.Source)'."
            }
        }

        $stagedTemplateEntry = $archive.Entries | Where-Object {
            $_.FullName.Replace("\", "/") -eq $rootTemplates[0].Name
        } | Select-Object -First 1
        if ($null -eq $stagedTemplateEntry) {
            throw "$($definition.Id).zip is missing its root .vstemplate."
        }

        $reader = [IO.StreamReader]::new($stagedTemplateEntry.Open())
        try {
            [xml]$zipTemplateXml = $reader.ReadToEnd()
        } finally {
            $reader.Dispose()
        }

        $zipNs = New-Object System.Xml.XmlNamespaceManager($zipTemplateXml.NameTable)
        $zipNs.AddNamespace("vst", "http://schemas.microsoft.com/developer/vstemplate/2005")
        $zipProjectItems = @($zipTemplateXml.SelectNodes(
            "/vst:VSTemplate/vst:TemplateContent/vst:Project/vst:ProjectItem",
            $zipNs))

        foreach ($mapping in $surrogateMappings) {
            $matches = @($zipProjectItems | Where-Object {
                ([string]$_.InnerText).Replace("/", "\") -eq $mapping.Source -and
                ([string]$_.GetAttribute("TargetFileName")).Replace("/", "\") -eq $mapping.Target
            })
            if ($matches.Count -ne 1) {
                throw "$($definition.Id).zip does not restore '$($mapping.Source)' to '$($mapping.Target)' exactly once."
            }
        }
    } finally {
        $archive.Dispose()
        if (Test-Path -LiteralPath $stagingDirectory) {
            Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
        }
    }

    if ($surrogateMappings.Count -gt 0) {
        Write-Host "[ OK ] Neutral nested project descriptors: $($surrogateMappings.Count)"
    }
    Write-Host "[ OK ] Project template: $($definition.Name) -> $zipPath"
}

# The main kernel template must now be one ordinary project whose copied Userland
# tree contains separately compiled child projects. KernelProjects is linked by wildcard.
$kernelSource = Join-Path $templateRoot "NovaOrynKernel"
foreach ($required in @(
    "NovaOrynKernel.csproj",
    "NovaOrynProject.json",
    "Kernel\Kernel.cs",
    "Boot\BootStartup.cs",
    "Boot\KernelPanicTransport.cs",
    "HAL\HardwareAbstractionLayer.cs",
    "Userland\Commands\NovaOryn.Userland.Commands.csproj",
    "Userland\Drivers\NovaOryn.Userland.Drivers.csproj",
    "KernelProjects\README.md",
    "Build-WorkspaceProjects.ps1"
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $kernelSource $required) -PathType Leaf)) {
        throw "NovaOryn Kernel template is missing workspace file: $required"
    }
}
$kernelNestedProjectFiles = @(Get-ChildItem -LiteralPath $kernelSource -Filter *.csproj -File -Recurse |
    Where-Object {
        -not [string]::Equals(
            $_.FullName,
            (Join-Path $kernelSource "NovaOrynKernel.csproj"),
            [StringComparison]::OrdinalIgnoreCase)
    })
if ($kernelNestedProjectFiles.Count -lt 1) {
    throw "NovaOryn Kernel template must contain nested SDK/Userland project descriptors."
}
Write-Host "[ OK ] Kernel template nested project descriptors present in source: $($kernelNestedProjectFiles.Count)"
$kernelProjectText = Get-Content -LiteralPath (Join-Path $kernelSource "NovaOrynKernel.csproj") -Raw
if ($kernelProjectText.IndexOf("<NovaOrynProjectType>Kernel</NovaOrynProjectType>", [StringComparison]::Ordinal) -lt 0) {
    throw "NovaOryn Kernel project must identify itself with <NovaOrynProjectType>Kernel</NovaOrynProjectType>."
}
if ($kernelProjectText.IndexOf('KernelProjects\**\*.csproj', [StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw "NovaOryn Kernel project must not auto-reference KernelProjects\**\*.csproj; NovaOryn.Configuration must be authoritative."
}
if ($kernelProjectText.IndexOf('@(NovaOrynConfiguredKernelProject)', [StringComparison]::Ordinal) -lt 0) {
    throw "NovaOryn Kernel project must reference @(NovaOrynConfiguredKernelProject) so the generated configuration controls kernel project inclusion."
}
$configurationPropsPath = Join-Path $kernelSource "NovaOryn.Configuration.props"
$configurationTargetsPath = Join-Path $kernelSource "NovaOryn.Configuration.targets"
if (-not (Test-Path -LiteralPath $configurationPropsPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $configurationTargetsPath -PathType Leaf)) {
    throw "NovaOryn Kernel template must contain NovaOryn.Configuration.props and NovaOryn.Configuration.targets."
}
Write-Host "[ OK ] Kernel template uses authoritative configuration-driven project references."

$msbuildArguments = @(
    $project,
    '/nologo',
    '/m',
    '/restore',
    '/t:Rebuild',
    "/p:Configuration=$Configuration",
    '/p:Platform=AnyCPU',
    '/p:RestoreIgnoreFailedSources=false',
    '/verbosity:minimal'
)

$previousEnvironment = @{
    DOTNET_ROOT = $env:DOTNET_ROOT
    DOTNET_HOST_PATH = $env:DOTNET_HOST_PATH
    DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR
    DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = $env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR
    DOTNET_MULTILEVEL_LOOKUP = $env:DOTNET_MULTILEVEL_LOOKUP
    MSBuildSDKsPath = $env:MSBuildSDKsPath
    PATH = $env:PATH
}

try {
    $env:DOTNET_ROOT = $dotnetRoot
    $env:DOTNET_HOST_PATH = $dotnet
    $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $dotnetRoot
    $env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = $requiredSdkSdks
    $env:DOTNET_MULTILEVEL_LOOKUP = "0"
    $env:MSBuildSDKsPath = $requiredSdkSdks
    $env:PATH = "$dotnetRoot;$($previousEnvironment.PATH)"

    Write-Host "[INFO] MSBuild SDK resolver root   : $dotnetRoot"
    Write-Host "[INFO] MSBuild SDKs path          : $requiredSdkSdks"

    & $msbuild @msbuildArguments
    $msbuildExitCode = $LASTEXITCODE
} finally {
    $env:DOTNET_ROOT = $previousEnvironment.DOTNET_ROOT
    $env:DOTNET_HOST_PATH = $previousEnvironment.DOTNET_HOST_PATH
    $env:DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR = $previousEnvironment.DOTNET_MSBUILD_SDK_RESOLVER_CLI_DIR
    $env:DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR = $previousEnvironment.DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR
    $env:DOTNET_MULTILEVEL_LOOKUP = $previousEnvironment.DOTNET_MULTILEVEL_LOOKUP
    $env:MSBuildSDKsPath = $previousEnvironment.MSBuildSDKsPath
    $env:PATH = $previousEnvironment.PATH
}
if ($msbuildExitCode -ne 0) { throw "NovaOryn VSIX build failed with exit code $msbuildExitCode." }

$templateFilesManifest = Join-Path $projectDirectory "obj\$Configuration\templateFiles.json"
if (-not (Test-Path -LiteralPath $templateFilesManifest -PathType Leaf)) {
    throw "VSSDK did not generate templateFiles.json. GenerateTemplatesManifest did not run."
}
$templateFilesText = Get-Content -LiteralPath $templateFilesManifest -Raw
foreach ($definition in $templateDefinitions) {
    if ($templateFilesText.IndexOf($definition.Id, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "VSSDK templateFiles.json does not reference $($definition.Id)."
    }
}
Write-Host "[ OK ] VSSDK registered all $($templateDefinitions.Count) NovaOryn project templates."

$vsix = Join-Path $projectDirectory "bin\$Configuration\NovaOryn.VisualStudio.vsix"
if (-not (Test-Path -LiteralPath $vsix -PathType Leaf)) {
    throw "NovaOryn VSIX was not generated: $vsix"
}

# Microsoft.VSSDK.BuildTools generated the registration manifest successfully,
# but SDK-style/external Content items are not guaranteed to become physical OPC
# parts in CreateVsixContainer. Deterministically embed all already-built
# template ZIPs and verify their manifest registrations before final validation.
$embedTemplates = Join-Path $projectDirectory "Embed-NovaOrynProjectTemplates.ps1"
if (-not (Test-Path -LiteralPath $embedTemplates -PathType Leaf)) {
    throw "NovaOryn VSIX template embed helper was not found: $embedTemplates"
}
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $embedTemplates `
    -VsixPath $vsix `
    -TemplatePackageRoot $packageRoot
if ($LASTEXITCODE -ne 0) {
    throw "NovaOryn VSIX project-template embedding failed with exit code $LASTEXITCODE."
}

$archive = [IO.Compression.ZipFile]::OpenRead($vsix)
try {
    foreach ($definition in $templateDefinitions) {
        $expectedEntry = "ProjectTemplates/$($definition.Id).zip"
        $entry = $archive.Entries | Where-Object { $_.FullName.Replace("\", "/") -eq $expectedEntry } | Select-Object -First 1
        if ($null -eq $entry -or $entry.Length -le 0) {
            throw "Built VSIX is missing project template payload: $expectedEntry"
        }
    }

    $manifestEntry = $archive.Entries | Where-Object { $_.FullName -eq "extension.vsixmanifest" } | Select-Object -First 1
    if ($null -eq $manifestEntry) { throw "NovaOryn VSIX is missing extension.vsixmanifest." }
    $reader = [IO.StreamReader]::new($manifestEntry.Open())
    try { [xml]$builtManifest = $reader.ReadToEnd() } finally { $reader.Dispose() }

    $builtIdentity = $builtManifest.PackageManifest.Metadata.Identity
    if ([string]$builtIdentity.Id -ne "NovaOryn.VisualStudio" -or [string]$builtIdentity.Version -ne $expectedVersion) {
        throw "Built VSIX identity/version is '$($builtIdentity.Id)' '$($builtIdentity.Version)', expected NovaOryn.VisualStudio $expectedVersion."
    }

    $builtManifestNamespace = New-Object System.Xml.XmlNamespaceManager($builtManifest.NameTable)
    $builtManifestNamespace.AddNamespace("vsix", $builtManifest.DocumentElement.NamespaceURI)
    $assets = @($builtManifest.SelectNodes("/vsix:PackageManifest/vsix:Assets/vsix:Asset[@Type='Microsoft.VisualStudio.ProjectTemplate']", $builtManifestNamespace))
    if ($assets.Count -ne $templateDefinitions.Count) {
        throw "Built VSIX registers $($assets.Count) project-template assets, expected $($templateDefinitions.Count)."
    }
    foreach ($definition in $templateDefinitions) {
        $path = "ProjectTemplates/$($definition.Id).zip"
        $matching = @($assets | Where-Object { [string]$_.Path -eq $path })
        if ($matching.Count -ne 1) { throw "Built VSIX does not register exactly one ProjectTemplate asset for $path." }
    }
} finally {
    $archive.Dispose()
}

$artifact = Join-Path $root "Artifacts\VisualStudio\NovaOryn.VisualStudio-$expectedVersion.vsix"
New-Item -ItemType Directory -Path (Split-Path -Parent $artifact) -Force | Out-Null
Copy-Item -LiteralPath $vsix -Destination $artifact -Force
Write-Host "[ OK ] NovaOryn VSIX: $artifact"
