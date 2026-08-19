'use strict';

const path = require('path');
const { createRequire } = require('module');

const rootPackage = path.join(__dirname, 'package.json');
const rootRequire = createRequire(rootPackage);
const required = [
  '@theia/application-manager',
  '@theia/cli',
  '@theia/electron',
  'electron',
  'node-pty',
  'native-keymap',
  'drivelist',
  'keytar'
];

let failed = false;
for (const name of required) {
  try {
    const packagePath = rootRequire.resolve(`${name}/package.json`);
    const pkg = rootRequire(packagePath);
    console.log(`[INFO] Root Theia CLI dependency: ${name} ${pkg.version}`);
  } catch (error) {
    console.error(`[FAIL] Root Theia CLI cannot resolve ${name}.`);
    failed = true;
  }
}

if (failed) {
  console.error('[FAIL] Theia CLI dependency surface is incomplete at the repository root.');
  process.exit(1);
}
console.log('[ OK ] Root Theia CLI dependency surface is complete.');
