.section .text.entry,"ax",@progbits
.global NovaOrynUefiEntry
.type NovaOrynUefiEntry,@function
.extern NovaOrynRuntimeInitialize
.extern KMain
NovaOrynUefiEntry:
    cli
    andq $-16, %rsp
    call NovaOrynRuntimeInitialize
    testb %al, %al
    jz NovaOrynX64Halt
    # BootContext ABI binding is completed with the runtime pack.
    call KMain
    jmp NovaOrynX64Halt
.size NovaOrynUefiEntry, .-NovaOrynUefiEntry
