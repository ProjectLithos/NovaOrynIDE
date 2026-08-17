bits 64
default rel
section .text

global NovaOrynX64DisableInterrupts
global NovaOrynX64EnableInterrupts
global NovaOrynX64AreInterruptsEnabled
global NovaOrynX64Halt
global NovaOrynX64Pause
global NovaOrynX64WaitForInterrupt
global NovaOrynX64WritePort8
global NovaOrynX64ReadPort8
global NovaOrynX64WritePort16
global NovaOrynX64ReadPort16
global NovaOrynX64WritePort32
global NovaOrynX64ReadPort32
global NovaOrynX64ReadTimestampCounter
global NovaOrynX64SupportsTsc
global NovaOrynX64SupportsInvariantTsc
global NovaOrynX64ReadMmio64
global NovaOrynX64WriteMmio64
global NovaOrynX64InitializeThreadContext
global NovaOrynX64SwitchThreadContext
global NovaOrynX64EnableKernelWriteProtect
global NovaOrynX64IsExecuteDisableEnabled
global NovaOrynX64IsKernelWriteProtectEnabled
global NovaOrynX64SupportsSmep
global NovaOrynX64EnableSmep
global NovaOrynX64SupportsSmap

NovaOrynX64DisableInterrupts:
    cli
    mov al, 1
    ret
NovaOrynX64EnableInterrupts:
    sti
    mov al, 1
    ret
NovaOrynX64AreInterruptsEnabled:
    pushfq
    pop rax
    shr rax, 9
    and rax, 1
    ret
NovaOrynX64Halt:
    cli
.halt_forever:
    hlt
    jmp .halt_forever
NovaOrynX64WaitForInterrupt:
    hlt
    mov al, 1
    ret
NovaOrynX64Pause:
    pause
    mov al, 1
    ret
NovaOrynX64WritePort8:
    mov r8b, dl
    mov dx, cx
    mov al, r8b
    out dx, al
    mov al, 1
    ret
NovaOrynX64ReadPort8:
    mov r8, rdx
    mov dx, cx
    in al, dx
    mov [r8], al
    mov al, 1
    ret
NovaOrynX64WritePort16:
    mov r8w, dx
    mov dx, cx
    mov ax, r8w
    out dx, ax
    mov al, 1
    ret
NovaOrynX64ReadPort16:
    mov r8, rdx
    mov dx, cx
    in ax, dx
    mov [r8], ax
    mov al, 1
    ret
NovaOrynX64WritePort32:
    mov r8d, edx
    mov dx, cx
    mov eax, r8d
    out dx, eax
    mov al, 1
    ret
NovaOrynX64ReadPort32:
    mov r8, rdx
    mov dx, cx
    in eax, dx
    mov [r8], eax
    mov al, 1
    ret

NovaOrynX64ReadTimestampCounter:
    lfence
    rdtsc
    shl rdx, 32
    or rax, rdx
    ret

NovaOrynX64SupportsTsc:
    push rbx
    mov eax, 1
    cpuid
    bt edx, 4
    setc al
    movzx eax, al
    pop rbx
    ret

NovaOrynX64SupportsInvariantTsc:
    push rbx
    mov eax, 0x80000000
    cpuid
    cmp eax, 0x80000007
    jb .no_invariant_tsc
    mov eax, 0x80000007
    cpuid
    bt edx, 8
    setc al
    movzx eax, al
    pop rbx
    ret
.no_invariant_tsc:
    xor eax, eax
    pop rbx
    ret

NovaOrynX64ReadMmio64:
    mov rax, [rcx]
    ret
NovaOrynX64WriteMmio64:
    mov [rcx], rdx
    mfence
    mov al, 1
    ret

; ---------------------------------------------------------------------------
; Symmetric multiprocessing bootstrap support.
; The template below is copied by the BSP into a UEFI-reserved 4 KiB page below
; 1 MiB. APs enter it in real mode after SIPI, adopt the active CR3, transition
; to long mode, report their APIC ID, and park with interrupts disabled until a
; later scheduler release gives them managed work.
; ---------------------------------------------------------------------------

