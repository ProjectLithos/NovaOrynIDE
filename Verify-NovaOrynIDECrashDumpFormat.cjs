const fs=require("fs");
let failed=0;
const read=p=>fs.readFileSync(p,"utf8");
const check=(ok,msg)=>{console.log(`${ok?"[ OK ]":"[FAIL]"} ${msg}`); if(!ok) failed++;};
const proto=read("packages/novaoryn-ide/src/common/novaoryn-protocol.ts");
const svc=read("packages/novaoryn-ide/src/node/novaoryn-project-service.ts");
const sdk=read("SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs");
const docs=read("SDK/docs/Crash-Dump-Format.md");

check(sdk.includes("public const UInt32 Magic=0x4E4F4344U"),"SDK NOCD magic");
check(sdk.includes("public const UInt16 Major=1")&&sdk.includes("public const UInt16 Minor=0"),"SDK format version 1.0");
for(const [id,name] of [[1,"CpuState"],[2,"Registers"],[3,"Stack"],[4,"PageTables"],[5,"Processes"],[6,"Modules"],[7,"Heap"],[8,"MemoryRanges"],[9,"Panic"],[10,"Drivers"]])
  check(sdk.includes(`${name}=${id}`),`SDK section ${id}: ${name}`);
check(sdk.includes("IsCompatible(UInt16 major,UInt16 minor) => major==Major"),"major-version compatibility rule");
check(proto.includes("interface NovaOrynCrashDumpDocument"),"IDE formal dump document model");
check(svc.includes("magic: 'NOCD'"),"IDE writer emits NOCD");
check(svc.includes("formatVersion: { major: 1, minor: 0 }"),"IDE writer emits format v1.0");
for(const name of ["cpuState","registers","stack","pageTables","processes","modules","heap","memoryRanges","panic","drivers"])
  check(svc.includes(`${name}: {`),`IDE writer emits ${name} section`);
check(svc.includes("parsed.schemaVersion === 1 && parsed.debugState"),"legacy pre-0.9.0 reader retained");
check(svc.includes("if (major !== 1)"),"unsupported major versions rejected");
check(docs.includes("Readers must ignore **unknown section kinds** and **unknown fields**"),"forward compatibility documented");
check(docs.includes("Pre-0.9.0") || docs.includes("pre-0.9.0"),"legacy compatibility documented");
if(failed)process.exitCode=1; else console.log("[ OK ] NovaOryn Crash Dump format contract verified.");
