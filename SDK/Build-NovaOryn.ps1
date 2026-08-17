[CmdletBinding()]
param(
    [string]$Project = "",
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [ValidateRange(5, 300)][int]$BootTimeoutSeconds = 30,
    [switch]$Run,
    [switch]$NoRun,
    [switch]$DryRun,
    [switch]$Validate
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# NovaOryn 0.35.22 migration guard.
# Filesystem implementations are end-user selectable projects. An incremental
# overlay from 0.35.20 can leave KernelFat32.cs on disk even though the 0.35.21
# contracts have already removed its FAT-specific types. Remove those stale files
# before any project is compiled.
$obsoleteBuiltInFileSystemFiles = @(
    (Join-Path $root "src\NovaOryn.Kernel.Storage\KernelFat32.cs"),
    (Join-Path $root "templates\NovaOrynKernel\Sdk\NovaOryn.Kernel.Storage\KernelFat32.cs"),
    (Join-Path $root "src\NovaOryn.VisualStudio\ProjectTemplates\CSharp\1033\NovaOrynKernel\Sdk\NovaOryn.Kernel.Storage\KernelFat32.cs")
)
foreach ($obsoleteFileSystemFile in $obsoleteBuiltInFileSystemFiles) {
    if (-not (Test-Path -LiteralPath $obsoleteFileSystemFile -PathType Leaf)) { continue }
    Write-Host "[INFO] Removing obsolete built-in filesystem source: $obsoleteFileSystemFile"
    Remove-Item -LiteralPath $obsoleteFileSystemFile -Force
    if (Test-Path -LiteralPath $obsoleteFileSystemFile -PathType Leaf) {
        throw "Could not remove obsolete built-in filesystem source: $obsoleteFileSystemFile"
    }
    Write-Host "[ OK ] Removed obsolete built-in filesystem source."
}


function Find-Executable {
    param(
        [Parameter(Mandatory = $true)][string]$DisplayName,
        [AllowNull()][AllowEmptyCollection()][string[]]$Candidates
    )

    $usableCandidates = @($Candidates | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    foreach ($candidate in $usableCandidates) {
        $expanded = [Environment]::ExpandEnvironmentVariables($candidate)
        if (Test-Path -LiteralPath $expanded -PathType Leaf) {
            return (Resolve-Path -LiteralPath $expanded).Path
        }
    }

    if ($usableCandidates.Count -eq 0) {
        throw "Required build tool is unavailable: $DisplayName. No usable candidate paths were supplied."
    }
    throw "Required build tool is unavailable: $DisplayName. Checked: $($usableCandidates -join ', ')"
}

function Find-Firmware {
    param(
        [Parameter(Mandatory = $true)][string]$DisplayName,
        [AllowNull()][string]$RecordedPath,
        [Parameter(Mandatory = $true)][string]$QemuPath,
        [Parameter(Mandatory = $true)][string[]]$FileNames
    )

    if (-not [string]::IsNullOrWhiteSpace($RecordedPath)) {
        $expanded = [Environment]::ExpandEnvironmentVariables($RecordedPath)
        if (Test-Path -LiteralPath $expanded -PathType Leaf) {
            return (Resolve-Path -LiteralPath $expanded).Path
        }
    }

    $qemuDirectory = Split-Path -Parent $QemuPath
    $roots = @(
        $qemuDirectory,
        (Join-Path $qemuDirectory "share"),
        (Join-Path $qemuDirectory "share\qemu"),
        ([IO.Path]::GetFullPath((Join-Path $qemuDirectory "..\share"))),
        ([IO.Path]::GetFullPath((Join-Path $qemuDirectory "..\share\qemu"))),
        ([Environment]::ExpandEnvironmentVariables("%ProgramFiles%\qemu")),
        ([Environment]::ExpandEnvironmentVariables("%ProgramFiles(x86)%\qemu")),
        ([Environment]::ExpandEnvironmentVariables("%LOCALAPPDATA%\Programs\qemu"))
    ) | Select-Object -Unique

    foreach ($searchRoot in $roots) {
        if (-not (Test-Path -LiteralPath $searchRoot -PathType Container)) { continue }
        foreach ($fileName in $FileNames) {
            $direct = Join-Path $searchRoot $fileName
            if (Test-Path -LiteralPath $direct -PathType Leaf) {
                return (Resolve-Path -LiteralPath $direct).Path
            }
            $recursive = Get-ChildItem -LiteralPath $searchRoot -Filter $fileName -File -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($null -ne $recursive) {
                return $recursive.FullName
            }
        }
    }

    throw "$DisplayName was not found beside the QEMU installation. Run Install-NovaOrynToolchain.bat."
}

$dotnet = Find-Executable -DisplayName ".NET SDK dotnet.exe" -Candidates @(
    (Join-Path $root ".toolchain\DotNet\dotnet.exe")
)

$toolPathsFile = Join-Path $root ".toolchain\NovaOryn.ToolPaths.json"
$paths = $null
if (Test-Path -LiteralPath $toolPathsFile -PathType Leaf) {
    try {
        $paths = Get-Content -LiteralPath $toolPathsFile -Raw | ConvertFrom-Json
    } catch {
        throw "Tool-path manifest is invalid: $toolPathsFile. $($_.Exception.Message)"
    }
}

function Get-RecordedPath {
    param([string[]]$Names)
    if ($null -eq $paths) { return $null }
    foreach ($name in $Names) {
        $property = $paths.PSObject.Properties[$name]
        if ($null -ne $property -and -not [string]::IsNullOrWhiteSpace([string]$property.Value)) {
            return [string]$property.Value
        }
    }
    return $null
}

$llvmRoot = Join-Path $root ".toolchain\LLVM\bin"
$lldLink = Find-Executable -DisplayName "LLD linker (lld-link.exe)" -Candidates @(
    (Join-Path $llvmRoot "lld-link.exe"),
    (Get-RecordedPath @("lld-link.exe", "lldLink"))
)
$llvmNm = Find-Executable -DisplayName "LLVM symbol tool (llvm-nm.exe)" -Candidates @(
    (Join-Path $llvmRoot "llvm-nm.exe"),
    (Get-RecordedPath @("llvm-nm.exe", "llvmNm"))
)
$nasm = Find-Executable -DisplayName "NASM assembler (nasm.exe)" -Candidates @(
    (Get-RecordedPath @("nasm.exe", "nasm", "nasmPath")),
    "%LOCALAPPDATA%\bin\NASM\nasm.exe",
    "%ProgramFiles%\NASM\nasm.exe",
    "%ProgramFiles(x86)%\NASM\nasm.exe",
    "%LOCALAPPDATA%\Microsoft\WinGet\Links\nasm.exe",
    ((Get-Command nasm.exe -ErrorAction SilentlyContinue).Source)
)
$toolchainManifestPath = Join-Path $root "toolchain\NovaOryn.Toolchain.json"
$toolchainManifest = Get-Content -LiteralPath $toolchainManifestPath -Raw | ConvertFrom-Json
$ilcVersion = [string]$toolchainManifest.nativeAot.packageVersion
$ilc = Find-Executable -DisplayName "NativeAOT compiler (ilc.exe)" -Candidates @(
    (Get-RecordedPath @("ilc", "ilc.exe", "ilcPath")),
    (Join-Path $root ".toolchain\NuGetPackages\runtime.win-x64.microsoft.dotnet.ilcompiler\$ilcVersion\tools\ilc.exe"),
    (Join-Path $env:USERPROFILE ".nuget\packages\runtime.win-x64.microsoft.dotnet.ilcompiler\$ilcVersion\tools\ilc.exe")
)

Write-Host "[ OK ] dotnet : $dotnet"
Write-Host "[ OK ] lld-link: $lldLink"
Write-Host "[ OK ] llvm-nm: $llvmNm"
Write-Host "[ OK ] nasm    : $nasm"
Write-Host "[ OK ] ilc     : $ilc"

$nativeOutput = Join-Path $root "Artifacts\Native\x64"
New-Item -ItemType Directory -Path $nativeOutput -Force | Out-Null

Write-Host "[INFO] Assembling x64 UEFI entry objects."
$entryNasmArguments = @("-f", "win64")
if ($Configuration -eq "Debug") {
    $entryNasmArguments += "-dNOVAORYN_DEBUG=1"
    Write-Host "[INFO] Debug UEFI image anchor enabled for source-breakpoint relocation."
}
$entryNasmArguments += (Join-Path $root "native\x64\Entry.asm")
$entryNasmArguments += "-o"
$entryNasmArguments += (Join-Path $nativeOutput "Entry.obj")
& $nasm @entryNasmArguments
if ($LASTEXITCODE -ne 0) { throw "NASM failed for Entry.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\Cpu.asm") -o (Join-Path $nativeOutput "Cpu.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for Cpu.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\Runtime.asm") -o (Join-Path $nativeOutput "Runtime.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for Runtime.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\Descriptors.asm") -o (Join-Path $nativeOutput "Descriptors.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for Descriptors.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\Interrupts.asm") -o (Join-Path $nativeOutput "Interrupts.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for Interrupts.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\InterruptControllers.asm") -o (Join-Path $nativeOutput "InterruptControllers.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for InterruptControllers.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\Paging.asm") -o (Join-Path $nativeOutput "Paging.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for Paging.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\Syscalls.asm") -o (Join-Path $nativeOutput "Syscalls.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for Syscalls.asm with exit code $LASTEXITCODE." }
& $nasm -f win64 (Join-Path $root "native\x64\UserMode.asm") -o (Join-Path $nativeOutput "UserMode.obj")
if ($LASTEXITCODE -ne 0) { throw "NASM failed for UserMode.asm with exit code $LASTEXITCODE." }

if ($Validate) {
    Write-Host "[INFO] Kernel build mode: VALIDATE (full SDK solution + all independent tests)."
    Write-Host "[INFO] Building NovaOryn executable tools."
    & $dotnet build (Join-Path $root "NovaOryn.sln") --configuration $Configuration --property:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn solution build failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running independent NovaOryn policy test programs."
    $policyTestPrograms = @(
        "NovaOryn.ApiPolicy.Tests",
        "NovaOryn.BuildPolicy.Tests",
        "NovaOryn.BootPolicy.Tests",
        "NovaOryn.MemoryPolicy.Tests",
        "NovaOryn.TemplatePolicy.Tests",
        "NovaOryn.DocumentationPolicy.Tests",
        "NovaOryn.ReleasePolicy.Tests"
    )
    foreach ($policyTestProgram in $policyTestPrograms) {
        Write-Host "[INFO] Policy test: $policyTestProgram..."
        $policyTestProject = Join-Path $root "tests\$policyTestProgram\$policyTestProgram.csproj"
        if (-not (Test-Path -LiteralPath $policyTestProject -PathType Leaf)) { throw "Policy test project was not found: $policyTestProject" }

        & $dotnet build $policyTestProject --configuration $Configuration --property:Platform="Any CPU" --nologo
        if ($LASTEXITCODE -ne 0) { throw "Policy test project '$policyTestProgram' failed to build with exit code $LASTEXITCODE." }

        $policyTestExecutable = Join-Path $root "tests\$policyTestProgram\bin\Any CPU\$Configuration\net10.0\$policyTestProgram.dll"
        if (-not (Test-Path -LiteralPath $policyTestExecutable -PathType Leaf)) { throw "Policy test executable was not produced: $policyTestExecutable" }
        & $dotnet $policyTestExecutable
        if ($LASTEXITCODE -ne 0) { throw "Policy test '$policyTestProgram' failed with exit code $LASTEXITCODE." }
    }
    Write-Host "[ OK ] All independent NovaOryn policy test programs passed."

    Write-Host "[INFO] Running NovaOryn boot-memory tests."
    $memoryTests = Join-Path $root "tests\NovaOryn.Memory.Tests\bin\$Configuration\net10.0\NovaOryn.Memory.Tests.dll"
    if (-not (Test-Path -LiteralPath $memoryTests -PathType Leaf)) { throw "NovaOryn boot-memory test executable was not produced: $memoryTests" }
    & $dotnet $memoryTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn boot-memory tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn physical-memory tests."
    $physicalMemoryTests = Join-Path $root "tests\NovaOryn.PhysicalMemory.Tests\bin\$Configuration\net10.0\NovaOryn.PhysicalMemory.Tests.dll"
    if (-not (Test-Path -LiteralPath $physicalMemoryTests -PathType Leaf)) { throw "NovaOryn physical-memory test executable was not produced: $physicalMemoryTests" }
    & $dotnet $physicalMemoryTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn physical-memory tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn virtual-memory tests."
    $virtualMemoryTests = Join-Path $root "tests\NovaOryn.VirtualMemory.Tests\bin\$Configuration\net10.0\NovaOryn.VirtualMemory.Tests.dll"
    if (-not (Test-Path -LiteralPath $virtualMemoryTests -PathType Leaf)) { throw "NovaOryn virtual-memory test executable was not produced: $virtualMemoryTests" }
    & $dotnet $virtualMemoryTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn virtual-memory tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn kernel address-space tests."
    $addressSpaceTests = Join-Path $root "tests\NovaOryn.AddressSpace.Tests\bin\$Configuration\net10.0\NovaOryn.AddressSpace.Tests.dll"
    if (-not (Test-Path -LiteralPath $addressSpaceTests -PathType Leaf)) { throw "NovaOryn kernel address-space test executable was not produced: $addressSpaceTests" }
    & $dotnet $addressSpaceTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn kernel address-space tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn early-allocator and kernel-heap tests."
    $heapTests = Join-Path $root "tests\NovaOryn.Heap.Tests\bin\$Configuration\net10.0\NovaOryn.Heap.Tests.dll"
    if (-not (Test-Path -LiteralPath $heapTests -PathType Leaf)) { throw "NovaOryn heap test executable was not produced: $heapTests" }
    & $dotnet $heapTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn heap tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn timers and clocks tests."
    $timeTestProject = Join-Path $root "tests\NovaOryn.Time.Tests\NovaOryn.Time.Tests.csproj"
    & $dotnet build $timeTestProject --configuration $Configuration --property:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn timers and clocks tests failed to build with exit code $LASTEXITCODE." }
    $timeTests = Join-Path $root "tests\NovaOryn.Time.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Time.Tests.dll"
    if (-not (Test-Path -LiteralPath $timeTests -PathType Leaf)) { throw "NovaOryn timers and clocks test executable was not produced: $timeTests" }
    & $dotnet $timeTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn timers and clocks tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn SMP and per-CPU state tests."
    $smpTestProject = Join-Path $root "tests\NovaOryn.Smp.Tests\NovaOryn.Smp.Tests.csproj"
    & $dotnet build $smpTestProject --configuration $Configuration --property:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn SMP and per-CPU state tests failed to build with exit code $LASTEXITCODE." }
    $smpTests = Join-Path $root "tests\NovaOryn.Smp.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Smp.Tests.dll"
    if (-not (Test-Path -LiteralPath $smpTests -PathType Leaf)) { throw "NovaOryn SMP and per-CPU state test executable was not produced: $smpTests" }
    & $dotnet $smpTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn SMP and per-CPU state tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn scheduler and threads tests."
    $schedulerTestProject = Join-Path $root "tests\NovaOryn.Scheduler.Tests\NovaOryn.Scheduler.Tests.csproj"
    & $dotnet build $schedulerTestProject --configuration $Configuration --property:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn scheduler and threads tests failed to build with exit code $LASTEXITCODE." }
    $schedulerTests = Join-Path $root "tests\NovaOryn.Scheduler.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Scheduler.Tests.dll"
    if (-not (Test-Path -LiteralPath $schedulerTests -PathType Leaf)) { throw "NovaOryn scheduler and threads test executable was not produced: $schedulerTests" }
    & $dotnet $schedulerTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn scheduler and threads tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn user/kernel separation tests."
    $protectionTestProject = Join-Path $root "tests\NovaOryn.Protection.Tests\NovaOryn.Protection.Tests.csproj"
    & $dotnet build $protectionTestProject --configuration $Configuration --property:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn user/kernel separation tests failed to build with exit code $LASTEXITCODE." }
    $protectionTests = Join-Path $root "tests\NovaOryn.Protection.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Protection.Tests.dll"
    if (-not (Test-Path -LiteralPath $protectionTests -PathType Leaf)) { throw "NovaOryn user/kernel separation test executable was not produced: $protectionTests" }
    & $dotnet $protectionTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn user/kernel separation tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn system-call tests."
    $systemCallTestProject = Join-Path $root "tests\NovaOryn.SystemCalls.Tests\NovaOryn.SystemCalls.Tests.csproj"
    & $dotnet build $systemCallTestProject --configuration $Configuration --property:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn system-call tests failed to build with exit code $LASTEXITCODE." }
    $systemCallTests = Join-Path $root "tests\NovaOryn.SystemCalls.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.SystemCalls.Tests.dll"
    if (-not (Test-Path -LiteralPath $systemCallTests -PathType Leaf)) { throw "NovaOryn system-call test executable was not produced: $systemCallTests" }
    & $dotnet $systemCallTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn system-call tests failed with exit code $LASTEXITCODE." }

    $processTestProject = Join-Path $root "tests\NovaOryn.Processes.Tests\NovaOryn.Processes.Tests.csproj"
    & $dotnet build $processTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn process/executable-loading tests failed to build with exit code $LASTEXITCODE." }
    $processTests = Join-Path $root "tests\NovaOryn.Processes.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Processes.Tests.dll"
    if (-not (Test-Path -LiteralPath $processTests -PathType Leaf)) { throw "NovaOryn process/executable-loading test executable was not produced: $processTests" }
    & $dotnet $processTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn process/executable-loading tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn driver-framework tests."
    $driverTestProject = Join-Path $root "tests\NovaOryn.Drivers.Tests\NovaOryn.Drivers.Tests.csproj"
    & $dotnet build $driverTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn driver-framework tests failed to build with exit code $LASTEXITCODE." }
    $driverTests = Join-Path $root "tests\NovaOryn.Drivers.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Drivers.Tests.dll"
    if (-not (Test-Path -LiteralPath $driverTests -PathType Leaf)) { throw "NovaOryn driver-framework test executable was not produced: $driverTests" }
    & $dotnet $driverTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn driver-framework tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn storage/filesystem tests."
    $storageTestProject = Join-Path $root "tests\NovaOryn.Storage.Tests\NovaOryn.Storage.Tests.csproj"
    & $dotnet build $storageTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn storage/filesystem tests failed to build with exit code $LASTEXITCODE." }
    $storageTests = Join-Path $root "tests\NovaOryn.Storage.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Storage.Tests.dll"
    if (-not (Test-Path -LiteralPath $storageTests -PathType Leaf)) { throw "NovaOryn storage/filesystem test executable was not produced: $storageTests" }
    & $dotnet $storageTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn storage/filesystem tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn networking tests."
    $networkTestProject = Join-Path $root "tests\NovaOryn.Networking.Tests\NovaOryn.Networking.Tests.csproj"
    & $dotnet build $networkTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn networking tests failed to build with exit code $LASTEXITCODE." }
    $networkTests = Join-Path $root "tests\NovaOryn.Networking.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Networking.Tests.dll"
    if (-not (Test-Path -LiteralPath $networkTests -PathType Leaf)) { throw "NovaOryn networking test executable was not produced: $networkTests" }
    & $dotnet $networkTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn networking tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn PCI/PCIe tests."
    $pciTestProject = Join-Path $root "tests\NovaOryn.Pci.Tests\NovaOryn.Pci.Tests.csproj"
    & $dotnet build $pciTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn PCI/PCIe tests failed to build with exit code $LASTEXITCODE." }
    $pciTests = Join-Path $root "tests\NovaOryn.Pci.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Pci.Tests.dll"
    if (-not (Test-Path -LiteralPath $pciTests -PathType Leaf)) { throw "NovaOryn PCI/PCIe test executable was not produced: $pciTests" }
    & $dotnet $pciTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn PCI/PCIe tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn NVMe tests."
    $nvmeTestProject = Join-Path $root "tests\NovaOryn.Nvme.Tests\NovaOryn.Nvme.Tests.csproj"
    & $dotnet build $nvmeTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn NVMe tests failed to build with exit code $LASTEXITCODE." }
    $nvmeTests = Join-Path $root "tests\NovaOryn.Nvme.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Nvme.Tests.dll"
    if (-not (Test-Path -LiteralPath $nvmeTests -PathType Leaf)) { throw "NovaOryn NVMe test executable was not produced: $nvmeTests" }
    & $dotnet $nvmeTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn NVMe tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn AHCI/SATA tests."
    $ahciTestProject = Join-Path $root "tests\NovaOryn.Ahci.Tests\NovaOryn.Ahci.Tests.csproj"
    & $dotnet build $ahciTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn AHCI/SATA tests failed to build with exit code $LASTEXITCODE." }
    $ahciTests = Join-Path $root "tests\NovaOryn.Ahci.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Ahci.Tests.dll"
    if (-not (Test-Path -LiteralPath $ahciTests -PathType Leaf)) { throw "NovaOryn AHCI/SATA test executable was not produced: $ahciTests" }
    & $dotnet $ahciTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn AHCI/SATA tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn VirtIO tests."
    $virtioTestProject = Join-Path $root "tests\NovaOryn.Virtio.Tests\NovaOryn.Virtio.Tests.csproj"
    & $dotnet build $virtioTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn VirtIO tests failed to build with exit code $LASTEXITCODE." }
    $virtioTests = Join-Path $root "tests\NovaOryn.Virtio.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Virtio.Tests.dll"
    if (-not (Test-Path -LiteralPath $virtioTests -PathType Leaf)) { throw "NovaOryn VirtIO test executable was not produced: $virtioTests" }
    & $dotnet $virtioTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn VirtIO tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn serial tests."
    $serialTestProject = Join-Path $root "tests\NovaOryn.Serial.Tests\NovaOryn.Serial.Tests.csproj"
    & $dotnet build $serialTestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn serial tests failed to build with exit code $LASTEXITCODE." }
    $serialTests = Join-Path $root "tests\NovaOryn.Serial.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Serial.Tests.dll"
    if (-not (Test-Path -LiteralPath $serialTests -PathType Leaf)) { throw "NovaOryn serial test executable was not produced: $serialTests" }
    & $dotnet $serialTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn serial tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn PS/2 input and keyboard-layout tests."
    $ps2TestProject = Join-Path $root "tests\NovaOryn.Ps2.Tests\NovaOryn.Ps2.Tests.csproj"
    & $dotnet build $ps2TestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn PS/2 tests failed to build with exit code $LASTEXITCODE." }
    $ps2Tests = Join-Path $root "tests\NovaOryn.Ps2.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Ps2.Tests.dll"
    if (-not (Test-Path -LiteralPath $ps2Tests -PathType Leaf)) { throw "NovaOryn PS/2 test executable was not produced: $ps2Tests" }
    & $dotnet $ps2Tests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn PS/2 tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn Intel E1000/E1000e tests."
    $e1000TestProject = Join-Path $root "tests\NovaOryn.E1000.Tests\NovaOryn.E1000.Tests.csproj"
    & $dotnet build $e1000TestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn Intel E1000/E1000e tests failed to build with exit code $LASTEXITCODE." }
    $e1000Tests = Join-Path $root "tests\NovaOryn.E1000.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.E1000.Tests.dll"
    if (-not (Test-Path -LiteralPath $e1000Tests -PathType Leaf)) { throw "NovaOryn Intel E1000/E1000e test executable was not produced: $e1000Tests" }
    & $dotnet $e1000Tests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn Intel E1000/E1000e tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn Realtek RTL8168/RTL8111 tests."
    $rtl8168TestProject = Join-Path $root "tests\NovaOryn.Rtl8168.Tests\NovaOryn.Rtl8168.Tests.csproj"
    & $dotnet build $rtl8168TestProject -c $Configuration -p:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn Realtek RTL8168/RTL8111 tests failed to build with exit code $LASTEXITCODE." }
    $rtl8168Tests = Join-Path $root "tests\NovaOryn.Rtl8168.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.Rtl8168.Tests.dll"
    if (-not (Test-Path -LiteralPath $rtl8168Tests -PathType Leaf)) { throw "NovaOryn Realtek RTL8168/RTL8111 test executable was not produced: $rtl8168Tests" }
    & $dotnet $rtl8168Tests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn Realtek RTL8168/RTL8111 tests failed with exit code $LASTEXITCODE." }

    Write-Host "[INFO] Running NovaOryn interrupt-broker tests."
    $interruptBrokerTestProject = Join-Path $root "tests\NovaOryn.InterruptBroker.Tests\NovaOryn.InterruptBroker.Tests.csproj"
    & $dotnet build $interruptBrokerTestProject --configuration $Configuration --property:Platform="Any CPU" --nologo
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn interrupt-broker tests failed to build with exit code $LASTEXITCODE." }
    $interruptBrokerTests = Join-Path $root "tests\NovaOryn.InterruptBroker.Tests\bin\Any CPU\$Configuration\net10.0\NovaOryn.InterruptBroker.Tests.dll"
    if (-not (Test-Path -LiteralPath $interruptBrokerTests -PathType Leaf)) { throw "NovaOryn interrupt-broker test executable was not produced: $interruptBrokerTests" }
    & $dotnet $interruptBrokerTests
    if ($LASTEXITCODE -ne 0) { throw "NovaOryn interrupt-broker tests failed with exit code $LASTEXITCODE." }


} else {
    Write-Host "[INFO] Kernel build mode: FAST (selected project + required build tools only)."
    Write-Host "[INFO] Full SDK solution tests and documentation are skipped. Use Validate-NovaOryn.bat for exhaustive validation."
    $requiredToolProjects = @(
        "src\NovaOryn.ManagedCompiler\NovaOryn.ManagedCompiler.csproj",
        "src\NovaOryn.Linker\NovaOryn.Linker.csproj",
        "src\NovaOryn.ImageBuilder\NovaOryn.ImageBuilder.csproj",
        "src\NovaOryn.QemuLauncher\NovaOryn.QemuLauncher.csproj",
        "src\NovaOryn.ProjectCreator\NovaOryn.ProjectCreator.csproj"
    )
    foreach ($requiredToolProjectRelative in $requiredToolProjects) {
        $requiredToolProject = Join-Path $root $requiredToolProjectRelative
        Write-Host "[INFO] Building required NovaOryn tool: $requiredToolProjectRelative"
        & $dotnet build $requiredToolProject --configuration $Configuration --property:Platform="Any CPU" --nologo
        if ($LASTEXITCODE -ne 0) { throw "Required NovaOryn build tool failed to build: $requiredToolProjectRelative (exit code $LASTEXITCODE)." }
    }


}

# Required tools are explicitly built with Platform="Any CPU" above. The SDK
# must execute those exact outputs rather than a stale bin\$Configuration copy
# left by an older build that did not use the platform-specific output folder.
$requiredToolOutput = Join-Path "bin\Any CPU" "$Configuration\net10.0"
$compiler = Join-Path $root "src\NovaOryn.ManagedCompiler\$requiredToolOutput\NovaOryn.ManagedCompiler.dll"
$linker = Join-Path $root "src\NovaOryn.Linker\$requiredToolOutput\NovaOryn.Linker.dll"
$imageBuilder = Join-Path $root "src\NovaOryn.ImageBuilder\$requiredToolOutput\NovaOryn.ImageBuilder.dll"
$qemuLauncher = Join-Path $root "src\NovaOryn.QemuLauncher\$requiredToolOutput\NovaOryn.QemuLauncher.dll"
foreach ($tool in @(
    @{Name='NovaOryn.ManagedCompiler';Path=$compiler},
    @{Name='NovaOryn.Linker';Path=$linker},
    @{Name='NovaOryn.ImageBuilder';Path=$imageBuilder},
    @{Name='NovaOryn.QemuLauncher';Path=$qemuLauncher}
)) {
    if (-not (Test-Path -LiteralPath $tool.Path -PathType Leaf)) {
        throw "$($tool.Name) was not produced: $($tool.Path)"
    }
}

$projectCreator = Join-Path $root "src\NovaOryn.ProjectCreator\$requiredToolOutput\NovaOryn.ProjectCreator.dll"
if (-not (Test-Path -LiteralPath $projectCreator -PathType Leaf)) {
    throw "NovaOryn.ProjectCreator was not produced: $projectCreator"
}
Write-Host "[ OK ] ManagedCompiler runtime: $compiler"
Write-Host "[ OK ] Linker runtime         : $linker"
Write-Host "[ OK ] ImageBuilder runtime   : $imageBuilder"
Write-Host "[ OK ] QemuLauncher runtime   : $qemuLauncher"
Write-Host "[ OK ] ProjectCreator runtime : $projectCreator"

$externalKernelDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) "Source\Repos\NovaOrynKernel"
if (Test-Path -LiteralPath $externalKernelDirectory -PathType Container) {
    Write-Host "[INFO] Refreshing the existing external NovaOryn kernel project."
    & $dotnet $projectCreator create --output $externalKernelDirectory --sdk-root $root
    if ($LASTEXITCODE -ne 0) { throw "External NovaOryn kernel project refresh failed with exit code $LASTEXITCODE." }

    $legacyRootKernel = Join-Path $externalKernelDirectory "Kernel.cs"
    if (Test-Path -LiteralPath $legacyRootKernel -PathType Leaf) {
        throw "External kernel migration left an obsolete root Kernel.cs in place: $legacyRootKernel"
    }

    $externalUserKernel = Join-Path $externalKernelDirectory "Kernel\Kernel.cs"
    $externalKernelSource = Ensure-HighLevelUserKernelSource `
        -KernelPath $externalUserKernel `
        -SdkRoot $root `
        -DisplayName "external user kernel"
    foreach ($forbiddenKernelToken in @("DllImport", "internal static class Native", "WritePort8", "RuntimeExport", "FramebufferConsole", "0x3F8")) {
        if ($externalKernelSource.IndexOf($forbiddenKernelToken, [StringComparison]::Ordinal) -ge 0) {
            throw "External user kernel still exposes low-level token '$forbiddenKernelToken': $externalUserKernel"
        }
    }

    Write-Host "[ OK ] External kernel project refreshed: $externalKernelDirectory"
    Write-Host "[ OK ] External user kernel is high-level only: $externalUserKernel"
}

function Ensure-HighLevelUserKernelSource {
    param(
        [Parameter(Mandatory = $true)][string]$KernelPath,
        [Parameter(Mandatory = $true)][string]$SdkRoot,
        [Parameter(Mandatory = $true)][string]$DisplayName
    )

    $canonicalKernelPath = Join-Path $SdkRoot "templates\\NovaOrynKernel\\Kernel\\Kernel.cs"
    if (-not (Test-Path -LiteralPath $canonicalKernelPath -PathType Leaf)) {
        throw "Canonical NovaOryn high-level user kernel template was not found: $canonicalKernelPath"
    }

    $canonicalSource = [IO.File]::ReadAllText($canonicalKernelPath)
    if ([string]::IsNullOrWhiteSpace($canonicalSource)) {
        throw "Canonical NovaOryn high-level user kernel template is empty: $canonicalKernelPath"
    }

    $needsRepair = -not (Test-Path -LiteralPath $KernelPath -PathType Leaf)
    if (-not $needsRepair) {
        $currentSource = [IO.File]::ReadAllText($KernelPath)
        $needsRepair = [string]::IsNullOrWhiteSpace($currentSource)
    }

    if ($needsRepair) {
        $kernelDirectory = Split-Path -Parent $KernelPath
        if (-not (Test-Path -LiteralPath $kernelDirectory -PathType Container)) {
            New-Item -ItemType Directory -Path $kernelDirectory -Force | Out-Null
        }
        [IO.File]::WriteAllText($KernelPath, $canonicalSource)
        Write-Host "[ OK ] Repaired empty $DisplayName from canonical SDK template: $KernelPath"
    }

    $verifiedSource = [IO.File]::ReadAllText($KernelPath)
    if ([string]::IsNullOrWhiteSpace($verifiedSource)) {
        throw "$DisplayName is empty immediately before compilation: $KernelPath"
    }

    $verifiedLength = (Get-Item -LiteralPath $KernelPath).Length
    Write-Host "[ OK ] $DisplayName source verified: $verifiedLength bytes"
    return $verifiedSource
}

$defaultKernelDirectory = Join-Path $root "src\NovaOryn.Kernel.Bootstrap"
$defaultProjectManifest = Join-Path $defaultKernelDirectory "NovaOrynProject.json"
$projectManifest = if ([string]::IsNullOrWhiteSpace($Project)) {
    $defaultProjectManifest
} elseif ([IO.Path]::IsPathRooted($Project)) {
    [IO.Path]::GetFullPath($Project)
} else {
    [IO.Path]::GetFullPath((Join-Path $root $Project))
}

if (-not (Test-Path -LiteralPath $projectManifest -PathType Leaf)) {
    throw "NovaOryn project manifest was not found: $projectManifest"
}

if (-not [string]::Equals([IO.Path]::GetFullPath($projectManifest), [IO.Path]::GetFullPath($defaultProjectManifest), [StringComparison]::OrdinalIgnoreCase)) {
    $selectedProjectDirectory = Split-Path -Parent $projectManifest
    Write-Host "[INFO] Refreshing the selected NovaOryn project before compilation: $selectedProjectDirectory"
    & $dotnet $projectCreator create --output $selectedProjectDirectory --sdk-root $root
    if ($LASTEXITCODE -ne 0) { throw "Selected NovaOryn project refresh failed with exit code $LASTEXITCODE." }
    $projectManifest = Join-Path $selectedProjectDirectory "NovaOrynProject.json"

    $selectedUserKernel = Join-Path $selectedProjectDirectory "Kernel\Kernel.cs"
    $selectedKernelSource = Ensure-HighLevelUserKernelSource `
        -KernelPath $selectedUserKernel `
        -SdkRoot $root `
        -DisplayName "selected user kernel"
    foreach ($forbiddenKernelToken in @("DllImport", "class Native", "WritePort8", "RuntimeExport", "NativeEntry", "FramebufferConsole", "0x3F8", "InitializeSerial")) {
        if ($selectedKernelSource.IndexOf($forbiddenKernelToken, [StringComparison]::Ordinal) -ge 0) {
            throw "Selected user kernel still exposes low-level token '$forbiddenKernelToken': $selectedUserKernel"
        }
    }
    Write-Host "[ OK ] Selected user kernel is high-level only: $selectedUserKernel"
}

Write-Host "[ OK ] C# kernel project manifest: $projectManifest"

Write-Host "[INFO] NovaOryn configured target architecture will be validated before managed compilation."
try {
    $configuredProject = Get-Content -LiteralPath $projectManifest -Raw | ConvertFrom-Json
    $configuredArchitecture = [string]$configuredProject.TargetArchitecture
    $configuredKernelModel = [string]$configuredProject.KernelModel
    if ([string]::IsNullOrWhiteSpace($configuredArchitecture)) { $configuredArchitecture = "x64" }
    if ([string]::IsNullOrWhiteSpace($configuredKernelModel)) { $configuredKernelModel = "Monolithic" }
    Write-Host "[ OK ] Target architecture: $configuredArchitecture"
    Write-Host "[ OK ] Kernel model       : $configuredKernelModel"
    if ($configuredArchitecture -notin @("x64", "X64")) {
        throw "The project is configured for '$configuredArchitecture', but this NovaOryn installation currently contains only the x64 architecture pack. Install/implement the matching architecture pack or reopen NovaOryn: Configure Project and select x64. NovaOryn will not silently build an x64 kernel for a different configured target."
    }
} catch {
    if ($_.Exception.Message -like "The project is configured for*") { throw }
    throw "Could not validate NovaOryn project configuration: $($_.Exception.Message)"
}

$projectData = Get-Content -LiteralPath $projectManifest -Raw | ConvertFrom-Json
$projectDirectory = Split-Path -Parent $projectManifest
$outputDirectory = if ([IO.Path]::IsPathRooted([string]$projectData.OutputDirectory)) {
    [IO.Path]::GetFullPath([string]$projectData.OutputDirectory)
} else {
    [IO.Path]::GetFullPath((Join-Path $projectDirectory ([string]$projectData.OutputDirectory)))
}
$imagePath = Join-Path $outputDirectory (([string]$projectData.Name) + ".img")

$dry = @()
if ($DryRun) { $dry = @("--dry-run") }

& $dotnet $compiler compile $projectManifest --dotnet $dotnet --ilc $ilc --configuration $Configuration --sdk-root $root @dry
if ($LASTEXITCODE -ne 0) { throw "Managed compilation failed with exit code $LASTEXITCODE." }

& $dotnet $linker link $projectManifest --lld-link $lldLink --llvm-nm $llvmNm --nasm $nasm --native-root $nativeOutput @dry
if ($LASTEXITCODE -ne 0) { throw "Native link failed with exit code $LASTEXITCODE." }

& $dotnet $imageBuilder create $projectManifest --output $imagePath @dry
if ($LASTEXITCODE -ne 0) { throw "Bootable EFI image creation failed with exit code $LASTEXITCODE." }

if ($NoRun -or -not $Run) {
    Write-Host "[ OK ] NovaOryn x64 NativeAOT build and FAT32 image creation completed."
    if ($NoRun) {
        Write-Host "[INFO] QEMU launch was skipped because -NoRun was supplied."
    } else {
        Write-Host "[INFO] QEMU launch is disabled for a normal build. Supply -Run or use the Visual Studio Run command to launch it."
    }
    exit 0
}

$qemu = Find-Executable -DisplayName "QEMU x64 system emulator" -Candidates @(
    (Get-RecordedPath @("qemuSystemX64", "qemu-system-x86_64.exe")),
    "%ProgramFiles%\qemu\qemu-system-x86_64.exe",
    "%ProgramFiles(x86)%\qemu\qemu-system-x86_64.exe",
    "%LOCALAPPDATA%\Programs\qemu\qemu-system-x86_64.exe",
    "%LOCALAPPDATA%\Microsoft\WinGet\Links\qemu-system-x86_64.exe",
    ((Get-Command qemu-system-x86_64.exe -ErrorAction SilentlyContinue).Source)
)
$ovmfCode = Find-Firmware -DisplayName "x64 OVMF code firmware" -RecordedPath (Get-RecordedPath @("ovmfCodeX64", "ovmfCode")) -QemuPath $qemu -FileNames @("edk2-x86_64-code.fd", "OVMF_CODE.fd")
$ovmfVars = Find-Firmware -DisplayName "x64 OVMF variable-store template" -RecordedPath (Get-RecordedPath @("ovmfVarsX64", "ovmfVars")) -QemuPath $qemu -FileNames @("edk2-i386-vars.fd", "edk2-x86_64-vars.fd", "OVMF_VARS.fd")
Write-Host "[ OK ] qemu    : $qemu"
Write-Host "[ OK ] OVMF code: $ovmfCode"
Write-Host "[ OK ] OVMF vars: $ovmfVars"

& $dotnet $qemuLauncher run $projectManifest --qemu $qemu --image $imagePath --ovmf-code $ovmfCode --ovmf-vars $ovmfVars --timeout-seconds $BootTimeoutSeconds @dry
if ($LASTEXITCODE -ne 0) { throw "QEMU runtime acceptance failed with exit code $LASTEXITCODE." }

Write-Host "[ OK ] NovaOryn x64 NativeAOT boot-and-run acceptance completed."
