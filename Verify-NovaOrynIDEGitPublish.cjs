const fs = require('fs');
const path = require('path');

const root = __dirname;
const build = fs.readFileSync(path.join(root, 'Build-NovaOrynIDE.bat'), 'utf8');
const ignore = fs.readFileSync(path.join(root, '.gitignore'), 'utf8');
const missing = [];

const requiredBuildFragments = [
  'https://github.com/ProjectLithos/NovaOrynIDE.git',
  'set "NOVAORYN_GIT_BRANCH=main"',
  'git init -b "%NOVAORYN_GIT_BRANCH%"',
  'git remote set-url origin "%NOVAORYN_GIT_REMOTE%"',
  'git add -A',
  'git diff --cached --quiet',
  'git commit -m "NovaOryn IDE 0.8.4"',
  'git push -u origin "%NOVAORYN_GIT_BRANCH%"',
  'rmdir /s /q "%NOVAORYN_SDK_ROOT%\\.git"',
];
for (const fragment of requiredBuildFragments) {
  if (!build.includes(fragment)) missing.push(`Build-NovaOrynIDE.bat: ${fragment}`);
}

const requiredIgnores = [
  'node_modules/',
  '.toolchain/',
  'Artifacts/',
  'lib/',
  'bin/',
  'obj/',
  '.browser_modules/',
];
for (const entry of requiredIgnores) {
  if (!ignore.includes(entry)) missing.push(`.gitignore: ${entry}`);
}

if (missing.length) {
  console.error('[FAIL] NovaOryn IDE Git-publish contract is incomplete:');
  for (const item of missing) console.error(`  - ${item}`);
  process.exit(1);
}

console.log('[ OK ] NovaOryn IDE build-owned GitHub source publishing contract verified.');
