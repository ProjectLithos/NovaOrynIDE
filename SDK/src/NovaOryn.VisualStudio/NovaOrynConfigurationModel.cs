using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
namespace NovaOryn.VisualStudio;
internal sealed class NovaOrynConfigurationModel
{
 public int Version{get;set;}=3; public bool Completed{get;set;} public string Architecture{get;set;}="x64"; public string KernelModel{get;set;}="Monolithic"; public string BootProtocol{get;set;}="Uefi";
 // WorkAreas intentionally means the areas the end user wants to implement. Those areas are missing from the supplied OS graph.
 public List<string> WorkAreas{get;set;}=new List<string>();
 public static readonly string[] Architectures={"x64","arm64","riscv64"}; public static readonly string[] KernelModels={"Monolithic","Microkernel","Hybrid"};
 public static readonly string[] AvailableWorkAreas={"Shell","GUI","Drivers","HAL","Audio","Filesystems","Storage","Networking","USB","Input","Processes","Scheduler","System Calls","Security","Diagnostics","Tests"};
 public static NovaOrynConfigurationModel Load(string path){if(!File.Exists(path))return CreateDefault();try{var m=new JavaScriptSerializer().Deserialize<NovaOrynConfigurationModel>(File.ReadAllText(path));if(m==null)return CreateDefault();m.Version=3;m.WorkAreas??=new List<string>();m.WorkAreas=m.WorkAreas.Where(x=>AvailableWorkAreas.Contains(x,StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();return m;}catch{return CreateDefault();}}
 public static NovaOrynConfigurationModel CreateDefault()=>new NovaOrynConfigurationModel{Version=3,Completed=false,Architecture="x64",KernelModel="Monolithic",BootProtocol="Uefi",WorkAreas=new List<string>()};
 public List<string> GetSuppliedAreas()=>AvailableWorkAreas.Where(x=>!WorkAreas.Contains(x,StringComparer.OrdinalIgnoreCase)).ToList();
 public void Save(string path){Directory.CreateDirectory(Path.GetDirectoryName(path)??".");File.WriteAllText(path,Pretty(new JavaScriptSerializer().Serialize(this))+Environment.NewLine);}
 public string GetArchitectureStatus(){if(string.Equals(Architecture,"x64",StringComparison.OrdinalIgnoreCase))return "Supported now: x64 UEFI/NativeAOT architecture pack.";if(string.Equals(Architecture,"arm64",StringComparison.OrdinalIgnoreCase))return "Target saved, but the ARM64 architecture pack is not installed yet. Build stops instead of generating the wrong architecture.";return "Target saved, but the RISC-V 64 architecture pack is not installed yet. Build stops instead of generating the wrong architecture.";}
 public string GetKernelModelDescription(){if(KernelModel=="Microkernel")return "Microkernel: only kernel mechanisms belong under Kernel; supplied drivers, filesystems, networking, audio and similar services are placed in Userland unless they are essential mechanisms.";if(KernelModel=="Hybrid")return "Hybrid: essential mechanisms and selected performance-critical components remain under Kernel; supplied services and optional components live in Userland.";return "Monolithic: supplied OS components may live under Kernel, while ordinary applications such as Shell/GUI remain userland programs.";}
 private static string Pretty(string json){int i=0;bool q=false,e=false;var b=new System.Text.StringBuilder();foreach(char c in json){if(q){b.Append(c);if(e)e=false;else if(c=='\\')e=true;else if(c=='\"')q=false;continue;}if(c=='\"'){q=true;b.Append(c);continue;}if(c=='{'||c=='['){b.Append(c).AppendLine();i++;b.Append(new string(' ',i*2));}else if(c=='}'||c==']'){b.AppendLine();i--;b.Append(new string(' ',i*2)).Append(c);}else if(c==',')b.Append(c).AppendLine().Append(new string(' ',i*2));else if(c==':')b.Append(": ");else if(!char.IsWhiteSpace(c))b.Append(c);}return b.ToString();}
}