global NovaOrynX64GetCurrentApicId
global NovaOrynX64PrepareApplicationProcessorTrampoline
global NovaOrynX64GetApplicationProcessorStartupStatus
global NovaOrynX64GetApplicationProcessorObservedApicId

section .rdata align=16
bits 16
NovaOrynApTrampolineTemplate:
    cli
    cld
    push cs
    pop ds
    lgdt [NovaOrynApTrampolineGdtDescriptor - NovaOrynApTrampolineTemplate]

    mov eax, cr4
    or eax, 0x20                    ; CR4.PAE
    mov cr4, eax

    mov eax, [NovaOrynApTrampolineCr3 - NovaOrynApTrampolineTemplate]
    mov cr3, eax

    mov ecx, 0xC0000080             ; IA32_EFER
    rdmsr
    or eax, 0x00000100              ; EFER.LME
    wrmsr

    mov eax, cr0
    or eax, 0x80000001              ; CR0.PG | CR0.PE
    mov cr0, eax

    ; Operand-size override encodes ptr16:32. The BSP patches the absolute
    ; 32-bit linear target because the long-mode code segment has base zero.
    db 0x66, 0xEA
NovaOrynApTrampolineFarTarget:
    dd 0
    dw 0x0008

align 8
NovaOrynApTrampolineGdt:
    dq 0x0000000000000000
    dq 0x00AF9A000000FFFF           ; 64-bit kernel code
    dq 0x00CF92000000FFFF           ; kernel data
NovaOrynApTrampolineGdtEnd:
NovaOrynApTrampolineGdtDescriptor:
    dw NovaOrynApTrampolineGdtEnd - NovaOrynApTrampolineGdt - 1
NovaOrynApTrampolineGdtBase:
    dd 0

align 8
NovaOrynApTrampolineCr3:
    dd 0
    dd 0
NovaOrynApTrampolineStackTop:
    dq 0
NovaOrynApTrampolineStatus:
    dd 0
NovaOrynApTrampolineObservedApicId:
    dd 0

bits 64
align 16
NovaOrynApTrampolineLongMode:
    mov ax, 0x10
    mov ds, ax
    mov es, ax
    mov ss, ax
    xor ebp, ebp
    lea rsi, [rel NovaOrynApTrampolineCr3]
    mov rsp, [rsi + (NovaOrynApTrampolineStackTop - NovaOrynApTrampolineCr3)]
    and rsp, -16

    mov eax, 1
    cpuid
    shr ebx, 24
    mov [rsi + (NovaOrynApTrampolineObservedApicId - NovaOrynApTrampolineCr3)], ebx
    mov dword [rsi + (NovaOrynApTrampolineStatus - NovaOrynApTrampolineCr3)], 1
    mfence
    cli
.ap_park:
    hlt
    jmp .ap_park
NovaOrynApTrampolineTemplateEnd:

section .text
bits 64
NovaOrynX64GetCurrentApicId:
    push rbx
    xor eax, eax
    cpuid
    cmp eax, 0x0B
    jb .legacy_apic_id
    mov eax, 0x0B
    xor ecx, ecx
    cpuid
    test ebx, ebx
    jz .legacy_apic_id
    mov eax, edx
    pop rbx
    ret
.legacy_apic_id:
    mov eax, 1
    cpuid
    mov eax, ebx
    shr eax, 24
    pop rbx
    ret

