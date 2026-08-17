bits 64
default rel

section .data align=8

; Win64 compiler security-cookie ABI used by NativeAOT/ILC stack-protector
; instrumentation. NovaOryn supplies the symbols freestanding instead of linking
; the Windows CRT. The cookie is reseeded before any managed code executes.
global __security_cookie
global __security_cookie_complement
__security_cookie:
    dq 0x00002B992DDFA232
__security_cookie_complement:
    dq 0xFFFFD466D2205DCD

section .text

global NovaOrynRuntimeInitialize

NovaOrynRuntimeInitialize:
    ; Seed the compiler security cookie from values available without firmware or
    ; a CRT: TSC, the live bootstrap stack address, and the loaded image address.
    rdtsc
    shl rdx, 32
    or rax, rdx
    xor rax, rsp
    lea rcx, [rel __security_cookie]
    xor rax, rcx
    rol rax, 17
    test rax, rax
    jnz .cookie_nonzero
    mov rax, 0x00002B992DDFA232
.cookie_nonzero:
    mov [rel __security_cookie], rax
    not rax
    mov [rel __security_cookie_complement], rax
    mov al, 1
    ret
