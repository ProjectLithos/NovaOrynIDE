bits 64
default rel

section .text

extern NovaOrynManagedSyscallDispatch

global NovaOrynX64ConfigureSystemCalls
global NovaOrynX64EnableSmap
global NovaOrynX64IsSmapEnabled
global NovaOrynX64BeginUserMemoryAccess
global NovaOrynX64EndUserMemoryAccess
global NovaOrynX64SyscallEntry

; Per-CPU syscall state layout, addressed through IA32_KERNEL_GS_BASE after SWAPGS.
; +00 kernel syscall stack top
; +08 saved user RSP
; +10 saved user RIP (RCX from SYSCALL)
; +18 saved user RFLAGS (R11 from SYSCALL)
; +20 encoded syscall number
; +28 argument 0 (RDI)
; +30 argument 1 (RSI)
; +38 argument 2 (RDX)
; +40 argument 3 (R10)
; +48 argument 4 (R8)
; +50 argument 5 (R9)

%define IA32_EFER           0xC0000080
%define IA32_STAR           0xC0000081
%define IA32_LSTAR          0xC0000082
%define IA32_FMASK          0xC0000084
%define IA32_KERNEL_GS_BASE 0xC0000102

; RCX = syscall state virtual address, RDX = aligned kernel syscall stack top.
NovaOrynX64ConfigureSystemCalls:
    test rcx, rcx
    jz .configure_failed
    test rdx, rdx
    jz .configure_failed
    test rdx, 0xF
    jnz .configure_failed
    mov [rcx], rdx

    ; Make SWAPGS expose the supplied per-CPU state on ring-3 -> ring-0 entry.
    mov r8, rcx
    mov ecx, IA32_KERNEL_GS_BASE
    mov eax, r8d
    mov rdx, r8
    shr rdx, 32
    wrmsr

    ; Enable the SYSCALL extension.
    mov ecx, IA32_EFER
    rdmsr
    or eax, 1
    wrmsr

    ; STAR: kernel CS=0x08. SYSRET base=0x13, producing SS=0x1B and CS=0x23.
    mov ecx, IA32_STAR
    mov eax, 0
    mov edx, 0x00130008
    wrmsr

    mov ecx, IA32_LSTAR
    lea r8, [rel NovaOrynX64SyscallEntry]
    mov eax, r8d
    mov rdx, r8
    shr rdx, 32
    wrmsr

    ; Clear TF, IF and DF on entry. The dispatcher executes with interrupts disabled.
    mov ecx, IA32_FMASK
    mov eax, 0x700
    xor edx, edx
    wrmsr

    mov eax, 1
    ret
.configure_failed:
    xor eax, eax
    ret

NovaOrynX64EnableSmap:
    push rbx
    mov eax, 7
    xor ecx, ecx
    cpuid
    bt ebx, 20
    jnc .smap_unsupported
    mov rax, cr4
    bts rax, 21
    mov cr4, rax
    mov eax, 1
    pop rbx
    ret
.smap_unsupported:
    xor eax, eax
    pop rbx
    ret

NovaOrynX64IsSmapEnabled:
    mov rax, cr4
    shr rax, 21
    and eax, 1
    ret

; STAC/CLAC are executed only when CR4.SMAP is active, so the helpers are safe
; on processors that do not implement SMAP.
NovaOrynX64BeginUserMemoryAccess:
    mov rax, cr4
    bt rax, 21
    jnc .begin_done
    stac
.begin_done:
    mov eax, 1
    ret

NovaOrynX64EndUserMemoryAccess:
    mov rax, cr4
    bt rax, 21
    jnc .end_done
    clac
.end_done:
    mov eax, 1
    ret

; x64 user ABI accepted by NovaOryn:
;   RAX = explicitly namespaced service number
;   RDI,RSI,RDX,R10,R8,R9 = six arguments (Linux register order)
; SYSCALL supplies user RIP in RCX and user RFLAGS in R11.
NovaOrynX64SyscallEntry:
    swapgs
    mov [gs:0x08], rsp
    mov [gs:0x10], rcx
    mov [gs:0x18], r11
    mov [gs:0x20], rax
    mov [gs:0x28], rdi
    mov [gs:0x30], rsi
    mov [gs:0x38], rdx
    mov [gs:0x40], r10
    mov [gs:0x48], r8
    mov [gs:0x50], r9

    mov rsp, [gs:0x00]
    and rsp, -16

    ; Preserve the user registers that SYSCALL promises not to clobber.
    push r15
    push r14
    push r13
    push r12
    push rbp
    push rbx
    push rdi
    push rsi
    push r10
    push r9
    push r8
    push rdx

    ; Win64 call ABI: RCX,RDX,R8,R9 then stack arguments, plus shadow space.
    sub rsp, 64
    mov rcx, [gs:0x20]
    mov rdx, [gs:0x28]
    mov r8,  [gs:0x30]
    mov r9,  [gs:0x38]
    mov rax, [gs:0x40]
    mov [rsp+32], rax
    mov rax, [gs:0x48]
    mov [rsp+40], rax
    mov rax, [gs:0x50]
    mov [rsp+48], rax
    call NovaOrynManagedSyscallDispatch
    add rsp, 64

    ; RAX is the ABI return value. Restore every other preserved register.
    pop rdx
    pop r8
    pop r9
    pop r10
    pop rsi
    pop rdi
    pop rbx
    pop rbp
    pop r12
    pop r13
    pop r14
    pop r15

    mov rcx, [gs:0x10]
    mov r11, [gs:0x18]
    mov rsp, [gs:0x08]
    swapgs
    sysret
