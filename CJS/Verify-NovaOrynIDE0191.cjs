const fs=require('fs');const path=require('path');const root=path.resolve(__dirname,'..');let failed=0;
function text(p){return fs.readFileSync(path.join(root,p),'utf8');}function ok(c,m){console.log(`${c?'[ OK ]':'[FAIL]'} ${m}`);if(!c)failed++;}function has(p,...xs){const s=text(p);for(const x of xs)ok(s.includes(x),`${p}: ${x}`)}
ok(text('VERSION').split(/\r?\n/)[0].trim()==='0.19.1','release is 0.19.1');
const c='SDK/src/NovaOryn.ApplicationFormat/NovaOrynApplicationContracts.cs',p='SDK/src/NovaOryn.ApplicationFormat/NovaOrynApplicationPackage.cs';
has(c,'Application=".exe"','NativeExecutable=".nexe"','DynamicLibrary=".dll"','StaticOrImportLibrary=".lib"','Magic=0x50414F4EU','HeaderBytes=192U','NovaOrynApplicationArchitecture','NovaOrynApplicationAbi','NovaOrynApplicationDependency','NovaOrynApplicationCapability','NovaOrynApplicationResource');
has(p,'TryInspect(','TryGetNativeImage(','TryGetDependency(','TryGetCapability(','TryGetResource(','TryGetStringBytes(','packageBytes!=n','Range(');
has('SDK/src/NovaOryn.ApplicationPacker/Program.cs','NovaOryn.ApplicationPacker','NovaOryn.Application.json','RequiredCapabilities','Resources','nativeImage','0x50414F4E');
has('SDK/src/NovaOryn.Kernel.Processes/NovaOrynApplicationLoader.cs','TryResolveNativeImage','SupportedAbiMajor=1','NovaOrynApplicationArchitecture.X64','packageInfo.AbiMinor>SupportedAbiMinor');
has('SDK/src/NovaOryn.Kernel.Processes/KernelProcesses.cs','NovaOrynApplicationLoader.TryResolveNativeImage','application.EntryPointRva','nativeLength','KernelSecurity.TryValidateExecutableRange');
has('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','IKernelApplicationFormatContract','ApplicationExtension=".exe"','NativeExecutableExtension=".nexe"','DynamicLibraryExtension=".dll"','StaticLibraryExtension=".lib"','TryEnumerateDependency','TryEnumerateRequiredCapability','TryEnumerateResource','TryResolveNativeImage');
has('SDK/NovaOryn.sln','NovaOryn.ApplicationFormat','NovaOryn.ApplicationPacker');
for(const b of ['SDK/templates/NovaOrynKernel/Sdk','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk']){for(const f of ['NovaOryn.ApplicationFormat/NovaOryn.ApplicationFormat.csproj','NovaOryn.ApplicationFormat/NovaOrynApplicationContracts.cs','NovaOryn.ApplicationFormat/NovaOrynApplicationPackage.cs','NovaOryn.Kernel.Processes/NovaOrynApplicationLoader.cs'])ok(fs.existsSync(path.join(root,b,f)),`${b}/${f} synchronized`);}
has('SDK/templates/NovaOrynKernel/NovaOrynKernel.csproj','NovaOryn.ApplicationFormat');has('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.csproj','NovaOryn.ApplicationFormat');has('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.vstemplate','NovaOryn.ApplicationFormat','NovaOrynApplicationLoader.cs');
ok(fs.existsSync(path.join(root,'SDK/docs/Executable-Application-Format.md')),'application-format documentation exists');
has('SDK/docs/Executable-Application-Format.md','**`.exe`**','**`.nexe`**','**`.dll`**','**`.lib`**','Declaration is not authority','entry-point RVA','override file associations');
// carried-forward duplicate-console regression
for(const f of ['packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx','packages/novaoryn-ide/lib/browser/novaoryn-toolbar-widget.js']){const s=text(f);ok(!s.includes('addWidget(this.kernelConsole'),`${f} does not auto-attach Kernel Console`);ok(!s.includes('activateWidget(this.kernelConsole.id'),`${f} does not auto-activate Kernel Console`);}

const authoritative=text('CJS/Verify-NovaOrynIDEAuthoritativeConfiguration.cjs');
ok(authoritative.includes("const NOVAORYN_IDE_VERSION = '0.19.1'"),'authoritative verifier expects 0.19.1 generator version');
ok(authoritative.includes('NovaOryn OS 0.19.1'),'authoritative verifier expects 0.19.1 configurator version');
const changed=text('Ancillary/ChangedFiles.txt');
ok(changed.includes('packages/novaoryn-ide/src/node/novaoryn-project-service.ts'),'ChangedFiles forces authoritative generator source');
ok(changed.includes('packages/novaoryn-ide/src/browser/novaoryn-widget.tsx'),'ChangedFiles forces authoritative configurator source');
ok(changed.includes('packages/novaoryn-ide/lib/node/novaoryn-project-service.js'),'ChangedFiles forces generated generator runtime');
ok(changed.includes('packages/novaoryn-ide/lib/browser/novaoryn-widget.js'),'ChangedFiles forces generated configurator runtime');
if(failed)process.exit(1);console.log('[ OK ] NovaOryn IDE 0.19.1 executable/application format verified.');
