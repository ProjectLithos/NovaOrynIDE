bits 64
default rel
section .text

global NovaOrynX64ControllerReadPort8
global NovaOrynX64ControllerWritePort8
global NovaOrynX64ReadMsr
global NovaOrynX64WriteMsr
global NovaOrynX64ReadMmio32
global NovaOrynX64WriteMmio32
global NovaOrynX64DisableLegacyPic

NovaOrynX64ControllerReadPort8:
    mov dx, cx
    xor eax, eax
    in al, dx
    ret
NovaOrynX64ControllerWritePort8:
    mov eax, edx
    mov dx, cx
    out dx, al
    mov eax, 1
    ret
NovaOrynX64ReadMsr:
    mov ecx, ecx
    rdmsr
    shl rdx, 32
    or rax, rdx
    ret
NovaOrynX64WriteMsr:
    mov r8, rdx
    mov eax, r8d
    shr r8, 32
    mov edx, r8d
    wrmsr
    mov eax, 1
    ret
NovaOrynX64ReadMmio32:
    mov eax, [rcx]
    ret
NovaOrynX64WriteMmio32:
    mov [rcx], edx
    mfence
    mov eax, 1
    ret


; Masks both 8259 PICs so APIC/MSI delivery can own external vectors.
NovaOrynX64DisableLegacyPic:
    mov al, 0xFF
    out 0x21, al
    out 0xA1, al
    mov eax, 1
    ret
