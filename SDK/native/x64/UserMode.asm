bits 64
default rel

section .text

global NovaOrynX64EnterUserMode

; Win64 ABI: RCX=user RIP, RDX=user RSP, R8=opaque first argument.
; On success IRETQ enters ring 3 and this function intentionally never returns.
NovaOrynX64EnterUserMode:
    test rcx, rcx
    jz .failed
    test rdx, rdx
    jz .failed
    test rdx, 0xF
    jnz .failed

    mov rdi, r8
    push qword 0x1B
    push rdx
    pushfq
    pop rax
    or rax, 0x200
    and rax, ~0x3000
    push rax
    push qword 0x23
    push rcx
    iretq
.failed:
    xor eax, eax
    ret
