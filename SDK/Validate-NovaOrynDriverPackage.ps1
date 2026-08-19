[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0)][string]$ManifestPath,
    [string]$TargetArchitecture,
    [string]$NovaOrynVersion,
    [string]$SdkApiVersion,
    [string]$DriverAbiVersion
)
$ErrorActionPreference = 'Stop'
function Fail([string]$message) { Write-Host "[FAIL] $message" -ForegroundColor Red; exit 1 }
function Ok([string]$message) { Write-Host "[ OK ] $message" -ForegroundColor Green }
function Parse-Version([string]$value, [string]$field, [int]$parts = 3) {
    $pattern = if ($parts -eq 2) { '^([0-9]+)\.([0-9]+)$' } else { '^([0-9]+)\.([0-9]+)\.([0-9]+)(?:[-+][0-9A-Za-z.-]+)?$' }
    if ($value -notmatch $pattern) { Fail "$field has invalid version '$value'." }
    return @([int64]$Matches[1], [int64]$Matches[2], $(if ($parts -eq 3) { [int64]$Matches[3] } else { 0 }))
}
function Version-AtLeast([string]$actual, [string]$minimum) {
    $a = Parse-Version $actual 'actual version'; $m = Parse-Version $minimum 'minimum version'
    for ($i=0; $i -lt 3; $i++) { if ($a[$i] -gt $m[$i]) { return $true }; if ($a[$i] -lt $m[$i]) { return $false } }
    return $true
}
if (-not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) { Fail "Driver manifest not found: $ManifestPath" }
try { $manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json } catch { Fail "Driver manifest is not valid JSON: $($_.Exception.Message)" }
$required = 'schemaVersion','id','name','kind','version','architecture','minimumNovaOrynVersion','sdkApiVersion','driverAbiVersion','ids','dependencies','capabilities','permissions','signing'
foreach ($field in $required) { if ($null -eq $manifest.$field) { Fail "Driver manifest is missing '$field'." } }
if ([int]$manifest.schemaVersion -ne 3) { Fail "Unsupported driver manifest schemaVersion '$($manifest.schemaVersion)'. Expected 3." }
if ([string]$manifest.id -notmatch '^[A-Za-z0-9]+(?:[._-][A-Za-z0-9]+)+$') { Fail "Driver id '$($manifest.id)' must be a stable reverse-domain/package style ID using letters, digits, '.', '_' or '-'." }
if ([string]::IsNullOrWhiteSpace([string]$manifest.name)) { Fail 'Driver name must not be empty.' }
[void](Parse-Version ([string]$manifest.version) 'version')
[void](Parse-Version ([string]$manifest.minimumNovaOrynVersion) 'minimumNovaOrynVersion')
[void](Parse-Version ([string]$manifest.sdkApiVersion) 'sdkApiVersion' 2)
[void](Parse-Version ([string]$manifest.driverAbiVersion) 'driverAbiVersion' 2)
$kinds=@('pci','usb','virtio','platform'); if ($kinds -notcontains ([string]$manifest.kind).ToLowerInvariant()) { Fail "Unsupported driver kind '$($manifest.kind)'." }
$architectures = @('any','x64','arm64'); if ($architectures -notcontains ([string]$manifest.architecture).ToLowerInvariant()) { Fail "Unsupported architecture '$($manifest.architecture)'." }
$allowedPermissions = @('mmio','pio','interrupts','msi','msix','dma','pci-config','physical-memory','timers','networking','filesystem')
$seen = @{}; foreach ($permission in @($manifest.permissions)) { $p=[string]$permission; if ($allowedPermissions -notcontains $p) { Fail "Unknown driver permission '$p'." }; if ($seen.ContainsKey($p)) { Fail "Duplicate driver permission '$p'." }; $seen[$p]=$true }
$depSeen=@{}; foreach ($dependency in @($manifest.dependencies)) { $dep=[string]$dependency; if ([string]::IsNullOrWhiteSpace($dep)) { Fail 'Driver dependency entries must not be empty.' }; if ($depSeen.ContainsKey($dep)) { Fail "Duplicate dependency '$dep'." }; $depSeen[$dep]=$true }
$capSeen=@{}; foreach ($capability in @($manifest.capabilities)) { $c=[string]$capability; if ($allowedPermissions -notcontains $c) { Fail "Unknown driver capability '$c'." }; if ($capSeen.ContainsKey($c)) { Fail "Duplicate driver capability '$c'." }; $capSeen[$c]=$true; if ($seen.ContainsKey($c) -eq $false) { Fail "Capability '$c' must also appear in permissions." } }
$signingStates=@('unsigned','development','signed','trusted','revoked'); $state=([string]$manifest.signing.state).ToLowerInvariant(); if ($signingStates -notcontains $state) { Fail "Unsupported signing state '$($manifest.signing.state)'." }
if ($state -in @('signed','trusted')) { if ([string]::IsNullOrWhiteSpace([string]$manifest.signing.algorithm) -or [string]::IsNullOrWhiteSpace([string]$manifest.signing.signerId) -or [string]::IsNullOrWhiteSpace([string]$manifest.signing.digest)) { Fail "Signing state '$state' requires algorithm, signerId and digest." } }
if ($state -eq 'revoked') { Fail 'This driver package is marked revoked and cannot be accepted.' }
if ($TargetArchitecture) { $target=$TargetArchitecture.ToLowerInvariant(); $driver=([string]$manifest.architecture).ToLowerInvariant(); if ($driver -ne 'any' -and $driver -ne $target) { Fail "Driver architecture '$driver' is incompatible with target '$target'." } }
if ($NovaOrynVersion -and -not (Version-AtLeast $NovaOrynVersion ([string]$manifest.minimumNovaOrynVersion))) { Fail "NovaOryn $NovaOrynVersion is below the driver's minimum $($manifest.minimumNovaOrynVersion)." }
if ($SdkApiVersion -and $SdkApiVersion -ne [string]$manifest.sdkApiVersion) { Fail "SDK API version '$SdkApiVersion' does not match driver requirement '$($manifest.sdkApiVersion)'." }
if ($DriverAbiVersion -and $DriverAbiVersion -ne [string]$manifest.driverAbiVersion) { Fail "Driver ABI version '$DriverAbiVersion' does not match driver requirement '$($manifest.driverAbiVersion)'." }
Ok "Driver package manifest '$($manifest.id)' $($manifest.version) is valid."
exit 0
