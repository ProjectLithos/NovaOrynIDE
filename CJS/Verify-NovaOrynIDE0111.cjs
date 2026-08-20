const fs=require("fs"),ts=require("typescript");
const file="packages/novaoryn-ide/src/browser/novaoryn-contribution.ts";
const source=fs.readFileSync(file,"utf8");
let fail=0;
const C=(v,m)=>{console.log(`${v?"[ OK ]":"[FAIL]"} ${m}`);if(!v)fail++;};

C(!source.includes("commandService.getCommand("),"unsupported CommandService.getCommand removed");
C(source.includes("await this.commandService.executeCommand(commandId);"),"Problems/Output use executeCommand directly");
C(source.includes("this.shell.getWidgets('bottom')"),"ApplicationShell bottom-widget fallback retained");
C(source.includes("await this.shell.activateWidget(match.id);"),"fallback activates actual widget");
C(source.includes("markBottomPanelSelection(label)"),"visual selector follows real activation");

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
else console.log("[ OK ] NovaOryn 0.10.11 Problems/Output activation contract verified.");
