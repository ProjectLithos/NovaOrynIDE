# NovaOryn 0.10.11

NovaOryn 0.10.11 implements roadmap item 16, user/kernel separation. It establishes the x64 ring-0/ring-3 address and selector policy, enables supervisor write protection and SMEP when supported, reports SMAP capability for the upcoming guarded-copy layer, enforces user page-table mappings, and validates future user-mode transition contexts. System-call entry and executable/process loading remain roadmap items 17 and 18.
