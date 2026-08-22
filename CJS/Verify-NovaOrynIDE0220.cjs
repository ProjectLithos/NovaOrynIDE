const fs=require('fs');const path=require('path');const crypto=require('crypto');const root=path.resolve(__dirname,'..');let failed=0;
function text(p){return fs.readFileSync(path.join(root,p),'utf8');}
function ok(c,m){console.log(`${c?'[ OK ]':'[FAIL]'} ${m}`);if(!c)failed++;}
function has(p,...xs){const s=text(p);for(const x of xs)ok(s.includes(x),`${p}: ${x}`);}
function exists(p){ok(fs.existsSync(path.join(root,p)),`${p} exists`);}
function same(a,b,m){ok(fs.readFileSync(path.join(root,a)).equals(fs.readFileSync(path.join(root,b))),m);}

ok(text('VERSION').split(/\r?\n/)[0].trim()==='0.22.0','release is 0.22.0');
for(const f of ['SDK/src/NovaOryn.Kernel.Networking/KernelNetworkApi.cs','SDK/src/NovaOryn.Kernel.Networking/KernelNetworkStack.cs','SDK/src/NovaOryn.Kernel.Networking/KernelDhcpDns.cs','SDK/docs/Network-Stack-API.md','SDK/docs/site-content/Network-Stack-API.md'])exists(f);

has('SDK/src/NovaOryn.Kernel.Networking/KernelNetworkApi.cs',
 'KernelNetworkAddressFamily','KernelNicInfo','KernelEthernetHeader','KernelNeighborProtocol','KernelIpv6Endpoint','KernelNetworkEndpoint','KernelNetworkStackCapabilities',
 'TryGetNic','ReceiveEthernet','ResolveArp','ConfigureIpv4','ConfigureIpv6','TryGetIpv6Configuration','ProcessNdp','SendIcmpEchoIpv4','CreateSocket','Bind','Connect','Listen','SendUdp','Receive','BuildDnsQuery','TryParseDnsAResponse','TryParseDnsAaaaResponse');
has('SDK/src/NovaOryn.Kernel.Networking/KernelNetworkStack.cs','EtherTypeArp','EtherTypeIpv4','EtherTypeIpv6','ReceiveNdp','type!=135&&type!=136','UpdateIpv6Neighbor','SendIcmpEchoIpv4','KernelNetworkProtocol.Udp','KernelNetworkProtocol.Tcp');
has('SDK/src/NovaOryn.Kernel.Networking/KernelNetworking.cs','RegisterInterface','Transmit','ConfigureIpv4','ConfigureIpv6','TryGetIpv6Configuration','UpdateNeighbor','TryResolveNeighbor','UpdateIpv6Neighbor','TryResolveIpv6Neighbor');
has('SDK/src/NovaOryn.Kernel.Networking/KernelDhcpDns.cs','KernelDnsRecordType','BuildDnsQuery','TryParseDnsAResponse','TryParseDnsAaaaResponse','type==28','dataLength==16');
has('SDK/src/NovaOryn.Kernel.Networking/KernelSockets.cs','KernelSocketType.Datagram','KernelSocketType.Stream','KernelTcpState','Create(','Bind(','Connect(','Listen(','SendTo(','Receive(','ObserveTcp');
has('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','KernelNetworkLayer','Nic=1','Ethernet=2','Arp=3','Ndp=4','Ipv4=5','Ipv6=6','Icmp=7','Udp=8','Tcp=9','Sockets=10','Dns=11','TryGetNic','TryReceiveEthernet','TryResolveArp','TryProcessNdp','TryConfigureIpv4','TryConfigureIpv6','TrySendIcmpEcho','TryCreateSocket','TrySendUdp','TryResolveNameIpv6');
has('SDK/docs/Network-Stack-API.md','NIC, Ethernet, ARP, NDP, IPv4, IPv6, ICMP, UDP, TCP, sockets and DNS','DNS API supports both A and AAAA records','NIC drivers sit underneath','Applications and services use sockets');

for(const f of ['KernelNetworkApi.cs','KernelDhcpDns.cs','KernelNetworkStack.cs','KernelNetworking.cs']){
 same(`SDK/src/NovaOryn.Kernel.Networking/${f}`,`SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.Networking/${f}`,`generated networking ${f} synchronized`);
 same(`SDK/src/NovaOryn.Kernel.Networking/${f}`,`SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Kernel.Networking/${f}`,`Visual Studio networking ${f} synchronized`);
}
same('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','SDK/templates/NovaOrynKernel/Sdk/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','generated professional network contract synchronized');
same('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','Visual Studio professional network contract synchronized');
has('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.vstemplate','Sdk\\NovaOryn.Kernel.Networking\\KernelNetworkApi.cs');

const authoritative=text('CJS/Verify-NovaOrynIDEAuthoritativeConfiguration.cjs');ok(authoritative.includes("const NOVAORYN_IDE_VERSION = '0.22.0'"),'authoritative verifier expects 0.22.0 generator version');ok(authoritative.includes('NovaOryn OS 0.22.0'),'authoritative verifier expects 0.22.0 configurator version');
const orchestrator=text('CJS/Run-NovaOrynIDEFinalVerification.cjs');ok(orchestrator.includes('CJS/Verify-NovaOrynIDE0220.cjs'),'final-verification orchestrator invokes 0.22.0 verifier');

const manifest=JSON.parse(text('SDK/NovaOryn-SourceManifest.json'));const byPath=new Map(manifest.files.map(x=>[x.path,x]));
for(const rel of ['src/NovaOryn.Kernel.Networking/KernelNetworkApi.cs','src/NovaOryn.Kernel.Networking/KernelDhcpDns.cs','src/NovaOryn.Kernel.Networking/KernelNetworkStack.cs','src/NovaOryn.Kernel.Networking/KernelNetworking.cs','src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs','docs/Network-Stack-API.md','docs/site-content/Network-Stack-API.md']){const e=byPath.get(rel);ok(!!e,`SDK source manifest lists ${rel}`);if(e){const b=fs.readFileSync(path.join(root,'SDK',rel));ok(e.length===b.length&&e.sha256===crypto.createHash('sha256').update(b).digest('hex'),`SDK source manifest matches ${rel}`);}}

for(const f of ['packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx','packages/novaoryn-ide/lib/browser/novaoryn-toolbar-widget.js']){const s=text(f);ok(!s.includes('addWidget(this.kernelConsole'),`${f} does not auto-attach Kernel Console`);ok(!s.includes('activateWidget(this.kernelConsole.id'),`${f} does not auto-activate Kernel Console`);}

if(failed)process.exit(1);console.log('[ OK ] NovaOryn IDE 0.22.0 network stack API verified.');
