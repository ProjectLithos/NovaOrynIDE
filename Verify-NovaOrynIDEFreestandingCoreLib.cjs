const fs=require('fs');const path=require('path');const root=__dirname;
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const core=read('SDK/src/NovaOryn.Freestanding.CoreLib/CoreLib.cs');
const templateCore=read('SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Freestanding.CoreLib/CoreLib.cs');
const vsCore=read('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Freestanding.CoreLib/CoreLib.cs');
const runtime=read('SDK/src/NovaOryn.Kernel.SubsystemContracts/KernelDiagnosticsRuntime.cs');
const template=read('SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.SubsystemContracts/KernelDiagnosticsRuntime.cs');
const vs=read('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Kernel.SubsystemContracts/KernelDiagnosticsRuntime.cs');
const checks=[
 ['String.Empty',core.includes('public static String Empty => "";')],
 ['String.IsNullOrEmpty',core.includes('public static Boolean IsNullOrEmpty(String value)')],
 ['String.IsNullOrWhiteSpace',core.includes('public static Boolean IsNullOrWhiteSpace(String value)')],
 ['String ordinal equality',core.includes('public static Boolean Equals(String first, String second)')&&core.includes('operator ==')],
 ['String.CompareOrdinal',core.includes('public static Int32 CompareOrdinal(String first, String second)')],
 ['String.IndexOf',core.includes('public Int32 IndexOf(Char value)')],
 ['String.Contains',core.includes('public Boolean Contains(Char value)')],
 ['String.StartsWith',core.includes('public Boolean StartsWith(String value)')],
 ['String.EndsWith',core.includes('public Boolean EndsWith(String value)')],
 ['Char.IsWhiteSpace',core.includes('public static Boolean IsWhiteSpace(Char value)')],
 ['Object.ReferenceEquals',core.includes('public static Boolean ReferenceEquals(Object first, Object second)')],
 ['generated OS CoreLib synchronized',templateCore===core],
 ['Visual Studio CoreLib synchronized',vsCore===core],
 ['runtime avoids unsupported concat',!runtime.includes('info.Reason+": "+info.Message')],
 ['SDK template avoids unsupported concat',!template.includes('info.Reason+": "+info.Message')],
 ['VS template avoids unsupported concat',!vs.includes('info.Reason+": "+info.Message')],
];
let bad=false;for(const[n,ok] of checks){console.log(`${ok?'[ OK ]':'[FAIL]'} ${n}`);if(!ok)bad=true;}
if(bad)process.exit(1);console.log(`[ OK ] NovaOryn IDE 0.8.2 freestanding CoreLib expansion verified (${checks.length} checks).`);
