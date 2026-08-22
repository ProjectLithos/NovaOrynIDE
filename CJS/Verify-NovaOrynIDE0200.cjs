const fs=require('fs');const path=require('path');const root=path.resolve(__dirname,'..');let failed=0;
function text(p){return fs.readFileSync(path.join(root,p),'utf8');}
function ok(c,m){console.log(`${c?'[ OK ]':'[FAIL]'} ${m}`);if(!c)failed++;}
function has(p,...xs){const s=text(p);for(const x of xs)ok(s.includes(x),`${p}: ${x}`);}
function exists(p){ok(fs.existsSync(path.join(root,p)),`${p} exists`);}

ok(text('VERSION').split(/\r?\n/)[0].trim()==='0.20.0','release is 0.20.0');
for(const f of ['SDK/NovaOryn.Package.schema.json','SDK/Pack-NovaOrynPackage.bat','SDK/NovaOryn-Package.bat','SDK/src/NovaOryn.PackageFormat/NovaOryn.PackageFormat.csproj','SDK/src/NovaOryn.PackageFormat/NovaOrynPackageContracts.cs','SDK/src/NovaOryn.PackageFormat/NovaOrynPackageArchive.cs','SDK/src/NovaOryn.PackagePacker/NovaOryn.PackagePacker.csproj','SDK/src/NovaOryn.PackagePacker/Program.cs','SDK/src/NovaOryn.PackageManager/NovaOryn.PackageManager.csproj','SDK/src/NovaOryn.PackageManager/Program.cs','SDK/templates/NovaOrynPackage/NovaOryn.Package.json','SDK/docs/Package-Manager-Format.md','SDK/docs/site-content/Package-Manager-Format.md'])exists(f);

has('SDK/src/NovaOryn.PackageFormat/NovaOrynPackageContracts.cs',
  'ContainerExtension = ".zip"','ManifestName = "NovaOryn.Package.json"','Format = "novaoryn-package-v1"','SchemaVersion = 1',
  'Application','Driver','Library','Service','KernelExtension','NovaOrynPackageDependency','NovaOrynPackageFile','NovaOrynPackageSignature');
has('SDK/NovaOryn.Package.schema.json','"novaoryn-package-v1"','"Application"','"Driver"','"Library"','"Service"','"KernelExtension"','"sha256"','"signing"');
has('SDK/src/NovaOryn.PackageFormat/NovaOrynPackageArchive.cs',
  'ZipFile.OpenRead','IsSafeRelativePath','Unsafe ZIP entry path','Duplicate ZIP entry path','Undeclared package payload','SHA-256 mismatch',
  'Application package must contain a payload .exe','Driver package must contain a payload .nodrv','Library package must contain a payload .dll or .lib',
  'Service package must contain a payload .exe','Kernel extensions must be signed or trusted','NovaOrynVersionConstraint');
has('SDK/Pack-NovaOrynPackage.bat','NovaOryn.PackagePacker','--configuration Release');
has('SDK/NovaOryn-Package.bat','NovaOryn.PackageManager','--configuration Release');
has('SDK/src/NovaOryn.PackagePacker/Program.cs','<output.zip>','NovaOryn.Package.json','NovaOrynPackageArchive.ValidateManifest','SHA256.HashData','ZipFile.Open','NovaOrynPackageArchive.Verify');
has('SDK/src/NovaOryn.PackageManager/Program.cs',
  '"verify"','"inspect"','"install"','"uninstall"','"list"','Unresolved dependency','Kernel extensions require signed/trusted package policy',
  'transactions','Directory.Move(stage, final)','previousBackup','previousTreeMoved','SaveDatabaseAtomic','database.json','required by:');
has('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs',
  'NovaOrynPackageContainer','Extension=".zip"','ManifestName="NovaOryn.Package.json"','NovaOrynPackageKind','NovaOrynPackageDependency','NovaOrynPackageFile','NovaOrynPackageSignature','INovaOrynPackageManagerContract');
has('SDK/NovaOryn.sln','NovaOryn.PackageFormat','NovaOryn.PackagePacker','NovaOryn.PackageManager');
has('SDK/docs/Package-Manager-Format.md','ordinary **ZIP archive**','Applications continue','transaction staging directory','Capabilities are declarations, not grants','KernelExtension');

// Existing 0.19.x executable naming remains intact.
has('SDK/src/NovaOryn.ApplicationFormat/NovaOrynApplicationContracts.cs','Application=".exe"','NativeExecutable=".nexe"','DynamicLibrary=".dll"','StaticOrImportLibrary=".lib"');
// Existing driver format remains a payload artifact rather than being silently redefined.
has('SDK/Pack-NovaOrynDriver.ps1','.nodrv');

const authoritative=text('CJS/Verify-NovaOrynIDEAuthoritativeConfiguration.cjs');
ok(authoritative.includes("const NOVAORYN_IDE_VERSION = '0.20.0'"),'authoritative verifier expects 0.20.0 generator version');
ok(authoritative.includes('NovaOryn OS 0.20.0'),'authoritative verifier expects 0.20.0 configurator version');
const orchestrator=text('CJS/Run-NovaOrynIDEFinalVerification.cjs');
ok(orchestrator.includes('CJS/Verify-NovaOrynIDE0200.cjs'),'final-verification orchestrator invokes 0.20.0 verifier');

// Preserve the duplicate Kernel Console fix.
for(const f of ['packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx','packages/novaoryn-ide/lib/browser/novaoryn-toolbar-widget.js']){const s=text(f);ok(!s.includes('addWidget(this.kernelConsole'),`${f} does not auto-attach Kernel Console`);ok(!s.includes('activateWidget(this.kernelConsole.id'),`${f} does not auto-activate Kernel Console`);}

if(failed)process.exit(1);console.log('[ OK ] NovaOryn IDE 0.20.0 ZIP package-manager format verified.');
