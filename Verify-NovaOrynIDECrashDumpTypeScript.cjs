const fs=require("fs");
const ts=require("typescript");
const files=[
  "packages/novaoryn-ide/src/common/novaoryn-protocol.ts",
  "packages/novaoryn-ide/src/node/novaoryn-project-service.ts",
  "packages/novaoryn-ide/src/browser/novaoryn-debug-inspector-widget.tsx"
];
let failures=0;
for(const file of files){
  const source=fs.readFileSync(file,"utf8");
  const out=ts.transpileModule(source,{
    compilerOptions:{
      target:ts.ScriptTarget.ES2022,
      module:ts.ModuleKind.CommonJS,
      jsx:ts.JsxEmit.ReactJSX,
      experimentalDecorators:true
    },
    reportDiagnostics:true,
    fileName:file
  });
  const diagnostics=(out.diagnostics||[]).filter(d=>d.category===ts.DiagnosticCategory.Error);
  if(diagnostics.length){
    failures+=diagnostics.length;
    console.log(`[FAIL] ${file}`);
    for(const d of diagnostics){
      console.log("  "+ts.flattenDiagnosticMessageText(d.messageText,"\n"));
    }
  } else {
    console.log(`[ OK ] ${file}: TypeScript syntax/transpile`);
  }
}
if(failures)process.exitCode=1; else console.log("[ OK ] Modified TypeScript sources transpile cleanly.");
