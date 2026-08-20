const fs=require("fs"),ts=require("typescript");
const file="packages/novaoryn-ide/src/browser/novaoryn-contribution.ts";
const out=ts.transpileModule(fs.readFileSync(file,"utf8"),{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,experimentalDecorators:true},reportDiagnostics:true,fileName:file});
const e=(out.diagnostics||[]).filter(d=>d.category===ts.DiagnosticCategory.Error);
for(const d of e)console.log("[FAIL] "+ts.flattenDiagnosticMessageText(d.messageText,"\n"));
if(e.length)process.exitCode=1;else console.log("[ OK ] NovaOrynContribution TypeScript transpiles.");
