# NovaOryn IDE 0.22.0

0.22.0 formalises the NovaOryn network stack API across NIC, Ethernet, ARP/NDP, IPv4/IPv6, ICMP, UDP, TCP, sockets and DNS.

The release adds `KernelNetworkApi`, IPv4/IPv6 endpoint-neutral contracts, NIC and Ethernet metadata, IPv6 NDP neighbor learning, ICMPv4 echo transmit support, DNS A/AAAA query/response APIs, and an expanded professional networking subsystem contract. Networking remains driver-independent: E1000, RTL8168, VirtIO-net and future NICs provide frame transport below the standard stack.
