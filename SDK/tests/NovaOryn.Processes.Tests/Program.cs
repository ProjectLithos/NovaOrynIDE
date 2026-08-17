using NovaOryn.Kernel.Processes;

static void Assert(bool condition,string name){if(!condition)throw new InvalidOperationException("[FAIL] "+name);Console.WriteLine("[ OK ] "+name);}
static void W16(byte[] b,int o,ushort v){b[o]=(byte)v;b[o+1]=(byte)(v>>8);}
static void W32(byte[] b,int o,uint v){W16(b,o,(ushort)v);W16(b,o+2,(ushort)(v>>16));}
static void W64(byte[] b,int o,ulong v){W32(b,o,(uint)v);W32(b,o+4,(uint)(v>>32));}

unsafe
{
    byte[] elf=new byte[0x200]; elf[0]=0x7F;elf[1]=(byte)'E';elf[2]=(byte)'L';elf[3]=(byte)'F';elf[4]=2;elf[5]=1;
    W16(elf,16,2);W16(elf,18,0x3E);W64(elf,24,0x400000);W64(elf,32,64);W16(elf,54,56);W16(elf,56,1);
    W32(elf,64,1);W32(elf,68,5);W64(elf,72,0x100);W64(elf,80,0x400000);W64(elf,96,0x20);W64(elf,104,0x1000);
    fixed(byte* p=elf){Assert(ProcessExecutableMath.TryInspect(p,(ulong)elf.Length,out var e)&&e.Format==ProcessExecutableFormat.Elf64&&e.EntryPoint==0x400000,"ELF64 x64 executable header is accepted.");Assert(ProcessExecutableMath.TryGetSegment(p,(ulong)elf.Length,e,0,out var s)&&s.VirtualAddress==0x400000&&s.FileSize==0x20&&(s.Protection&ProcessSegmentProtection.Execute)!=0,"ELF64 PT_LOAD metadata is decoded with execute protection.");}

    byte[] pe=new byte[0x400];pe[0]=(byte)'M';pe[1]=(byte)'Z';W32(pe,0x3C,0x80);W32(pe,0x80,0x00004550);W16(pe,0x84,0x8664);W16(pe,0x86,1);W16(pe,0x94,0xF0);
    int opt=0x98;W16(pe,opt,0x20B);W32(pe,opt+16,0x1000);W64(pe,opt+24,0x140000000);W32(pe,opt+56,0x2000);int sec=opt+0xF0;W32(pe,sec+8,0x100);W32(pe,sec+12,0x1000);W32(pe,sec+16,0x100);W32(pe,sec+20,0x200);W32(pe,sec+36,0x60000000);
    fixed(byte* p=pe){Assert(ProcessExecutableMath.TryInspect(p,(ulong)pe.Length,out var e)&&e.Format==ProcessExecutableFormat.PortableExecutable64&&e.EntryPoint==0x140001000,"PE32+ x64 executable header is accepted.");Assert(ProcessExecutableMath.TryGetSegment(p,(ulong)pe.Length,e,0,out var s)&&s.VirtualAddress==0x140001000&&(s.Protection&ProcessSegmentProtection.Execute)!=0,"PE32+ section metadata is decoded with execute protection.");}

    elf[18]=0xB7; fixed(byte* p=elf) Assert(!ProcessExecutableMath.TryInspect(p,(ulong)elf.Length,out _),"Non-x64 ELF images are rejected.");
    Assert(ProcessExecutableMath.PageFloor(0x12345)==0x12000&&ProcessExecutableMath.TryPageCeiling(0x12345,out var ceiling)&&ceiling==0x13000,"Executable loader page alignment is deterministic.");
}
Console.WriteLine("[ OK ] Process and executable-loading tests passed.");
