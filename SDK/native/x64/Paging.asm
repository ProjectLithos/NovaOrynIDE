bits 64
default rel

section .text

global NovaOrynX64ReadPageTableRoot
global NovaOrynX64WritePageTableRoot
global NovaOrynX64InvalidatePage
global NovaOrynX64EnableExecuteDisable
global NovaOrynX64Supports1GiBPages

NovaOrynX64ReadPageTableRoot:
    mov rax, cr3
    and rax, -4096
    ret

NovaOrynX64WritePageTableRoot:
    test rcx, 0xFFF
    jnz .write_failed
    test rcx, rcx
    jz .write_failed
    mov cr3, rcx
    mov al, 1
    ret
.write_failed:
    xor eax, eax
    ret

NovaOrynX64InvalidatePage:
    invlpg [rcx]
    mov al, 1
    ret

NovaOrynX64EnableExecuteDisable:
    push rbx
    mov eax, 0x80000000
    cpuid
    cmp eax, 0x80000001
    jb .nx_unsupported
    mov eax, 0x80000001
    cpuid
    bt edx, 20
    jnc .nx_unsupported
    mov ecx, 0xC0000080
    rdmsr
    bts eax, 11
    wrmsr
    mov al, 1
    pop rbx
    ret
.nx_unsupported:
    xor eax, eax
    pop rbx
    ret

NovaOrynX64Supports1GiBPages:
    push rbx
    mov eax, 0x80000000
    cpuid
    cmp eax, 0x80000001
    jb .page1g_unsupported
    mov eax, 0x80000001
    cpuid
    bt edx, 26
    setc al
    pop rbx
    ret
.page1g_unsupported:
    xor eax, eax
    pop rbx
    ret
