param([Parameter(Position=0)][string]$Command="help",[Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
$ErrorActionPreference='Stop'; $root=$PSScriptRoot
function Run-Script([string]$name,[string[]]$args){$p=Join-Path $root $name;if(!(Test-Path $p)){Write-Error "Missing $name";return 1}& $p @args;return $LASTEXITCODE}
function Doctor {
  $manifest=Join-Path $root 'NovaOryn.SdkManifest.json'; $ok=$true
  Write-Host '[INFO] NovaOryn doctor'
  if(Test-Path $manifest){$m=Get-Content $manifest -Raw|ConvertFrom-Json;Write-Host "[ OK ] SDK manifest $($m.sdkVersion), API $($m.apiVersion), ABI $($m.abiVersion)"}else{Write-Host '[FAIL] SDK manifest missing';$ok=$false}
  $checks=@(@('dotnet','.toolchain\DotNet\dotnet.exe'),@('ILC','.toolchain\NuGetPackages'),@('LLVM','.toolchain\LLVM\bin\lld-link.exe'),@('QEMU','C:\Program Files\qemu\qemu-system-x86_64.exe'))
  foreach($c in $checks){$p=if([IO.Path]::IsPathRooted($c[1])){$c[1]}else{Join-Path $root $c[1]};if(Test-Path $p){Write-Host "[ OK ] $($c[0]): $p"}else{Write-Host "[WARN] $($c[0]) not found at expected path: $p"}}
  $nasm=(Get-Command nasm.exe -ErrorAction SilentlyContinue);if($nasm){Write-Host "[ OK ] NASM: $($nasm.Source)"}else{Write-Host '[WARN] NASM is not currently on PATH'}
  foreach($f in 'NovaOryn.ApiContract.json','NovaOryn.ApiBaseline.json','NovaOryn.SubsystemContracts.json','NovaOryn.QemuTestMatrix.json','NovaOryn.TestContract.json'){if(Test-Path (Join-Path $root $f)){Write-Host "[ OK ] $f"}else{Write-Host "[FAIL] $f missing";$ok=$false}}
  if($ok){Write-Host '[ OK ] SDK consistency checks passed';return 0};return 1
}
switch($Command.ToLowerInvariant()){
 'new' { exit (Run-Script 'Build-NovaOryn.ps1' @('-NewProject')+$Arguments) }
 'build' { exit (Run-Script 'Build-NovaOryn.ps1' $Arguments) }
 'run' { exit (Run-Script 'Build-NovaOryn.ps1' @('-Run')+$Arguments) }
 'debug' { exit (Run-Script 'Build-NovaOryn.ps1' @('-Debug')+$Arguments) }
 'test' { exit (Run-Script 'Run-NovaOrynTests.ps1' $Arguments) }
 'pack' { Write-Host '[INFO] Package contract: novaoryn-package-v1'; exit 0 }
 'doctor' { exit (Doctor) }
 default { Write-Host 'NovaOryn CLI: new | build | run | debug | test | pack | doctor'; exit 0 }
}
