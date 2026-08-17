using NovaOryn.Kernel.Storage;

static void Assert(bool condition,string name){if(!condition)throw new InvalidOperationException("[FAIL] "+name);Console.WriteLine("[ OK ] "+name);}

KernelStorageOptions dynamicOptions=KernelStorageOptions.DynamicDefault;
Assert(KernelStorageMath.IsValidOptions(dynamicOptions),"Default storage registries are valid.");
Assert(dynamicOptions.RegistryMode==KernelStorageRegistryMode.Dynamic,"Storage registries grow from the kernel heap by default.");
Assert(KernelStorageMath.NextCapacity(16,UInt32.MaxValue)==32,"Dynamic storage capacity doubles when full.");
Assert(KernelStorageMath.IsValidOptions(KernelStorageOptions.Fixed(4,8,4,2,4,16,16)),"Explicit deterministic storage bounds remain available.");
Assert(KernelStorageMath.IsValidGeometry(new KernelStorageGeometry(512,4096,1_000_000,false,false)),"Normal block geometry is accepted.");
Assert(!KernelStorageMath.IsValidGeometry(new KernelStorageGeometry(1000,1000,100,false,false)),"Non-power-of-two logical sectors are rejected.");

unsafe
{
    byte* mbr=stackalloc byte[512];for(int i=0;i<512;i++)mbr[i]=0;mbr[510]=0x55;mbr[511]=0xAA;int p=446;mbr[p+4]=0x0C;mbr[p+8]=0x00;mbr[p+9]=0x08;mbr[p+12]=0x00;mbr[p+13]=0x10;
    Assert(KernelStorageMath.TryParseMbrPartition(mbr,512,0,out KernelPartitionInfo partition),"MBR partition entries are discovered.");
    Assert(partition.FirstBlock==2048&&partition.BlockCount==4096&&partition.MbrType==0x0C,"MBR LBA and type fields are decoded.");

    byte* gpt=stackalloc byte[512];for(int i=0;i<512;i++)gpt[i]=0;gpt[0]=(byte)'E';gpt[1]=(byte)'F';gpt[2]=(byte)'I';gpt[3]=(byte)' ';gpt[4]=(byte)'P';gpt[5]=(byte)'A';gpt[6]=(byte)'R';gpt[7]=(byte)'T';gpt[72]=2;gpt[80]=128;gpt[84]=128;
    Assert(KernelStorageMath.TryParseGptHeader(gpt,512,out ulong entries,out uint count,out uint size)&&entries==2&&count==128&&size==128,"GPT headers expose the partition-entry array.");

}

Console.WriteLine("[ OK ] Generic storage/VFS tests passed; filesystem tests live in selectable modules.");
