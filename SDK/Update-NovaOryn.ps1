# This script may be launched from inside the selected archive by Bootstrap-Update-NovaOryn.ps1.
param(
    [Parameter(Position = 0)]
    [string]$ArchiveFolder
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step([string]$Message) { Apply-NovaOrynDeclaredDeletions -RepositoryRoot $root

Write-Host "[INFO] $Message" }
function Write-Ok([string]$Message) { Write-Host "[ OK ] $Message" }
function Fail([string]$Message) { throw "[FAIL] $Message" }

function Get-VersionFromName([string]$Name, [string]$Kind) {
    $pattern = '^NovaOryn-' + [regex]::Escape($Kind) + '-(?<version>\d+\.\d+\.\d+)\.zip$'
    $match = [regex]::Match($Name, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $match.Success) { return $null }
    return [version]$match.Groups['version'].Value
}

function Find-LatestArchive([string[]]$Folders, [string]$Kind) {
    $matches = foreach ($folder in ($Folders | Select-Object -Unique)) {
        if (-not (Test-Path -LiteralPath $folder -PathType Container)) { continue }
        Get-ChildItem -LiteralPath $folder -File -Filter "NovaOryn-$Kind-*.zip" | ForEach-Object {
            $version = Get-VersionFromName $_.Name $Kind
            if ($null -ne $version) { [pscustomobject]@{ File = $_; Version = $version } }
        }
    }
    return $matches | Sort-Object Version -Descending | Select-Object -First 1
}

function Test-RepositoryHasCommit([string]$RepositoryRoot) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot '.git') -PathType Container)) { return $false }

    $standardOutput = Join-Path ([IO.Path]::GetTempPath()) ('NovaOrynGitOut-' + [guid]::NewGuid().ToString('N') + '.txt')
    $standardError = Join-Path ([IO.Path]::GetTempPath()) ('NovaOrynGitErr-' + [guid]::NewGuid().ToString('N') + '.txt')

    try {
        $process = Start-Process `
            -FilePath 'git.exe' `
            -ArgumentList @('-C', $RepositoryRoot, 'rev-parse', '--verify', '--quiet', 'HEAD') `
            -Wait `
            -PassThru `
            -NoNewWindow `
            -RedirectStandardOutput $standardOutput `
            -RedirectStandardError $standardError

        return $process.ExitCode -eq 0
    } finally {
        Remove-Item -LiteralPath $standardOutput -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $standardError -Force -ErrorAction SilentlyContinue
    }
}

function Ensure-Repository([string]$RepositoryRoot, [string]$RemoteUrl) {
    New-Item -ItemType Directory -Path $RepositoryRoot -Force | Out-Null
    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryRoot '.git') -PathType Container)) {
        Write-Step 'Initialising the Git repository.'
        & git.exe -C $RepositoryRoot init -b main
        if ($LASTEXITCODE -ne 0) { Fail 'git init failed.' }
    }
    $remoteNames = @(& git.exe -C $RepositoryRoot remote)
    if ($LASTEXITCODE -ne 0) { Fail 'Could not list Git remotes.' }

    if ($remoteNames -notcontains 'origin') {
        & git.exe -C $RepositoryRoot remote add origin $RemoteUrl
        if ($LASTEXITCODE -ne 0) { Fail 'Could not add the origin remote.' }
        Write-Ok "Added origin remote: $RemoteUrl"
        return
    }

    $origin = (& git.exe -C $RepositoryRoot remote get-url origin | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) { Fail 'Could not read the origin remote.' }
    if ($origin -ne $RemoteUrl) {
        Fail "The origin remote is '$origin', not '$RemoteUrl'."
    }
}

function Expand-SourceArchive([string]$ArchivePath, [string]$RepositoryRoot) {
    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('NovaOrynUpdate-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    try {
        Write-Step "Extracting $([IO.Path]::GetFileName($ArchivePath))."
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $temporaryRoot -Force
        if (Test-Path -LiteralPath (Join-Path $temporaryRoot '.git')) { Fail 'The archive must not contain a .git directory.' }
        $rootItems = @(Get-ChildItem -LiteralPath $temporaryRoot -Force)
        if ($rootItems.Count -eq 1 -and $rootItems[0].PSIsContainer) { Fail 'The archive has an enclosing top-level directory.' }
        Get-ChildItem -LiteralPath $temporaryRoot -Force | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $RepositoryRoot -Recurse -Force
        }
    } finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Get-ArchivePathSet([string]$ArchivePath) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $archive = [IO.Compression.ZipFile]::OpenRead($ArchivePath)
    try {
        foreach ($entry in $archive.Entries) {
            if ([string]::IsNullOrWhiteSpace($entry.Name)) { continue }
            $relativePath = $entry.FullName.Replace('\', '/').TrimStart('/')
            $null = $paths.Add($relativePath)
        }
    } finally {
        $archive.Dispose()
    }

    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('NovaOrynManifest-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $temporaryRoot -Force
        $manifestPath = Join-Path $temporaryRoot 'NovaOryn-Changes.json'
        if (Test-Path -LiteralPath $manifestPath -PathType Leaf) {
            $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
            foreach ($path in @($manifest.deletedFiles)) {
                if (-not [string]::IsNullOrWhiteSpace([string]$path)) { $null = $paths.Add(([string]$path).Replace('\', '/')) }
            }
            foreach ($rename in @($manifest.renamedFiles)) {
                if ($null -eq $rename) { continue }
                if (-not [string]::IsNullOrWhiteSpace([string]$rename.from)) { $null = $paths.Add(([string]$rename.from).Replace('\', '/')) }
                if (-not [string]::IsNullOrWhiteSpace([string]$rename.to)) { $null = $paths.Add(([string]$rename.to).Replace('\', '/')) }
            }
        }
    } finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    return $paths
}

function Get-Sha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-ArchiveFileHashes([string]$ArchivePath) {
    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('NovaOrynArchiveHashes-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $temporaryRoot -Force
        $hashes = @{}
        Get-ChildItem -LiteralPath $temporaryRoot -File -Recurse -Force | ForEach-Object {
            $relative = $_.FullName.Substring($temporaryRoot.Length).TrimStart('\', '/').Replace('\', '/')
            $hashes[$relative] = Get-Sha256 $_.FullName
        }
        return $hashes
    } finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}


function Get-ArchiveDeclaredDeletionSet([string]$ArchivePath) {
    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('NovaOrynArchiveChanges-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $temporaryRoot -Force
        $manifestPath = Join-Path $temporaryRoot 'NovaOryn-Changes.json'
        $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { return $paths }
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        foreach ($relativePath in @($manifest.deletedFiles)) {
            if ([string]::IsNullOrWhiteSpace([string]$relativePath)) { continue }
            [void]$paths.Add(([string]$relativePath).Replace('\', '/'))
        }
        return $paths
    } finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}


function Get-ArchiveTargetPathSet([string]$ArchivePath) {
    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('NovaOrynTargetManifest-' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    try {
        Expand-Archive -LiteralPath $ArchivePath -DestinationPath $temporaryRoot -Force
        $manifestPath = Join-Path $temporaryRoot 'NovaOryn-SourceManifest.json'
        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
            Fail 'Selected archive does not contain NovaOryn-SourceManifest.json.'
        }

        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        $paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        foreach ($file in @($manifest.files)) {
            if ($null -eq $file -or [string]::IsNullOrWhiteSpace([string]$file.path)) { continue }
            [void]$paths.Add(([string]$file.path).Replace('\\', '/'))
        }
        return $paths
    } finally {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Assert-TargetSourceManifest([string]$RepositoryRoot) {
    $manifestPath = Join-Path $RepositoryRoot 'NovaOryn-SourceManifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        Fail 'Updated source tree does not contain NovaOryn-SourceManifest.json.'
    }

    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    } catch {
        Fail 'Updated NovaOryn-SourceManifest.json is invalid.'
    }

    $problems = [Collections.Generic.List[string]]::new()
    foreach ($file in @($manifest.files)) {
        if ($null -eq $file -or [string]::IsNullOrWhiteSpace([string]$file.path)) {
            $problems.Add('<manifest entry without path>')
            continue
        }

        $relative = ([string]$file.path).Replace('\', '/')
        if ([IO.Path]::IsPathRooted($relative) -or $relative.Split('/') -contains '..') {
            $problems.Add("$relative (unsafe path)")
            continue
        }

        $target = Join-Path $RepositoryRoot $relative
        if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
            $problems.Add("$relative (missing)")
            continue
        }

        $actualItem = Get-Item -LiteralPath $target
        $expectedSize = [long]$file.size
        if ($actualItem.Length -ne $expectedSize) {
            $problems.Add("$relative (size $($actualItem.Length), expected $expectedSize)")
            continue
        }

        $expectedHash = ([string]$file.sha256).ToLowerInvariant()
        $actualHash = Get-Sha256 $target
        if ($actualHash -ne $expectedHash) {
            $problems.Add("$relative (SHA-256 mismatch)")
        }
    }

    if ($problems.Count -gt 0) {
        Fail "Updated source tree does not match NovaOryn-SourceManifest.json: $($problems -join ', ')"
    }

    Write-Ok "Target source manifest verified: $(@($manifest.files).Count) file(s)."
}

function Get-PreviouslySuppliedFileHashes([string]$RepositoryRoot) {
    $manifestPath = Join-Path $RepositoryRoot 'NovaOryn-SourceManifest.json'
    $hashes = @{}
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { return $hashes }

    try {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
        foreach ($file in @($manifest.files)) {
            if ($null -eq $file) { continue }
            $relative = ([string]$file.path).Replace('\', '/')
            $hash = ([string]$file.sha256).ToLowerInvariant()
            if (-not [string]::IsNullOrWhiteSpace($relative) -and -not [string]::IsNullOrWhiteSpace($hash)) {
                $hashes[$relative] = $hash
            }
        }
    } catch {
        Fail 'NovaOryn-SourceManifest.json is invalid and cannot be used to validate previously supplied files.'
    }
    return $hashes
}

function Assert-SafeWorkingTree([string]$RepositoryRoot, [string]$ArchivePath) {
    $status = @(& git.exe -C $RepositoryRoot -c core.quotepath=false status --porcelain --untracked-files=all)
    if ($LASTEXITCODE -ne 0) { Fail 'Could not inspect repository status.' }
    if ($status.Count -eq 0) { return }

    $archivePaths = Get-ArchivePathSet $ArchivePath
    $archiveHashes = Get-ArchiveFileHashes $ArchivePath
    $declaredDeletions = Get-ArchiveDeclaredDeletionSet $ArchivePath
    $targetPaths = Get-ArchiveTargetPathSet $ArchivePath
    $previousHashes = Get-PreviouslySuppliedFileHashes $RepositoryRoot
    $unexpected = [Collections.Generic.List[string]]::new()

    foreach ($line in $status) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) { continue }
        $statusCode = $line.Substring(0, 2)
        $pathText = $line.Substring(3).Trim()
        $candidatePaths = if ($pathText.Contains(' -> ')) { $pathText -split ' -> ' } else { @($pathText) }

        foreach ($candidate in $candidatePaths) {
            $normalized = $candidate.Trim('"').Replace('\', '/')
            $localPath = Join-Path $RepositoryRoot $normalized

            # A tracked path that is already absent locally and absent from the selected
            # target source manifest is a valid carried-forward deletion. This filesystem
            # and target-state rule is independent of which Git porcelain column reports D.
            $isMissingLocally = -not (Test-Path -LiteralPath $localPath)
            if ($isMissingLocally -and -not $targetPaths.Contains($normalized)) {
                continue
            }

            # Immediate declared deletions and rename sources remain valid as well.
            $isDeletion = ($statusCode.Length -ge 2) -and (($statusCode[0] -eq 'D') -or ($statusCode[1] -eq 'D'))
            $isRename = $pathText.Contains(' -> ')
            if (($isDeletion -or $isRename) -and ($archivePaths.Contains($normalized) -or $declaredDeletions.Contains($normalized))) {
                continue
            }

            # The source manifest is generated release-control data. A selected archive
            # always replaces it, so an older unapplied manifest is safe to overwrite.
            if ($normalized.Equals('NovaOryn-SourceManifest.json', [StringComparison]::OrdinalIgnoreCase) -and $archiveHashes.ContainsKey($normalized)) {
                continue
            }

            if (-not (Test-Path -LiteralPath $localPath -PathType Leaf)) {
                $unexpected.Add($normalized)
                continue
            }

            $currentHash = Get-Sha256 $localPath

            # Accept a file already extracted from the selected ChangedFiles archive.
            if ($archiveHashes.ContainsKey($normalized) -and $archiveHashes[$normalized] -eq $currentHash) {
                continue
            }

            # Also accept unchanged NovaOryn files left uncommitted by an earlier release,
            # but only when their exact content matches the source manifest already supplied.
            if ($previousHashes.ContainsKey($normalized) -and $previousHashes[$normalized] -eq $currentHash) {
                continue
            }

            $unexpected.Add($normalized)
        }
    }

    if ($unexpected.Count -gt 0) {
        $uniqueUnexpected = $unexpected | Sort-Object -Unique
        Fail "C:\NovaOryn contains uncommitted changes that are neither exact files from $([IO.Path]::GetFileName($ArchivePath)) nor unchanged files recorded by NovaOryn-SourceManifest.json: $($uniqueUnexpected -join ', ')"
    }

    Write-Ok 'Existing uncommitted files are verified NovaOryn release files and are safe to commit.'
}

function Clear-UncommittedInitialTree([string]$RepositoryRoot) {
    Get-ChildItem -LiteralPath $RepositoryRoot -Force | Where-Object Name -ne '.git' | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
}

function Apply-ChangeManifest([string]$RepositoryRoot) {
    $manifestPath = Join-Path $RepositoryRoot 'NovaOryn-Changes.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { return }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    foreach ($relativePath in @($manifest.deletedFiles)) {
        if ([string]::IsNullOrWhiteSpace([string]$relativePath)) { continue }
        $target = Join-Path $RepositoryRoot ([string]$relativePath)
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force; Write-Step "Deleted: $relativePath" }
    }
    foreach ($rename in @($manifest.renamedFiles)) {
        if ($null -eq $rename) { continue }
        $oldPath = Join-Path $RepositoryRoot ([string]$rename.from)
        $newPath = Join-Path $RepositoryRoot ([string]$rename.to)
        if (Test-Path -LiteralPath $oldPath) {
            if (Test-Path -LiteralPath $newPath) { Remove-Item -LiteralPath $oldPath -Recurse -Force }
            else {
                $newParent = Split-Path -Parent $newPath
                if (-not (Test-Path -LiteralPath $newParent)) { New-Item -ItemType Directory -Path $newParent -Force | Out-Null }
                Move-Item -LiteralPath $oldPath -Destination $newPath -Force
            }
            Write-Step "Renamed: $($rename.from) -> $($rename.to)"
        }
    }
}

try {
    $null = Get-Command git.exe -ErrorAction Stop
    $repositoryRoot = 'C:\NovaOryn'
    $remoteUrl = 'https://github.com/ProjectLithos/NovaOryn.git'
    $scriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Path
    $downloadsFolder = Join-Path $env:USERPROFILE 'Downloads'
    $archiveFolders = @($scriptFolder, $downloadsFolder)
    if (-not [string]::IsNullOrWhiteSpace($ArchiveFolder)) {
        if (-not (Test-Path -LiteralPath $ArchiveFolder -PathType Container)) { Fail "Archive folder does not exist: $ArchiveFolder" }
        $archiveFolders = @($ArchiveFolder)
    }

    $hasCommit = Test-RepositoryHasCommit $repositoryRoot
    $archiveKind = if ($hasCommit) { 'ChangedFiles' } else { 'FullSource' }
    $latest = Find-LatestArchive $archiveFolders $archiveKind
    if ($null -eq $latest) { Fail "No valid NovaOryn-$archiveKind-x.y.z.zip archive was found. Checked: $($archiveFolders -join ', ')" }

    Write-Ok "Selected $($latest.File.Name)."
    Ensure-Repository $repositoryRoot $remoteUrl
    if ($hasCommit) { Assert-SafeWorkingTree $repositoryRoot $latest.File.FullName } else { Clear-UncommittedInitialTree $repositoryRoot }
    Expand-SourceArchive $latest.File.FullName $repositoryRoot
    if ($hasCommit) { Apply-ChangeManifest $repositoryRoot }
    Assert-TargetSourceManifest $repositoryRoot

    & git.exe -C $repositoryRoot add -A
    if ($LASTEXITCODE -ne 0) { Fail 'git add failed.' }
    & git.exe -C $repositoryRoot diff --cached --quiet
    if ($LASTEXITCODE -eq 0) { Write-Ok 'The archive produced no source changes. No commit was created.'; exit 0 }
    if ($LASTEXITCODE -ne 1) { Fail 'Could not inspect the staged changes.' }

    $commitKind = if ($hasCommit) { 'Update' } else { 'Initial source' }
    $commitMessage = "$commitKind NovaOryn to $($latest.Version)"
    Write-Step "Creating commit: $commitMessage"
    & git.exe -C $repositoryRoot commit -m $commitMessage
    if ($LASTEXITCODE -ne 0) { Fail 'git commit failed. Configure Git user.name and user.email, then run the batch again.' }

    Write-Ok "Committed $($latest.File.Name) to $repositoryRoot."
    Write-Step 'Pushing main to origin before any toolchain download.'
    & git.exe -C $repositoryRoot push -u origin main
    if ($LASTEXITCODE -ne 0) { Fail 'git push failed. The toolchain was not downloaded.' }
    Write-Ok 'The source commit is present on origin/main.'

    $toolchainInstaller = Join-Path $repositoryRoot 'Install-NovaOrynToolchain.bat'
    if (-not (Test-Path -LiteralPath $toolchainInstaller -PathType Leaf)) { Fail "Missing toolchain installer: $toolchainInstaller" }
    Write-Step 'Checking and installing the pinned toolchain where required.'
    & $toolchainInstaller
    if ($LASTEXITCODE -ne 0) { Fail 'The source was pushed, but toolchain installation failed.' }
    Write-Ok 'Source update, push, and toolchain validation completed.'
    exit 0
} catch {
    Write-Host $_.Exception.Message
    exit 1
}
