[CmdletBinding()]
param(
    [switch]$GateProduction
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Npm = Join-Path $Root '.toolchain\Node\npm.cmd'
$Artifacts = Join-Path $Root 'Artifacts\Security'
$JsonRoot = Join-Path $Root 'JSON'
$NpmWorkspace = Join-Path $Root '.toolchain\NpmWorkspace'
$BaselinePath = Join-Path $JsonRoot 'Security-Baseline.json'
$FullReport = Join-Path $Artifacts 'npm-audit-full.json'
$ProductionReport = Join-Path $Artifacts 'npm-audit-production.json'

function Fail([string]$Message) { Write-Host "[FAIL] $Message"; exit 1 }
function Info([string]$Message) { Write-Host "[INFO] $Message" }
function Ok([string]$Message) { Write-Host "[ OK ] $Message" }

function Invoke-Audit([string[]]$ExtraArgs, [string]$OutputPath) {
    $arguments = @('audit','--json') + $ExtraArgs
    $tempErr = [System.IO.Path]::GetTempFileName()
    Push-Location -LiteralPath $NpmWorkspace
    try {
        $stdoutLines = & $Npm @arguments 2> $tempErr
        $stdout = ($stdoutLines -join [Environment]::NewLine)
        $stderr = Get-Content -LiteralPath $tempErr -Raw
        if ([string]::IsNullOrWhiteSpace($stdout)) {
            Fail "npm audit produced no JSON output. $stderr"
        }
        Set-Content -LiteralPath $OutputPath -Value $stdout -Encoding UTF8
        try { return ($stdout | ConvertFrom-Json) }
        catch { Fail "npm audit returned invalid JSON for $OutputPath. $stderr" }
    } finally {
        Pop-Location
        Remove-Item -LiteralPath $tempErr -Force -ErrorAction SilentlyContinue
    }
}

function Print-Summary([string]$Name, $Report) {
    $v = $Report.metadata.vulnerabilities
    Write-Host ("[INFO] {0}: total={1}, critical={2}, high={3}, moderate={4}, low={5}" -f $Name,$v.total,$v.critical,$v.high,$v.moderate,$v.low)
}

if (-not (Test-Path -LiteralPath $Npm)) { Fail "Pinned npm is unavailable: $Npm" }
if (-not (Test-Path -LiteralPath $BaselinePath)) { Fail "Missing security baseline: $BaselinePath" }
New-Item -ItemType Directory -Force -Path $Artifacts | Out-Null
$baseline = Get-Content -LiteralPath $BaselinePath -Raw | ConvertFrom-Json

Info 'Running complete dependency audit (development + production)...'
$full = Invoke-Audit @() $FullReport
Print-Summary 'Full dependency tree' $full

Info 'Running shipped production dependency audit...'
$production = Invoke-Audit @('--omit=dev') $ProductionReport
Print-Summary 'Production dependency tree' $production

$critical = @($production.vulnerabilities.PSObject.Properties | Where-Object { [string]$_.Value.severity -eq 'critical' })
if ($critical.Count -gt 0) {
    Fail "Production audit contains critical vulnerabilities: $($critical.Name -join ', ')."
}

$approved = @{}
foreach ($entry in $baseline.approvedProductionHighPackages) { $approved[[string]$entry.name] = $entry }
$unapprovedHigh = @()
$approvedHigh = @()
foreach ($property in $production.vulnerabilities.PSObject.Properties) {
    if ([string]$property.Value.severity -eq 'high') {
        if ($approved.ContainsKey($property.Name)) { $approvedHigh += $property.Name }
        else { $unapprovedHigh += $property.Name }
    }
}

if ($approvedHigh.Count -gt 0) {
    Write-Host "[WARN] Temporarily approved upstream production highs: $($approvedHigh -join ', ')"
    foreach ($name in $approvedHigh) {
        $entry = $approved[$name]
        Write-Host "[WARN]   $name - review after $($entry.reviewAfter): $($entry.reason)"
    }
}
if ($unapprovedHigh.Count -gt 0) {
    Fail "Unapproved production high vulnerabilities detected: $($unapprovedHigh -join ', ')."
}

Ok "Security reports written to $Artifacts"
if ($GateProduction) { Ok 'Production security gate passed.' }
exit 0
