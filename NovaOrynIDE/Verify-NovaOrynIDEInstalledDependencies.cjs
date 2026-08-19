'use strict';

const path = require('path');
const { createRequire } = require('module');

const expected = Object.freeze({
  theia: '1.74.0',
  electron: '42.3.0'
});

const electronWorkspacePackage = path.join(__dirname, 'applications', 'electron', 'package.json');
const workspaceRequire = createRequire(electronWorkspacePackage);

function fail(message) {
  console.error(`[FAIL] ${message}`);
  process.exitCode = 2;
}

try {
  const electronPackage = workspaceRequire('electron/package.json');
  const theiaElectronPackage = workspaceRequire('@theia/electron/package.json');
  const peer = theiaElectronPackage.peerDependencies && theiaElectronPackage.peerDependencies.electron;

  console.log(`[INFO] Installed Electron: ${electronPackage.version}`);
  console.log(`[INFO] @theia/electron: ${theiaElectronPackage.version} requires Electron ${peer || '<missing>'}`);

  let valid = true;
  if (electronPackage.version !== expected.electron) {
    fail(`Installed Electron ${electronPackage.version} does not match the NovaOryn pin ${expected.electron}.`);
    valid = false;
  }
  if (theiaElectronPackage.version !== expected.theia) {
    fail(`Installed @theia/electron ${theiaElectronPackage.version} does not match the NovaOryn pin ${expected.theia}.`);
    valid = false;
  }
  if (peer !== expected.electron) {
    fail(`@theia/electron requires Electron ${peer || '<missing>'}, expected ${expected.electron}.`);
    valid = false;
  }

  if (valid) {
    console.log('[ OK ] Installed Theia/Electron versions are synchronized.');
    process.exitCode = 0;
  }
} catch (error) {
  fail(`Unable to resolve the Electron workspace dependency graph: ${error && error.message ? error.message : String(error)}`);
}
