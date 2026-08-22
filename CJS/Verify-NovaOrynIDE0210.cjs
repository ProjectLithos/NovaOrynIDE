const fs=require('fs');const path=require('path');const crypto=require('crypto');const root=path.resolve(__dirname,'..');let failed=0;
function text(p){return fs.readFileSync(path.join(root,p),'utf8');}
function ok(c,m){console.log(`${c?'[ OK ]':'[FAIL]'} ${m}`);if(!c)failed++;}
function has(p,...xs){const s=text(p);for(const x of xs)ok(s.includes(x),`${p}: ${x}`);}
function exists(p){ok(fs.existsSync(path.join(root,p)),`${p} exists`);}
function same(a,b,m){ok(fs.readFileSync(path.join(root,a)).equals(fs.readFileSync(path.join(root,b))),m);}

ok(text('VERSION').split(/\r?\n/)[0].trim()==='0.21.0','release is 0.21.0');
for(const f of ['SDK/src/NovaOryn.Kernel.Storage/KernelStorageContracts.cs','SDK/src/NovaOryn.Kernel.Storage/KernelVfs.cs','SDK/src/NovaOryn.Filesystem.FatFs/FatFs.cs','SDK/docs/Filesystem-VFS-Contract.md','SDK/docs/site-content/Filesystem-VFS-Contract.md'])exists(f);

has('SDK/src/NovaOryn.Kernel.Storage/KernelStorageContracts.cs',
  'KernelFilePermissions','OwnerRead','OwnerWrite','OwnerExecute','GroupRead','OtherRead','ReadOnly','System','Hidden',
  'KernelFileSystemFeatures','Directories','Permissions','AsyncIoReserved','KernelVfsIoModel','AsynchronousReserved',
  'KernelDirectoryHandle','KernelVfsProviderInfo','KernelVfsMountInfo','ReadDirectory','GetPermissions','SetPermissions');
has('SDK/src/NovaOryn.Kernel.Storage/KernelVfs.cs',
  'RegisterFileSystem','TryGetProviderInfo','public static Boolean Mount(','public static Boolean Unmount(','FindExactMount','MountPathEquals','AllocateMountPath',
  'public static Boolean Open(','public static Boolean Read(','public static Boolean Write(','public static Boolean Seek(','public static Boolean Flush(','public static Boolean Close(',
  'public static Boolean OpenDirectory(','public static Boolean ReadDirectory(','public static Boolean RewindDirectory(','public static Boolean CloseDirectory(',
  'public static Boolean TryGetPermissions(','public static Boolean TrySetPermissions(','PermissionsAllow','TryProviderGetPermissions',
  'public static KernelVfsIoModel IoModel=>KernelVfsIoModel.Synchronous','public static Boolean SupportsAsyncIo=>false');
has('SDK/src/NovaOryn.Filesystem.FatFs/FatFs.cs',
  'KernelFileSystemFeatures.Directories','KernelFileSystemFeatures.Permissions','&ReadDirectory','&GetPermissions','&SetPermissions',
  'TryReadDirectoryEntry','TryDirectoryEntryInSector','KernelFilePermissions.ReadOnly','KernelFilePermissions.Hidden','KernelFilePermissions.System',
  'chmod-style changes are unsupported');
has('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs',
  'IKernelVfsContract','TryUnmount','TryWrite','TryOpenDirectory','TryReadDirectory','TryCloseDirectory','TryGetPermissions','TrySetPermissions','TryRegisterFileSystemDriver','SupportsAsyncIo');
has('SDK/docs/Filesystem-VFS-Contract.md','mount namespaces','longest-prefix','Directory names are copied into a caller-owned character buffer','Filesystem drivers','intentionally synchronous','SupportsAsyncIo');

same('SDK/src/NovaOryn.Kernel.Storage/KernelStorageContracts.cs','SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.Storage/KernelStorageContracts.cs','generated kernel storage contracts synchronized');
same('SDK/src/NovaOryn.Kernel.Storage/KernelVfs.cs','SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.Storage/KernelVfs.cs','generated kernel VFS synchronized');
same('SDK/src/NovaOryn.Kernel.Storage/KernelStorageContracts.cs','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Kernel.Storage/KernelStorageContracts.cs','Visual Studio storage contracts synchronized');
same('SDK/src/NovaOryn.Kernel.Storage/KernelVfs.cs','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Kernel.Storage/KernelVfs.cs','Visual Studio VFS synchronized');
same('SDK/src/NovaOryn.Filesystem.FatFs/FatFs.cs','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynFilesystemFatFs/FatFs.cs','Visual Studio FatFs provider synchronized');
same('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','generated professional VFS contract synchronized');
same('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','Visual Studio professional VFS contract synchronized');

// Async is ABI-reserved, not falsely implemented.
const vfs=text('SDK/src/NovaOryn.Kernel.Storage/KernelVfs.cs');
ok(!vfs.includes('SubmitAsync')&&!vfs.includes('CompleteAsync'),'0.21.0 does not fake async I/O implementation');

// Existing application/package formats remain unchanged in principle.
has('SDK/src/NovaOryn.ApplicationFormat/NovaOrynApplicationContracts.cs','Application=".exe"','NativeExecutable=".nexe"','DynamicLibrary=".dll"','StaticOrImportLibrary=".lib"');
has('SDK/src/NovaOryn.PackageFormat/NovaOrynPackageContracts.cs','ContainerExtension = ".zip"','ManifestName = "NovaOryn.Package.json"');

const authoritative=text('CJS/Verify-NovaOrynIDEAuthoritativeConfiguration.cjs');
ok(authoritative.includes("const NOVAORYN_IDE_VERSION = '0.21.0'"),'authoritative verifier expects 0.21.0 generator version');
ok(authoritative.includes('NovaOryn OS 0.21.0'),'authoritative verifier expects 0.21.0 configurator version');
const orchestrator=text('CJS/Run-NovaOrynIDEFinalVerification.cjs');ok(orchestrator.includes('CJS/Verify-NovaOrynIDE0210.cjs'),'final-verification orchestrator invokes 0.21.0 verifier');

// Source manifest hashes must cover the changed SDK VFS files and new docs.
const manifest=JSON.parse(text('SDK/NovaOryn-SourceManifest.json'));const byPath=new Map(manifest.files.map(x=>[x.path,x]));
for(const rel of ['src/NovaOryn.Kernel.Storage/KernelStorageContracts.cs','src/NovaOryn.Kernel.Storage/KernelVfs.cs','src/NovaOryn.Filesystem.FatFs/FatFs.cs','docs/Filesystem-VFS-Contract.md','docs/site-content/Filesystem-VFS-Contract.md']){
  const e=byPath.get(rel);ok(!!e,`SDK source manifest lists ${rel}`);if(e){const b=fs.readFileSync(path.join(root,'SDK',rel));ok(e.length===b.length&&e.sha256===crypto.createHash('sha256').update(b).digest('hex'),`SDK source manifest matches ${rel}`);}
}

// Preserve duplicate Kernel Console fix.
for(const f of ['packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx','packages/novaoryn-ide/lib/browser/novaoryn-toolbar-widget.js']){const s=text(f);ok(!s.includes('addWidget(this.kernelConsole'),`${f} does not auto-attach Kernel Console`);ok(!s.includes('activateWidget(this.kernelConsole.id'),`${f} does not auto-activate Kernel Console`);}

if(failed)process.exit(1);console.log('[ OK ] NovaOryn IDE 0.21.0 filesystem VFS contract verified.');
