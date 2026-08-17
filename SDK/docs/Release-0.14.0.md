# NovaOryn 0.14.0

NovaOryn 0.14.0 completes roadmap item 21, networking.

The release adds `NovaOryn.Kernel.Networking`, a driver-facing and heap-backed networking layer with dynamic interface, route, neighbour and socket registries. It implements Ethernet II dispatch, ARP neighbour learning/replies, IPv4 validation and longest-prefix routing, IPv6 header foundations, ICMP echo replies, UDP datagram transmit/receive, DHCP/DNS client packet helpers and TCP connection-state foundations. Network drivers remain hardware-facing and protocol-neutral.

The generated command-line and Visual Studio kernels initialise networking after storage and expose the same SDK source. `NovaOryn.Networking.Tests` independently validates registry policy, IPv4 subnet/routing calculations and Internet checksums.

Roadmap item 22 is debugging, testing and diagnostics.
