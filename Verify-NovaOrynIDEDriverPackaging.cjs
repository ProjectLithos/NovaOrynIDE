const fs = require('fs');
const path = require('path');
const root = __dirname;
function read(rel) { return fs.readFileSync(path.join(root, rel), 'utf8'); }
function requireText(rel, needles) { const text=read(rel); for (const n of needles) if (!text.includes(n)) throw new Error(`${rel} missing ${n}`); }
requireText('SDK/NovaOryn.DriverPackage.schema.json', ['"schemaVersion"', '"id"', '"minimumNovaOrynVersion"', '"dependencies"', '"permissions"', '"signing"']);
requireText('SDK/Validate-NovaOrynDriverPackage.ps1', ['Expected 3', 'TargetArchitecture', 'SdkApiVersion', 'DriverAbiVersion', "state -eq 'revoked'"]);
requireText('SDK/Pack-NovaOrynDriver.ps1', ['NovaOryn.Driver.json', '.nodrv', 'Validate-NovaOrynDriverPackage.ps1']);
requireText('packages/novaoryn-ide/src/common/novaoryn-protocol.ts', ['id: string;', 'minimumNovaOrynVersion', 'dependencies?', 'signing?']);
requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', ['schemaVersion: 3', 'novaoryn.driver.', 'minimumNovaOrynVersion: sdkContract.sdkVersion', "signing: { state: 'unsigned' }"]);
requireText('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernelDriver/NovaOrynKernelDriver.vstemplate', ['NovaOryn.Driver.json']);
requireText('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynUserlandDriver/NovaOrynUserlandDriver.vstemplate', ['NovaOryn.Driver.json']);
console.log('[ OK ] Driver packaging schema, validation, packer and templates verified.');
