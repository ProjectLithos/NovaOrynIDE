// NovaOryn IDE release contract: 0.22.1
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const build = fs.readFileSync(path.join(root, 'Build-NovaOrynIDE.bat'), 'utf8');
const publish = fs.readFileSync(path.join(root, 'Scripts', 'Publish-NovaOrynIDESource.ps1'), 'utf8');
const ignore = fs.readFileSync(path.join(root, '.gitignore'), 'utf8');
const missing = [];

for (const fragment of [
  'Scripts\\Publish-NovaOrynIDESource.ps1',
  'Publishing NovaOryn IDE source to GitHub'
]) if (!build.includes(fragment)) missing.push(`Build-NovaOrynIDE.bat: ${fragment}`);

for (const fragment of [
  'https://github.com/ProjectLithos/NovaOrynIDE.git',
  "$branch = 'main'",
  "@('init','-b',$branch,'.')",
  "@('remote','set-url','origin',$remote)",
  "@('add','-A')",
  'diff --cached --quiet',
  "core.hooksPath=NUL",
  "@('push','-u','origin',$branch)",
  "Join-Path $sdkRoot '.git'"
]) if (!publish.includes(fragment)) missing.push(`Scripts/Publish-NovaOrynIDESource.ps1: ${fragment}`);

for (const entry of ['node_modules/','.toolchain/','Artifacts/','lib/','bin/','obj/','.browser_modules/']) {
  if (!ignore.includes(entry)) missing.push(`.gitignore: ${entry}`);
}
if (missing.length) {
  console.error('[FAIL] NovaOryn IDE Git-publish contract is incomplete:');
  for (const item of missing) console.error(`  - ${item}`);
  process.exit(1);
}
console.log('[ OK ] NovaOryn IDE build-owned GitHub source publishing contract verified.');
