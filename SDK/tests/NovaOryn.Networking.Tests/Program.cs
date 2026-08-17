using NovaOryn.Kernel.Networking;
static void Assert(bool condition,string name){if(!condition)throw new InvalidOperationException("[FAIL] "+name);Console.WriteLine("[ OK ] "+name);}
KernelNetworkOptions options=KernelNetworkOptions.DynamicDefault;
Assert(KernelNetworkMath.IsValidOptions(options),"Default networking registries are valid.");
Assert(options.RegistryMode==KernelNetworkRegistryMode.Dynamic,"Networking registries are heap-backed and dynamic by default.");
Assert(KernelNetworkMath.NextCapacity(8,UInt32.MaxValue)==16,"Dynamic network capacity doubles when full.");
Assert(KernelNetworkMath.IsValidOptions(KernelNetworkOptions.Fixed(2,4,8,16)),"Explicit deterministic network bounds remain available.");
KernelIpv4Address a=KernelIpv4Address.FromBytes(192,168,1,20),b=KernelIpv4Address.FromBytes(192,168,1,99),mask=KernelIpv4Address.FromBytes(255,255,255,0);
Assert(KernelNetworkMath.SameSubnet(a,b,mask),"IPv4 subnet matching works.");
KernelNetworkRoute route=new(KernelIpv4Address.FromBytes(10,0,0,0),KernelIpv4Address.FromBytes(255,0,0,0),default,new KernelNetworkInterfaceHandle(1),10);
Assert(KernelNetworkMath.RouteMatches(route,KernelIpv4Address.FromBytes(10,20,30,40)),"IPv4 route matching works.");
Assert(KernelNetworkMath.PrefixLength(KernelIpv4Address.FromBytes(255,255,255,0))==24,"IPv4 prefix length is calculated.");
unsafe{byte* ip=stackalloc byte[20];for(int i=0;i<20;i++)ip[i]=0;ip[0]=0x45;ip[8]=64;ip[9]=17;KernelNetworkMath.WriteUInt16Network(ip+2,2,20);KernelNetworkMath.WriteUInt32Network(ip+12,4,a.Value);KernelNetworkMath.WriteUInt32Network(ip+16,4,b.Value);ushort checksum=KernelNetworkMath.InternetChecksum(ip,20);KernelNetworkMath.WriteUInt16Network(ip+10,2,checksum);Assert(KernelNetworkMath.InternetChecksum(ip,20)==0,"Internet checksum validates IPv4 headers.");}
unsafe{byte* dhcp=stackalloc byte[300];Assert(KernelDhcpDns.BuildDhcpDiscover(dhcp,300,0x12345678,new KernelMacAddress(2,0,0,0,0,1),out uint dhcpLength)&&dhcpLength==244,"DHCP discover packets are built.");byte* dns=stackalloc byte[256];Assert(KernelDhcpDns.BuildDnsAQuery(dns,256,0x1234,"nova.local",out uint dnsLength)&&dnsLength>20,"DNS A queries are built.");}
Console.WriteLine("[ OK ] Networking tests passed.");
