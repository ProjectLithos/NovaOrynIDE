const fs=require("fs"),ts=require("typescript");
const file="packages/novaoryn-ide/src/browser/novaoryn-contribution.ts";
const source=fs.readFileSync(file,"utf8");
const out=ts.transpileModule(source,{
  compilerOptions:{
    target:ts.ScriptTarget.ES2022,
    module:ts.ModuleKind.CommonJS,
    experimentalDecorators:true
  },
  reportDiagnostics:true,
  fileName:file
});
const errors=(out.diagnostics||[]).filter(d=>d.category===ts.DiagnosticCategory.Error);
for(const d of errors)console.log("[FAIL] "+ts.flattenDiagnosticMessageText(d.messageText,"\n"));
if(errors.length)process.exitCode=1;
else console.log("[ OK ] novaoryn-contribution.ts transpiles with no TypeScript syntax errors.");
