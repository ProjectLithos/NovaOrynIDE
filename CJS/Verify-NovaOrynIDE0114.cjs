const fs=require('fs');
const build=fs.readFileSync('Build-NovaOrynIDE.bat','utf8');
const run=fs.readFileSync('Run-NovaOrynIDE.bat','utf8');
let fail=false;
for (const [name,text,need] of [
 ['build',build,'applications\\electron\\lib\\.novaoryn-build-version'],
 ['build',build,'echo 0.11.14'],
 ['run',run,'applications\\electron\\lib\\.novaoryn-build-version'],
 ['run',run,'NOVAORYN_BUILT_VERSION'],
 ['run',run,'=="0.11.14"']
]) { if(!text.includes(need)){console.error(`[FAIL] ${name}: missing ${need}`);fail=true;} }
if(run.includes('ConvertFrom-Json') && run.includes('BuildState')){console.error('[FAIL] Run still depends on JSON build-state marker.');fail=true;}
if(fail) process.exitCode=1; else console.log('[ OK ] NovaOryn IDE 0.11.14 generated-build marker contract verified.');
