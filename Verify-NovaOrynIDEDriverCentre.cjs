const fs=require('fs');
const files=['packages/novaoryn-ide/src/browser/novaoryn-driver-centre-widget.tsx','packages/novaoryn-ide/src/common/novaoryn-protocol.ts','packages/novaoryn-ide/src/node/novaoryn-project-service.ts'];
for(const f of files){if(!fs.existsSync(f)){console.error('[FAIL] Missing '+f);process.exit(1);}}
const w=fs.readFileSync(files[0],'utf8'),p=fs.readFileSync(files[1],'utf8'),s=fs.readFileSync(files[2],'utf8');
for(const token of ['Driver Development Centre','Create Driver Project','PCI / PCIe','VirtIO','Declared capabilities']) if(!w.includes(token)){console.error('[FAIL] Driver Centre missing '+token);process.exit(1);}
for(const token of ['createDriver(projectPath','listDrivers(projectPath','NovaOryn.Driver.json','driverAbiVersion']) if(!(p.includes(token)||s.includes(token))){console.error('[FAIL] Driver contract missing '+token);process.exit(1);}
console.log('[ OK ] NovaOryn IDE 0.9.0 Driver Development Centre contract verified.');
