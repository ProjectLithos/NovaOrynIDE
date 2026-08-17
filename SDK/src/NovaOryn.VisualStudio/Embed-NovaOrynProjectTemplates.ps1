[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$VsixPath,
    [Parameter(Mandatory = $true)][string]$TemplatePackageRoot
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $VsixPath -PathType Leaf)) {
    throw "VSIX container was not found: $VsixPath"
}
if (-not (Test-Path -LiteralPath $TemplatePackageRoot -PathType Container)) {
    throw "NovaOryn project-template package directory was not found: $TemplatePackageRoot"
}

$templateFileNames = @(
    "NovaOrynKernel.zip",
    "NovaOrynKernelDriver.zip",
    "NovaOrynKernelLibrary.zip",
    "NovaOrynFilesystemFatFs.zip",
    "NovaOrynUserlandApplication.zip",
    "NovaOrynUserlandService.zip",
    "NovaOrynUserlandDriver.zip",
    "NovaOrynUserlandLibrary.zip",
    "NovaOrynTestProject.zip"
)

foreach ($templateFileName in $templateFileNames) {
    $templatePath = Join-Path $TemplatePackageRoot $templateFileName
    if (-not (Test-Path -LiteralPath $templatePath -PathType Leaf)) {
        throw "NovaOryn template payload was not found before VSIX embedding: $templatePath"
    }
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$archive = [IO.Compression.ZipFile]::Open($VsixPath, [IO.Compression.ZipArchiveMode]::Update)
try {
    foreach ($templateFileName in $templateFileNames) {
        $partName = "ProjectTemplates/$templateFileName"
        $templatePath = Join-Path $TemplatePackageRoot $templateFileName

        $existingEntries = @($archive.Entries | Where-Object {
            $_.FullName.Replace("\", "/") -eq $partName
        })
        foreach ($existingEntry in $existingEntries) {
            $existingEntry.Delete()
        }

        $entry = $archive.CreateEntry($partName, [IO.Compression.CompressionLevel]::Optimal)
        $entryStream = $entry.Open()
        $sourceStream = [IO.File]::OpenRead($templatePath)
        try {
            $sourceStream.CopyTo($entryStream)
        } finally {
            $sourceStream.Dispose()
            $entryStream.Dispose()
        }

        Write-Host "[ OK ] Embedded VSIX project-template payload: $partName"
    }

    # Ensure extension.vsixmanifest registers exactly one ProjectTemplate asset
    # for every physical ZIP payload.
    $manifestEntry = $archive.Entries |
        Where-Object { $_.FullName -eq "extension.vsixmanifest" } |
        Select-Object -First 1
    if ($null -eq $manifestEntry) {
        throw "VSIX container is missing extension.vsixmanifest: $VsixPath"
    }

    $manifestReader = [IO.StreamReader]::new($manifestEntry.Open())
    try {
        [xml]$manifestXml = $manifestReader.ReadToEnd()
    } finally {
        $manifestReader.Dispose()
    }

    $vsixNamespace = $manifestXml.DocumentElement.NamespaceURI
    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($manifestXml.NameTable)
    $namespaceManager.AddNamespace("vsix", $vsixNamespace)

    $assetsNode = $manifestXml.SelectSingleNode(
        "/vsix:PackageManifest/vsix:Assets",
        $namespaceManager)
    if ($null -eq $assetsNode) {
        $assetsNode = $manifestXml.CreateElement("Assets", $vsixNamespace)
        [void]$manifestXml.DocumentElement.AppendChild($assetsNode)
    }

    # Remove duplicate NovaOryn ProjectTemplate asset registrations, then add
    # exactly the canonical template paths. Do not touch unrelated asset types.
    $projectTemplateAssets = @($manifestXml.SelectNodes(
        "/vsix:PackageManifest/vsix:Assets/vsix:Asset[@Type='Microsoft.VisualStudio.ProjectTemplate']",
        $namespaceManager))
    foreach ($asset in $projectTemplateAssets) {
        $path = [string]$asset.Path
        if ($path.StartsWith("ProjectTemplates/NovaOryn", [StringComparison]::OrdinalIgnoreCase)) {
            [void]$assetsNode.RemoveChild($asset)
        }
    }

    foreach ($templateFileName in $templateFileNames) {
        $asset = $manifestXml.CreateElement("Asset", $vsixNamespace)
        [void]$asset.SetAttribute("Type", "Microsoft.VisualStudio.ProjectTemplate")
        [void]$asset.SetAttribute("Path", "ProjectTemplates/$templateFileName")
        [void]$assetsNode.AppendChild($asset)
    }

    $manifestEntry.Delete()
    $newManifestEntry = $archive.CreateEntry(
        "extension.vsixmanifest",
        [IO.Compression.CompressionLevel]::Optimal)
    $manifestWriter = [IO.StreamWriter]::new(
        $newManifestEntry.Open(),
        [Text.UTF8Encoding]::new($false))
    try {
        $manifestXml.Save($manifestWriter)
    } finally {
        $manifestWriter.Dispose()
    }

    # A VSIX is an OPC package. Register each nested ZIP as an application/zip
    # package part so the container remains structurally valid.
    $typesEntry = $archive.Entries |
        Where-Object { $_.FullName -eq "[Content_Types].xml" } |
        Select-Object -First 1
    if ($null -eq $typesEntry) {
        throw "VSIX container is missing [Content_Types].xml: $VsixPath"
    }

    $typesReader = [IO.StreamReader]::new($typesEntry.Open())
    try {
        [xml]$typesXml = $typesReader.ReadToEnd()
    } finally {
        $typesReader.Dispose()
    }

    foreach ($templateFileName in $templateFileNames) {
        $opcPartName = "/ProjectTemplates/$templateFileName"
        $existingOverride = @($typesXml.Types.Override | Where-Object {
            [string]$_.PartName -eq $opcPartName
        }) | Select-Object -First 1

        if ($null -eq $existingOverride) {
            $override = $typesXml.CreateElement(
                "Override",
                $typesXml.DocumentElement.NamespaceURI)
            [void]$override.SetAttribute("PartName", $opcPartName)
            [void]$override.SetAttribute("ContentType", "application/zip")
            [void]$typesXml.DocumentElement.AppendChild($override)
        } else {
            $existingOverride.SetAttribute("ContentType", "application/zip")
        }
    }

    $typesEntry.Delete()
    $newTypesEntry = $archive.CreateEntry(
        "[Content_Types].xml",
        [IO.Compression.CompressionLevel]::Optimal)
    $typesWriter = [IO.StreamWriter]::new(
        $newTypesEntry.Open(),
        [Text.UTF8Encoding]::new($false))
    try {
        $typesXml.Save($typesWriter)
    } finally {
        $typesWriter.Dispose()
    }
} finally {
    $archive.Dispose()
}

# Re-open the completed package and verify physical payloads + registrations.
$check = [IO.Compression.ZipFile]::OpenRead($VsixPath)
try {
    foreach ($templateFileName in $templateFileNames) {
        $partName = "ProjectTemplates/$templateFileName"
        $entry = $check.Entries |
            Where-Object { $_.FullName.Replace("\", "/") -eq $partName } |
            Select-Object -First 1
        if ($null -eq $entry -or $entry.Length -le 0) {
            throw "VSIX project-template payload was not embedded correctly: $partName"
        }
    }

    $manifestEntry = $check.Entries |
        Where-Object { $_.FullName -eq "extension.vsixmanifest" } |
        Select-Object -First 1
    if ($null -eq $manifestEntry) {
        throw "VSIX registration verification cannot find extension.vsixmanifest."
    }

    $manifestReader = [IO.StreamReader]::new($manifestEntry.Open())
    try {
        [xml]$manifestCheck = $manifestReader.ReadToEnd()
    } finally {
        $manifestReader.Dispose()
    }

    $namespaceManager = New-Object System.Xml.XmlNamespaceManager($manifestCheck.NameTable)
    $namespaceManager.AddNamespace("vsix", $manifestCheck.DocumentElement.NamespaceURI)

    $assets = @($manifestCheck.SelectNodes(
        "/vsix:PackageManifest/vsix:Assets/vsix:Asset[@Type='Microsoft.VisualStudio.ProjectTemplate']",
        $namespaceManager))

    foreach ($templateFileName in $templateFileNames) {
        $expectedPath = "ProjectTemplates/$templateFileName"
        $matches = @($assets | Where-Object {
            [string]$_.Path -eq $expectedPath
        })
        if ($matches.Count -ne 1) {
            throw "VSIX must register exactly one ProjectTemplate asset for: $expectedPath"
        }
    }
} finally {
    $check.Dispose()
}

Write-Host "[ OK ] VSIX contains and registers all $($templateFileNames.Count) NovaOryn project templates."
