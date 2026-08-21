[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$versionPath = Join-Path $root 'VERSION'
$sdkRoot = Join-Path $root 'SDK'
$remote = 'https://github.com/ProjectLithos/NovaOrynIDE.git'
$branch = 'main'

function Info([string]$Message) { Write-Host "[INFO] $Message" }
function Ok([string]$Message) { Write-Host "[ OK ] $Message" }
function Fail([string]$Message) { Write-Host "[FAIL] $Message"; exit 1 }
function Invoke-Git([string[]]$Arguments, [switch]$AllowFailure) {
    & git.exe @Arguments
    $code = $LASTEXITCODE
    if (-not $AllowFailure -and $code -ne 0) { throw "git $($Arguments -join ' ') failed with exit code $code" }
    return $code
}

if (-not (Get-Command git.exe -ErrorAction SilentlyContinue)) { Fail 'Git for Windows was not found on PATH.' }
if (-not (Test-Path -LiteralPath $versionPath)) { Fail 'VERSION is missing.' }
$version = ([string](Get-Content -LiteralPath $versionPath -TotalCount 1)).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+$') { Fail "Invalid VERSION line 1: $version" }

$nestedGit = Join-Path $sdkRoot '.git'
if (Test-Path -LiteralPath $nestedGit) {
    Info 'Removing nested SDK Git metadata so SDK source is committed with the IDE.'
    Remove-Item -LiteralPath $nestedGit -Recurse -Force
}

Push-Location -LiteralPath $root
try {
    if (-not (Test-Path -LiteralPath (Join-Path $root '.git'))) {
        Info 'Initialising NovaOrynIDE Git repository.'
        Invoke-Git @('init','-b',$branch,'.') | Out-Null
    }

    & git.exe remote get-url origin *> $null
    if ($LASTEXITCODE -ne 0) { Invoke-Git @('remote','add','origin',$remote) | Out-Null }
    else { Invoke-Git @('remote','set-url','origin',$remote) | Out-Null }
    Invoke-Git @('branch','-M',$branch) | Out-Null

    & git.exe rev-parse --verify HEAD *> $null
    if ($LASTEXITCODE -ne 0) {
        & git.exe fetch origin $branch *> $null
        if ($LASTEXITCODE -eq 0) {
            & git.exe show-ref --verify --quiet "refs/remotes/origin/$branch"
            if ($LASTEXITCODE -eq 0) {
                Info "Adopting existing origin/$branch history while preserving this source tree."
                Invoke-Git @('update-ref',"refs/heads/$branch", "refs/remotes/origin/$branch") | Out-Null
                Invoke-Git @('reset','--mixed','HEAD') | Out-Null
            }
        }
    }

    & git.exe config user.name *> $null
    if ($LASTEXITCODE -ne 0) { Invoke-Git @('config','user.name','NovaOrynIDE Build') | Out-Null }
    & git.exe config user.email *> $null
    if ($LASTEXITCODE -ne 0) { Invoke-Git @('config','user.email','novaorynide-build@users.noreply.github.com') | Out-Null }

    Info 'Staging source tree; .gitignore excludes tool downloads and build output.'
    Invoke-Git @('add','-A') | Out-Null

    & git.exe diff --cached --quiet
    if ($LASTEXITCODE -ne 0) {
        Info "Committing NovaOryn IDE $version source changes."
        # Automated builds must not execute developer-local repository hooks. A hook that
        # invokes Build-NovaOrynIDE.bat would recursively restart the build at commit time.
        Invoke-Git @('-c','core.hooksPath=NUL','commit','-m',"NovaOryn IDE $version") | Out-Null
    } else {
        Info 'No source changes require a new commit.'
    }

    Info "Pushing $branch to $remote..."
    Invoke-Git @('push','-u','origin',$branch) | Out-Null
    Ok "NovaOryn IDE source committed and pushed to $remote."
} catch {
    Fail "NovaOryn IDE source commit/push failed: $($_.Exception.Message)"
} finally {
    Pop-Location
}
exit 0
