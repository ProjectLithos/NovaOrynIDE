param(
  [string]$Manifest = "NovaOryn.Tests.json",
  [ValidateSet("kernel","unit","integration","boot","driver","stress","fault-injection","hardware-simulation")][string]$Kind,
  [string]$Tag,
  [switch]$List,
  [switch]$FailFast,
  [string]$Report = "Artifacts\Tests\NovaOryn-TestReport.json"
)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$allowed=@('kernel','unit','integration','boot','driver','stress','fault-injection','hardware-simulation')
$manifestPath=if([IO.Path]::IsPathRooted($Manifest)){$Manifest}else{Join-Path (Get-Location) $Manifest}
if(!(Test-Path -LiteralPath $manifestPath)){
  $fallback=Join-Path $root $Manifest
  if(Test-Path -LiteralPath $fallback){$manifestPath=$fallback}else{
    Write-Host "[FAIL] NovaOryn test manifest not found: $Manifest"; exit 2
  }
}
try{$m=Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json}catch{Write-Host "[FAIL] Invalid test manifest JSON: $($_.Exception.Message)";exit 2}
if([int]$m.schemaVersion -ne 1){Write-Host '[FAIL] Unsupported NovaOryn test manifest schemaVersion. Expected 1.';exit 2}
$seen=@{};$selected=@()
foreach($t in @($m.tests)){
  if([string]::IsNullOrWhiteSpace([string]$t.id) -or [string]::IsNullOrWhiteSpace([string]$t.name)){Write-Host '[FAIL] Every test requires id and name.';exit 2}
  if($seen.ContainsKey([string]$t.id)){Write-Host "[FAIL] Duplicate test id: $($t.id)";exit 2};$seen[[string]$t.id]=$true
  if($allowed -notcontains [string]$t.kind){Write-Host "[FAIL] Unsupported test kind '$($t.kind)' for $($t.id)";exit 2}
  if($null -ne $t.enabled -and -not [bool]$t.enabled){continue}
  if($Kind -and [string]$t.kind -ne $Kind){continue}
  if($Tag -and (@($t.tags) -notcontains $Tag)){continue}
  $selected+=$t
}
Write-Host "[INFO] NovaOryn SDK Test Framework 0.11.1"
Write-Host "[INFO] Manifest: $manifestPath"
Write-Host "[INFO] Selected tests: $($selected.Count)"
if($List){foreach($t in $selected){Write-Host ("{0,-20} {1,-20} {2}" -f $t.kind,$t.id,$t.name)};exit 0}
$results=@();$failed=0
foreach($t in $selected){
  $timeout=if($null -eq $t.timeoutSeconds){300}else{[int]$t.timeoutSeconds};$expected=if($null -eq $t.expectedExitCode){0}else{[int]$t.expectedExitCode}
  $working=if([string]::IsNullOrWhiteSpace([string]$t.workingDirectory)){Split-Path -Parent $manifestPath}elseif([IO.Path]::IsPathRooted([string]$t.workingDirectory)){[string]$t.workingDirectory}else{Join-Path (Split-Path -Parent $manifestPath) ([string]$t.workingDirectory)}
  Write-Host "[TEST] [$($t.kind)] $($t.id) - $($t.name)"
  $psi=[Diagnostics.ProcessStartInfo]::new();$psi.FileName=[string]$t.command;$psi.WorkingDirectory=$working;$psi.UseShellExecute=$false;$psi.RedirectStandardOutput=$true;$psi.RedirectStandardError=$true
  foreach($a in @($t.arguments)){$null=$psi.ArgumentList.Add([string]$a)}
  if($t.environment){foreach($p in $t.environment.PSObject.Properties){$psi.Environment[[string]$p.Name]=[string]$p.Value}}
  $sw=[Diagnostics.Stopwatch]::StartNew();$p=[Diagnostics.Process]::new();$p.StartInfo=$psi
  try{$null=$p.Start()}catch{$sw.Stop();$results+=[ordered]@{id=$t.id;name=$t.name;kind=$t.kind;result='failed';durationMilliseconds=$sw.ElapsedMilliseconds;exitCode=-1;stdout='';stderr=$_.Exception.Message};$failed++;Write-Host "[FAIL] Could not start: $($_.Exception.Message)";if($FailFast){break};continue}
  $outTask=$p.StandardOutput.ReadToEndAsync();$errTask=$p.StandardError.ReadToEndAsync();$timedOut=$false
  if($timeout -gt 0 -and -not $p.WaitForExit($timeout*1000)){$timedOut=$true;try{$p.Kill($true)}catch{};$p.WaitForExit()}else{$p.WaitForExit()}
  $stdout=$outTask.GetAwaiter().GetResult();$stderr=$errTask.GetAwaiter().GetResult();$sw.Stop();$exitCode=if($timedOut){-1}else{$p.ExitCode}
  $result=if($timedOut){'timeout'}elseif($exitCode -eq $expected){'passed'}else{'failed'}
  if($result -ne 'passed'){$failed++}
  $results+=[ordered]@{id=$t.id;name=$t.name;kind=$t.kind;result=$result;durationMilliseconds=$sw.ElapsedMilliseconds;exitCode=$exitCode;stdout=$stdout;stderr=$stderr}
  if($result -eq 'passed'){Write-Host "[ OK ] $($t.id) ($($sw.ElapsedMilliseconds) ms)"}else{Write-Host "[FAIL] $($t.id): $result, exit $exitCode"}
  if($FailFast -and $result -ne 'passed'){break}
}
$reportPath=if([IO.Path]::IsPathRooted($Report)){$Report}else{Join-Path (Split-Path -Parent $manifestPath) $Report};$reportDir=Split-Path -Parent $reportPath;if($reportDir){$null=New-Item -ItemType Directory -Force -Path $reportDir}
$summary=[ordered]@{format='novaoryn-test-report-v1';generatedUtc=(Get-Date).ToUniversalTime().ToString('o');manifest=$manifestPath;selected=$selected.Count;run=$results.Count;passed=@($results|Where-Object result -eq 'passed').Count;failed=@($results|Where-Object result -eq 'failed').Count;timedOut=@($results|Where-Object result -eq 'timeout').Count;results=$results}
$summary|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $reportPath -Encoding UTF8
Write-Host "[INFO] Test report: $reportPath"
if($failed -gt 0){Write-Host "[FAIL] NovaOryn tests failed: $failed";exit 1};Write-Host "[ OK ] NovaOryn tests passed: $($results.Count)";exit 0
