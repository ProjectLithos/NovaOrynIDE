const fs=require("fs");
let fail=0;
const R=p=>fs.readFileSync(p,"utf8");
const C=(v,m)=>{console.log(`${v?"[ OK ]":"[FAIL]"} ${m}`); if(!v)fail++;};

const project=R("SDK/templates/NovaOrynKernel/NovaOrynKernel.csproj");
const vsProject=R("SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.csproj");
const native=R("SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.X64.LowLevel/Native.cs");
const transport=R("SDK/templates/NovaOrynKernel/Boot/KernelPanicTransport.cs");
const service=R("packages/novaoryn-ide/src/node/novaoryn-project-service.ts");

C(project.includes("<DisableTransitiveProjectReferences>true</DisableTransitiveProjectReferences>"),"generated kernel disables transitive project references");
C(project.includes('Sdk\\NovaOryn.Kernel.X64.LowLevel\\NovaOryn.Kernel.X64.LowLevel.csproj'),"normal generated kernel directly references x64 low-level ABI");
C(vsProject.includes('Sdk\\NovaOryn.Kernel.X64.LowLevel\\NovaOryn.Kernel.X64.LowLevel.csproj'),"Visual Studio generated kernel directly references x64 low-level ABI");
C(native.includes("namespace NovaOryn.Kernel.Internal.X64;"),"Native type namespace is NovaOryn.Kernel.Internal.X64");
C(transport.includes("using NovaOryn.Kernel.Internal.X64;"),"panic transport imports the real Native namespace");
C(native.includes("CapturePanicContext"),"low-level project exports panic context capture");
C(native.includes("PanicDebuggerBreak"),"low-level project exports panic debugger break");
C(service.includes("Sdk\\\\NovaOryn.Kernel.X64.LowLevel\\\\NovaOryn.Kernel.X64.LowLevel.csproj"),"existing OS project reference is automatically repaired");
C(service.includes("projectChanged = true"),"project repair persists all required edits together");

if(fail)process.exitCode=1; else console.log("[ OK ] NovaOryn 0.10.9 panic low-level reference contract verified.");
