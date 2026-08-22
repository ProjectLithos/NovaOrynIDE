const path = require('path');
const { spawnSync } = require('child_process');

const root = path.resolve(__dirname, '..');
const node = process.execPath;
const verifiers = [
  'CJS/Verify-NovaOrynIDE0113.cjs',
  'CJS/Verify-NovaOrynIDETestFramework0110.cjs',
  'CJS/Verify-NovaOrynIDE0210.cjs'
];

for (const verifier of verifiers) {
  console.log(`[INFO] Running ${verifier}...`);
  const result = spawnSync(node, [path.join(root, verifier)], {
    cwd: root,
    stdio: 'inherit',
    windowsHide: true
  });
  if (result.error) {
    console.error(`[FAIL] Could not start ${verifier}: ${result.error.message}`);
    process.exit(90);
  }
  const code = typeof result.status === 'number' ? result.status : 91;
  if (code !== 0) {
    console.error(`[FAIL] ${verifier} failed with exit code ${code}.`);
    process.exit(code || 1);
  }
  console.log(`[ OK ] ${verifier} completed successfully.`);
}

console.log('[ OK ] Final NovaOryn IDE verification suite completed; proceeding to the Theia production build.');
