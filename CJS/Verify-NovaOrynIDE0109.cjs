const fs=require("fs"),ts=require("typescript");
const file="packages/novaoryn-ide/src/browser/novaoryn-contribution.ts";
const source=fs.readFileSync(file,"utf8");
let fail=0;
const C=(v,m)=>{console.log(`${v?"[ OK ]":"[FAIL]"} ${m}`);if(!v)fail++;};

C(source.includes("const bottom = this.shell.bottomPanel?.node;"),
  "control strip still mounts on ApplicationShell.bottomPanel.node");
C(source.includes("this.shell.bottomPanel.hide();"),
  "Close control uses public Lumino Widget.hide()");
C(!source.includes("this.shell.collapseBottomPanel();"),
  "protected ApplicationShell.collapseBottomPanel call removed");
C(source.includes("this.shell.bottomPanel.toggleMaximized();"),
  "maximize/restore control retained");

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
if(errors.length)fail+=errors.length;
else console.log("[ OK ] modified contribution transpiles cleanly.");

if(fail)process.exitCode=1;
else console.log("[ OK ] NovaOryn 0.10.11 bottom-panel contract verified.");
