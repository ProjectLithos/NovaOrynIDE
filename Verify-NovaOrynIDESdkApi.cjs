const fs = require('fs');
const path = require('path');
function read(p){ return fs.readFileSync(path.join(__dirname,p),'utf8'); }
function requireText(text, value, file){ if(!text.includes(value)) throw new Error(`${file}: missing ${value}`); }
const contribution='packages/novaoryn-ide/src/browser/novaoryn-contribution.ts';
const frontend='packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts';
const protocol='packages/novaoryn-ide/src/common/novaoryn-protocol.ts';
const service='packages/novaoryn-ide/src/node/novaoryn-project-service.ts';
const widget='packages/novaoryn-ide/src/browser/novaoryn-sdk-api-widget.tsx';
requireText(read(contribution), 'NovaOrynCommands.SDK_API', contribution);
requireText(read(contribution), 'CommonMenus.HELP', contribution);
requireText(read(frontend), 'NovaOrynSdkApiWidget', frontend);
requireText(read(protocol), 'getSdkApiSiteUrl(): Promise<string>', protocol);
requireText(read(service), "path.join(NOVAORYN_SDK_ROOT, 'docs', 'site', 'index.html')", service);
requireText(read(widget), "className='novaoryn-sdk-api-frame'", widget);
if(!fs.existsSync(path.join(__dirname,'SDK','docs','site','index.html'))) throw new Error('Bundled SDK API site index.html is missing.');
console.log('[ OK ] Help -> SDK API is wired to the bundled SDK documentation site.');