; RCX = low-memory trampoline address, RDX = active CR3, R8 = AP stack top.
NovaOrynX64PrepareApplicationProcessorTrampoline:
    test rcx, rcx
    jz .prepare_failed
    test rcx, 0xFFF
    jnz .prepare_failed
    cmp rcx, 0x100000
    jae .prepare_failed
    test rdx, rdx
    jz .prepare_failed
    mov rax, 0xFFFFFFFF
    cmp rdx, rax
    ja .prepare_failed
    test r8, r8
    jz .prepare_failed

    push rsi
    push rdi
    mov r10, rcx
    lea rsi, [rel NovaOrynApTrampolineTemplate]
    mov rdi, r10
    mov ecx, NovaOrynApTrampolineTemplateEnd - NovaOrynApTrampolineTemplate
    rep movsb

    lea rax, [r10 + (NovaOrynApTrampolineGdt - NovaOrynApTrampolineTemplate)]
    mov [r10 + (NovaOrynApTrampolineGdtBase - NovaOrynApTrampolineTemplate)], eax
    lea rax, [r10 + (NovaOrynApTrampolineLongMode - NovaOrynApTrampolineTemplate)]
    mov [r10 + (NovaOrynApTrampolineFarTarget - NovaOrynApTrampolineTemplate)], eax
    mov [r10 + (NovaOrynApTrampolineCr3 - NovaOrynApTrampolineTemplate)], edx
    mov [r10 + (NovaOrynApTrampolineStackTop - NovaOrynApTrampolineTemplate)], r8
    mov dword [r10 + (NovaOrynApTrampolineStatus - NovaOrynApTrampolineTemplate)], 0
    mov dword [r10 + (NovaOrynApTrampolineObservedApicId - NovaOrynApTrampolineTemplate)], 0xFFFFFFFF
    mfence
    pop rdi
    pop rsi
    mov eax, 1
    ret
.prepare_failed:
    xor eax, eax
    ret

NovaOrynX64GetApplicationProcessorStartupStatus:
    test rcx, rcx
    jz .status_zero
    mov eax, [rcx + (NovaOrynApTrampolineStatus - NovaOrynApTrampolineTemplate)]
    ret
.status_zero:
    xor eax, eax
    ret

NovaOrynX64GetApplicationProcessorObservedApicId:
    test rcx, rcx
    jz .observed_invalid
    mov eax, [rcx + (NovaOrynApTrampolineObservedApicId - NovaOrynApTrampolineTemplate)]
    ret
.observed_invalid:
    mov eax, 0xFFFFFFFF
    ret


; Thread context layout (256 bytes):
;  0 RBX, 8 RBP, 16 RDI, 24 RSI, 32 R12, 40 R13, 48 R14, 56 R15,
; 64 RSP, 72 RIP, 80..239 XMM6..XMM15, 240 initial RCX argument.
; RCX=context, RDX=stack top, R8=entry point, R9=argument.
NovaOrynX64InitializeThreadContext:
    test rcx, rcx
    jz .thread_init_failed
    test rdx, rdx
    jz .thread_init_failed
    test r8, r8
    jz .thread_init_failed
    pxor xmm0, xmm0
    xor rax, rax
    mov [rcx + 0], rax
    mov [rcx + 8], rax
    mov [rcx + 16], rax
    mov [rcx + 24], rax
    mov [rcx + 32], rax
    mov [rcx + 40], rax
    mov [rcx + 48], rax
    mov [rcx + 56], rax
    movdqu [rcx + 80], xmm0
    movdqu [rcx + 96], xmm0
    movdqu [rcx + 112], xmm0
    movdqu [rcx + 128], xmm0
    movdqu [rcx + 144], xmm0
    movdqu [rcx + 160], xmm0
    movdqu [rcx + 176], xmm0
    movdqu [rcx + 192], xmm0
    movdqu [rcx + 208], xmm0
    movdqu [rcx + 224], xmm0
    and rdx, -16
    sub rdx, 40
    lea rax, [rel NovaOrynX64ThreadReturned]
    mov [rdx], rax
    mov [rcx + 64], rdx
    mov [rcx + 72], r8
    mov [rcx + 240], r9
    mov eax, 1
    ret
.thread_init_failed:
    xor eax, eax
    ret

