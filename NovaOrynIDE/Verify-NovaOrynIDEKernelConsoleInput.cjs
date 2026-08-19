const fs=require("fs");
const path=require("path");
const root=__dirname;
const files=[
  "SDK/src/NovaOryn.Kernel.CommandLine/KernelCommandLine.cs",
  "SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.CommandLine/KernelCommandLine.cs",
  "SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Kernel.CommandLine/KernelCommandLine.cs"
];
let failures=0;
function check(ok,msg){console.log(`${ok?"[ OK ]":"[FAIL]"} ${msg}`);if(!ok)failures++;}
for(const rel of files){
  const text=fs.readFileSync(path.join(root,rel),"utf8");
  check(text.includes("!KernelPs2.IsInitialized() && !KernelPs2.Initialize()"),`${rel}: shell initializes PS/2 when HAL did not`);
  check(text.includes("KernelPs2.SetKeyboardEventHandler(&HandlePs2KeyboardEvent)"),`${rel}: decoded keys reach shell`);
  check(text.includes("KernelConsole.SetInputService(&ServiceInputNow)"),`${rel}: idle loop services keyboard`);
  check(text.includes("return KernelPs2.Service();"),`${rel}: non-blocking polling fallback`);
}
const hal=fs.readFileSync(path.join(root,"SDK/templates/NovaOrynKernel/HAL/HardwareAbstractionLayer.cs"),"utf8");
check(hal.includes("#if NOVAORYN_KERNELAREA_INPUT"),"general Input subsystem remains configuration-controlled");
const kernel=fs.readFileSync(path.join(root,"SDK/templates/NovaOrynKernel/Kernel/Kernel.cs"),"utf8");
check(kernel.includes("KernelCommandLine.Initialize()"),"interactive console initializes SDK input bridge");
check(kernel.includes("KernelConsole.RunInteractive()"),"interactive console enters input-servicing idle loop");
if(failures){process.exitCode=1;} else console.log("[ OK ] Kernel console input contract verified.");
