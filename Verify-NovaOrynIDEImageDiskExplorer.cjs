const fs=require('fs'),p=require('path');
const root=__dirname,read=x=>fs.readFileSync(p.join(root,x),'utf8');
const protocol=read('packages/novaoryn-ide/src/common/novaoryn-protocol.ts');
const service=read('packages/novaoryn-ide/src/node/novaoryn-project-service.ts');
const widget=read('packages/novaoryn-ide/src/browser/novaoryn-image-disk-explorer-widget.tsx');
const contrib=read('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts');
const front=read('packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts');
const checks=[
 ['protocol contracts',protocol.includes('NovaOrynDiskImageInspection')&&protocol.includes('listDiskImages(projectPath')&&protocol.includes('readDiskImageEntry(projectPath')],
 ['image discovery',service.includes("new Set(['.img', '.raw', '.iso', '.vhd', '.vhdx', '.bin'])")&&service.includes("'GPT disk image'")],
 ['GPT parser',service.includes("'EFI PART'")&&service.includes('guidFromDiskBytes')&&service.includes('protectiveMbr')],
 ['MBR parser',service.includes("scheme:'mbr'")&&service.includes('446+i*16')],
 ['FAT32 parser',service.includes("signature.startsWith('FAT32')")&&service.includes('parseFat32')&&service.includes('fatLongNamePart')],
 ['bounded raw reader',service.includes('Math.min(4096')&&service.includes("source:'disk'")],
 ['bounded FAT file reader',service.includes('readDiskImageEntry')&&service.includes("source:'file'")],
 ['native explorer widget',widget.includes('Image / Disk Explorer')&&widget.includes('Filesystem')&&widget.includes('Raw image bytes')&&widget.includes('Hex')],
 ['engineering menu',contrib.includes('NovaOrynCommands.IMAGES')&&contrib.includes("order: '13'")],
 ['widget factory',front.includes('NovaOrynImageDiskExplorerWidget')]
];
let bad=false;for(const[c,ok]of checks){console.log(`${ok?'[ OK ]':'[FAIL]'} ${c}`);if(!ok)bad=true;}if(bad)process.exit(1);console.log('[ OK ] NovaOryn IDE 0.4.0 Image / Disk Explorer contract verified.');
