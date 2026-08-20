const fs=require("fs");
let fail=0;
const R=p=>fs.readFileSync(p,"utf8");
const C=(v,m)=>{console.log(`${v?"[ OK ]":"[FAIL]"} ${m}`);if(!v)fail++;};

const acpiSource=R("SDK/src/NovaOryn.Kernel.Acpi/KernelAcpiPlatform.cs");
const acpiTemplate=R("SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.Acpi/KernelAcpiPlatform.cs");
const transport=R("SDK/src/NovaOryn.Kernel.Bootstrap/KernelPanicTransport.cs");
const templateTransport=R("SDK/templates/NovaOrynKernel/Boot/KernelPanicTransport.cs");
const vsTransport=R("SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Boot/KernelPanicTransport.cs");
const service=R("packages/novaoryn-ide/src/node/novaoryn-project-service.ts");

C(acpiSource.includes("public static unsafe class KernelAcpiPower"),"SDK exports KernelAcpiPower");
C(acpiSource.includes("public static Boolean Reboot()"),"SDK KernelAcpiPower exports Reboot");
C(acpiTemplate.includes("public static unsafe class KernelAcpiPower"),"generated ACPI template exports same class");
C(transport.includes("KernelAcpiPower.Reboot()"),"SDK panic transport uses exported reboot class");
C(templateTransport.includes("KernelAcpiPower.Reboot()"),"normal OS template uses exported reboot class");
C(vsTransport.includes("KernelAcpiPower.Reboot()"),"Visual Studio template uses exported reboot class");
C(!transport.includes("KernelAcpiPlatform.Reboot()"),"obsolete reboot symbol removed");
C(service.includes("path.join('Boot', 'KernelPanicTransport.cs')"),"existing OS refresh receives corrected transport");

if(fail)process.exitCode=1;else console.log("[ OK ] NovaOryn 0.10.4 panic ACPI reboot contract verified.");