; RCX=current context, RDX=next context.
NovaOrynX64SwitchThreadContext:
    test rcx, rcx
    jz .thread_switch_failed
    test rdx, rdx
    jz .thread_switch_failed
    mov [rcx + 0], rbx
    mov [rcx + 8], rbp
    mov [rcx + 16], rdi
    mov [rcx + 24], rsi
    mov [rcx + 32], r12
    mov [rcx + 40], r13
    mov [rcx + 48], r14
    mov [rcx + 56], r15
    lea rax, [rsp + 8]
    mov [rcx + 64], rax
    mov rax, [rsp]
    mov [rcx + 72], rax
    movdqu [rcx + 80], xmm6
    movdqu [rcx + 96], xmm7
    movdqu [rcx + 112], xmm8
    movdqu [rcx + 128], xmm9
    movdqu [rcx + 144], xmm10
    movdqu [rcx + 160], xmm11
    movdqu [rcx + 176], xmm12
    movdqu [rcx + 192], xmm13
    movdqu [rcx + 208], xmm14
    movdqu [rcx + 224], xmm15

    mov rbx, [rdx + 0]
    mov rbp, [rdx + 8]
    mov rdi, [rdx + 16]
    mov rsi, [rdx + 24]
    mov r12, [rdx + 32]
    mov r13, [rdx + 40]
    mov r14, [rdx + 48]
    mov r15, [rdx + 56]
    movdqu xmm6, [rdx + 80]
    movdqu xmm7, [rdx + 96]
    movdqu xmm8, [rdx + 112]
    movdqu xmm9, [rdx + 128]
    movdqu xmm10, [rdx + 144]
    movdqu xmm11, [rdx + 160]
    movdqu xmm12, [rdx + 176]
    movdqu xmm13, [rdx + 192]
    movdqu xmm14, [rdx + 208]
    movdqu xmm15, [rdx + 224]
    mov rsp, [rdx + 64]
    mov rcx, [rdx + 240]
    mov r10, [rdx + 72]
    mov eax, 1
    jmp r10
.thread_switch_failed:
    xor eax, eax
    ret

; Kernel thread entry points are non-returning at this roadmap stage.
NovaOrynX64ThreadReturned:
    cli
.thread_return_halt:
    hlt
    jmp .thread_return_halt

; User/kernel separation primitives.
NovaOrynX64IsExecuteDisableEnabled:
    mov ecx, 0xC0000080             ; IA32_EFER
    rdmsr
    shr eax, 11                     ; EFER.NXE
    and eax, 1
    ret

NovaOrynX64EnableKernelWriteProtect:
    mov rax, cr0
    bts rax, 16                     ; CR0.WP
    mov cr0, rax
    mov eax, 1
    ret

NovaOrynX64IsKernelWriteProtectEnabled:
    mov rax, cr0
    shr rax, 16
    and eax, 1
    ret

NovaOrynX64SupportsSmep:
    push rbx
    xor eax, eax
    cpuid
    cmp eax, 7
    jb .smep_not_supported
    mov eax, 7
    xor ecx, ecx
    cpuid
    bt ebx, 7
    setc al
    movzx eax, al
    pop rbx
    ret
.smep_not_supported:
    xor eax, eax
    pop rbx
    ret

NovaOrynX64EnableSmep:
    push rbx
    xor eax, eax
    cpuid
    cmp eax, 7
    jb .smep_unsupported
    mov eax, 7
    xor ecx, ecx
    cpuid
    bt ebx, 7
    jnc .smep_unsupported
    mov rax, cr4
    bts rax, 20                    ; CR4.SMEP
    mov cr4, rax
    mov eax, 1
    pop rbx
    ret
.smep_unsupported:
    xor eax, eax
    pop rbx
    ret

NovaOrynX64SupportsSmap:
    push rbx
    xor eax, eax
    cpuid
    cmp eax, 7
    jb .smap_not_supported
    mov eax, 7
    xor ecx, ecx
    cpuid
    bt ebx, 20
    setc al
    movzx eax, al
    pop rbx
    ret
.smap_not_supported:
    xor eax, eax
    pop rbx
    ret

