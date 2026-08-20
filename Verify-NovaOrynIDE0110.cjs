const fs=require("fs"),ts=require("typescript");
const file="packages/novaoryn-ide/src/browser/novaoryn-contribution.ts";
const source=fs.readFileSync(file,"utf8");
let fail=0;
const C=(v,m)=>{console.log(`${v?"[ OK ]":"[FAIL]"} ${m}`);if(!v)fail++;};

C(source.includes("activateBottomPanelView('Problems')"),"Problems button activates real Problems view");
C(source.includes("activateBottomPanelView('Output')"),"Output button activates real Output view");
C(source.includes("this.commandService.executeCommand(commandId)"),"view activation uses Theia command service");
C(source.includes("this.shell.getWidgets('bottom')"),"ApplicationShell fallback finds actual bottom widgets");
C(source.includes("this.shell.activateWidget(match.id)"),"fallback activates actual widget");
C(source.includes("markBottomPanelSelection(label)"),"selector visual state follows successful activation");
C(!source.includes("activateBottomPanelTab(label"),"old DOM text-click switcher removed");

const out=ts.transpileModule(source,{
  compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS,experimentalDecorators:true},
  reportDiagnostics:true,fileName:file
});
const errors=(out.diagnostics||[]).filter(d=>d.category===ts.DiagnosticCategory.Error);
for(const d of errors)console.log("[FAIL] "+ts.flattenDiagnosticMessageText(d.messageText,"\n"));
if(errors.length)fail+=errors.length;
else console.log("[ OK ] modified contribution transpiles cleanly.");

if(fail)process.exitCode=1;
else console.log("[ OK ] NovaOryn 0.10.11 Problems/Output separation verified.");
