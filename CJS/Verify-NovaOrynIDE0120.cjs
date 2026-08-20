const fs = require('fs');
let fail = false;
const requireText = (file, terms) => {
  const text = fs.readFileSync(file, 'utf8');
  for (const term of terms) {
    if (!text.includes(term)) { console.error(`[FAIL] ${file}: missing ${term}`); fail = true; }
  }
  return text;
};
const build = requireText('Build-NovaOrynIDE.bat', ['applications\\electron\\lib\\.novaoryn-build-version', 'echo 0.12.0']);
const run = requireText('Run-NovaOrynIDE.bat', ['applications\\electron\\lib\\.novaoryn-build-version', 'NOVAORYN_BUILT_VERSION', '=="0.12.0"']);
if (run.includes('ConvertFrom-Json') && run.includes('BuildState')) { console.error('[FAIL] Run still depends on JSON build-state marker.'); fail = true; }
const protocol = requireText('packages/novaoryn-ide/src/common/novaoryn-protocol.ts', [
  'NovaOrynHardwareMatrixPreset', 'NovaOrynHardwareMatrixCase', 'runHardwareMatrix', 'readHardwareMatrixOutput'
]);
const service = requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', [
  "const NOVAORYN_IDE_VERSION = '0.12.0'", 'createHardwareMatrixCases', 'executeHardwareMatrix',
  "[1, 2, 4, 8]", "['virtio-blk', 'ahci', 'nvme']", "['virtio-net', 'e1000']", "['gop', 'virtio-gpu']",
  "['none', 'xhci']", "['uefi', 'bios']", 'NovaOryn.QemuHardwareMatrix.json',
  "serial.includes('NovaOryn KMain started.')", "serial.includes('NovaOryn> ')"
]);
const widget = requireText('packages/novaoryn-ide/src/browser/novaoryn-test-explorer-widget.tsx', [
  'QEMU Hardware Test Matrix', 'Balanced matrix', 'Full Cartesian matrix', 'Run Hardware Matrix'
]);
if (service.includes('KernelPanicTransport.Initialize()')) { console.error('[FAIL] comprehensive kernel generator exposes KernelPanicTransport.Initialize().'); fail = true; }
if (!service.includes('KernelStructuredLogging.Initialize()')) { console.error('[FAIL] explicit structured logging initialization is missing.'); fail = true; }
if (!fail) console.log('[ OK ] NovaOryn IDE 0.12.0 QEMU hardware test matrix contract verified.');
if (fail) process.exitCode = 1;
