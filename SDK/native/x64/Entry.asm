bits 64
default rel

section .text

global NovaOrynUefiEntry
%ifdef NOVAORYN_DEBUG
global NovaOrynDebugImageAnchor
%endif
global NovaOrynCaptureUefiFramebuffer
global NovaOrynCaptureFinalUefiMemoryMap
global NovaOrynCaptureUefiAcpiRoot
extern NovaOrynRuntimeInitialize
extern NovaOrynManagedEntry
extern NovaOrynX64Halt

; EFI_GRAPHICS_OUTPUT_PROTOCOL_GUID
section .rdata align=16
NovaOrynGraphicsOutputProtocolGuid:
    db 0xDE, 0xA9, 0x42, 0x90, 0xDC, 0x23, 0x38, 0x4A
    db 0x96, 0xFB, 0x7A, 0xDE, 0xD0, 0x80, 0x51, 0x6A

; Native boot context consumed by the managed no-CoreLib bootstrap.
; 00 UInt64 signature (ASCII "NOVAORYN")
; 08 UInt64 framebuffer address
; 10 UInt64 framebuffer size
; 18 UInt32 width
; 1C UInt32 height
; 20 UInt32 pixels per scan line
; 24 UInt32 UEFI pixel format
; 28 UInt32 red mask
; 2C UInt32 green mask
; 30 UInt32 blue mask
; 34 UInt32 reserved mask
; 38 UInt64 final UEFI memory-map address
; 40 UInt64 final UEFI memory-map byte length
; 48 UInt64 final UEFI map key accepted by ExitBootServices
; 50 UInt64 UEFI memory descriptor size
; 58 UInt32 UEFI memory descriptor version
; 5C UInt32 GetMemoryMap/ExitBootServices capture attempts
; 60 UInt64 final EFI_STATUS (zero on success)
; 68 UInt64 final-map flag (one only after ExitBootServices succeeds)
; 70 UInt64 UEFI-allocated bootstrap page-table workspace address
; 78 UInt64 UEFI-allocated bootstrap page-table workspace page count
; 80 UInt64 ACPI RSDP physical address from UEFI configuration tables
; 88 UInt64 UEFI-reserved application-processor SIPI trampoline address
; 90 UInt64 application-processor trampoline page count
section .data align=16
NovaOrynBootContext:
    dq 0x4E59524F41564F4E
    dq 0
    dq 0
    dd 0
    dd 0
    dd 0
    dd 0
    dd 0
    dd 0
    dd 0
    dd 0
    dq NovaOrynFinalMemoryMapBuffer
    dq 0
    dq 0
    dq 0
    dd 0
    dd 0
    dq 0
    dq 0
    dq 0
    dq 0
    dq 0
    dq 0
    dq 0

; The final memory map must be captured into storage allocated before the last
; GetMemoryMap call. A fixed 512 KiB buffer provides descriptor-growth headroom
; without calling AllocatePool between GetMemoryMap and ExitBootServices.
section .bss align=4096
NovaOrynFinalMemoryMapBuffer:
    resb 524288
NovaOrynFinalMemoryMapBufferEnd:

; NovaOryn must not continue managed execution on firmware-owned stack storage after
; ExitBootServices. Keep an image-owned, page-aligned bootstrap stack alive until
; the kernel stack arena is established by a later subsystem.
align 4096
NovaOrynBootstrapStack:
    resb 65536
NovaOrynBootstrapStackEnd:

section .text
NovaOrynUefiEntry:
%ifdef NOVAORYN_DEBUG
NovaOrynDebugImageAnchor:
    ; Debug builds stop here once, before any kernel code. The IDE consumes this
    ; trap internally to calculate the UEFI relocation delta, arms user source
    ; breakpoints, and immediately continues. It is never a user-visible stop.
    int3
%endif
    ; UEFI x64 enters with ImageHandle in RCX and EFI_SYSTEM_TABLE* in RDX.
    ; Preserve both values and maintain Windows x64 shadow space/alignment.
    push rbp
    mov rbp, rsp
    push r12
    push r13
    sub rsp, 32
    mov r12, rcx
    mov r13, rdx

    mov rcx, r13
    call NovaOrynCaptureUefiAcpiRoot

    mov rcx, r13
    call NovaOrynCaptureUefiFramebuffer
    test al, al
    jz NovaOrynX64Halt

    ; This routine obtains the map whose key is passed immediately to
    ; ExitBootServices. A stale key causes a fresh GetMemoryMap retry.
    mov rcx, r12
    mov rdx, r13
    call NovaOrynCaptureFinalUefiMemoryMap
    test al, al
    jz NovaOrynX64Halt

    ; ExitBootServices has succeeded. Abandon the firmware stack before any
    ; managed allocator or page-table code can reclaim firmware-owned storage.
    lea rsp, [rel NovaOrynBootstrapStackEnd]
    and rsp, -16
    xor ebp, ebp
    sub rsp, 32                 ; Windows x64 shadow space for subsequent calls.

    call NovaOrynRuntimeInitialize
    test al, al
    jz NovaOrynX64Halt

    cli
    lea rcx, [rel NovaOrynBootContext]
    call NovaOrynManagedEntry
    jmp NovaOrynX64Halt


; Captures the RSDP pointer from the UEFI configuration table before ExitBootServices.
; RCX = EFI_SYSTEM_TABLE*. Prefer ACPI 2.0 GUID, then fall back to ACPI 1.0 GUID.
NovaOrynCaptureUefiAcpiRoot:
    push rbx
    push rsi
    push rdi
    mov qword [rel NovaOrynBootContext + 0x80], 0
    test rcx, rcx
    jz .acpi_failed
    mov rbx, [rcx + 0x68]       ; NumberOfTableEntries
    mov rsi, [rcx + 0x70]       ; ConfigurationTable
    test rbx, rbx
    jz .acpi_failed
    test rsi, rsi
    jz .acpi_failed
    xor edi, edi                ; ACPI 1.0 fallback RSDP
.acpi_scan:
    ; EFI_ACPI_20_TABLE_GUID = 8868e871-e4f1-11d3-bc22-0080c73c8881
    mov rax, [rsi]
    mov rdx, 0x11D3E4F18868E871
    cmp rax, rdx
    jne .acpi_check_v1
    mov rax, [rsi + 8]
    mov rdx, 0x81883CC7800022BC
    cmp rax, rdx
    jne .acpi_check_v1
    mov rax, [rsi + 16]
    test rax, rax
    jz .acpi_check_v1
    mov [rel NovaOrynBootContext + 0x80], rax
    mov al, 1
    jmp .acpi_return
.acpi_check_v1:
    ; ACPI_TABLE_GUID = eb9d2d30-2d88-11d3-9a16-0090273fc14d
    mov rax, [rsi]
    mov rdx, 0x11D32D88EB9D2D30
    cmp rax, rdx
    jne .acpi_next
    mov rax, [rsi + 8]
    mov rdx, 0x4DC13F279000169A
    cmp rax, rdx
    jne .acpi_next
    mov rax, [rsi + 16]
    test rax, rax
    jz .acpi_next
    mov rdi, rax
.acpi_next:
    add rsi, 24
    dec rbx
    jnz .acpi_scan
    test rdi, rdi
    jz .acpi_failed
    mov [rel NovaOrynBootContext + 0x80], rdi
    mov al, 1
    jmp .acpi_return
.acpi_failed:
    xor eax, eax
.acpi_return:
    pop rdi
    pop rsi
    pop rbx
    ret

NovaOrynCaptureFinalUefiMemoryMap:
    ; RCX = EFI_HANDLE ImageHandle, RDX = EFI_SYSTEM_TABLE*.
    push rbx
    push rsi
    push rdi
    push r12
    push r13
    push r14
    sub rsp, 40                 ; 32-byte shadow space plus fifth argument.

    mov rbx, rcx
    test rbx, rbx
    jz .final_failed_no_status
    test rdx, rdx
    jz .final_failed_no_status

    mov rsi, [rdx + 0x60]       ; EFI_SYSTEM_TABLE.BootServices
    test rsi, rsi
    jz .final_failed_no_status
    mov r13, [rsi + 0x28]       ; EFI_BOOT_SERVICES.AllocatePages
    mov rdi, [rsi + 0x38]       ; EFI_BOOT_SERVICES.GetMemoryMap
    mov r12, [rsi + 0xE8]       ; EFI_BOOT_SERVICES.ExitBootServices
    test r13, r13
    jz .final_failed_no_status
    test rdi, rdi
    jz .final_failed_no_status
    test r12, r12
    jz .final_failed_no_status

    ; Capture one planning map while Boot Services are still available. The planner
    ; derives the page-table workspace requirement from ConventionalMemory extents.
    mov qword [rel NovaOrynBootContext + 0x40], NovaOrynFinalMemoryMapBufferEnd - NovaOrynFinalMemoryMapBuffer
    mov qword [rel NovaOrynBootContext + 0x48], 0
    mov qword [rel NovaOrynBootContext + 0x50], 0
    mov dword [rel NovaOrynBootContext + 0x58], 0
    lea rcx, [rel NovaOrynBootContext + 0x40]
    lea rdx, [rel NovaOrynFinalMemoryMapBuffer]
    lea r8, [rel NovaOrynBootContext + 0x48]
    lea r9, [rel NovaOrynBootContext + 0x50]
    lea rax, [rel NovaOrynBootContext + 0x58]
    mov [rsp + 32], rax
    call rdi
    test rax, rax
    jnz .final_failed_status
    call NovaOrynValidateCapturedMap
    test al, al
    jz .final_failed_no_status

    lea rcx, [rel NovaOrynFinalMemoryMapBuffer]
    mov rdx, [rel NovaOrynBootContext + 0x40]
    mov r8, [rel NovaOrynBootContext + 0x50]
    call NovaOrynPlanBootstrapPageTables
    test rax, rax
    jz .final_failed_no_status
    mov [rel NovaOrynBootContext + 0x78], rax
    mov qword [rel NovaOrynBootContext + 0x70], 0

    ; AllocateAnyPages + EfiLoaderData. Allocation intentionally happens before the
    ; final map/key capture and therefore becomes part of the retained final map.
    xor ecx, ecx
    mov edx, 2
    mov r8, rax
    lea r9, [rel NovaOrynBootContext + 0x70]
    call r13
    test rax, rax
    jnz .final_failed_status
    mov rax, [rel NovaOrynBootContext + 0x70]
    test rax, rax
    jz .final_failed_no_status
    test rax, 0xFFF
    jnz .final_failed_no_status

    ; Prove the reserved workspace is writable under the inherited UEFI mappings.
    mov qword [rax], 0
    mov rcx, [rel NovaOrynBootContext + 0x78]
    shl rcx, 12
    dec rcx
    mov byte [rax + rcx], 0

    ; Reserve one SIPI target page below 1 MiB while Boot Services can still
    ; satisfy AllocateMaxAddress. Failure is non-fatal: the managed SMP layer
    ; will retain BSP-only operation and report TrampolineUnavailable.
    mov qword [rel NovaOrynBootContext + 0x88], 0x000000000009F000
    mov qword [rel NovaOrynBootContext + 0x90], 0
    mov ecx, 1                      ; AllocateMaxAddress
    mov edx, 2                      ; EfiLoaderData
    mov r8d, 1                      ; one 4 KiB page
    lea r9, [rel NovaOrynBootContext + 0x88]
    call r13
    test rax, rax
    jnz .ap_trampoline_unavailable
    mov rax, [rel NovaOrynBootContext + 0x88]
    test rax, rax
    jz .ap_trampoline_unavailable
    test rax, 0xFFF
    jnz .ap_trampoline_unavailable
    cmp rax, 0x100000
    jae .ap_trampoline_unavailable
    mov qword [rax], 0
    mov byte [rax + 4095], 0
    mov qword [rel NovaOrynBootContext + 0x90], 1
    jmp .retry_final_map
.ap_trampoline_unavailable:
    mov qword [rel NovaOrynBootContext + 0x88], 0
    mov qword [rel NovaOrynBootContext + 0x90], 0

.retry_final_map:
    inc dword [rel NovaOrynBootContext + 0x5C]
    cmp dword [rel NovaOrynBootContext + 0x5C], 8
    ja .final_failed_no_status

    mov qword [rel NovaOrynBootContext + 0x40], NovaOrynFinalMemoryMapBufferEnd - NovaOrynFinalMemoryMapBuffer
    mov qword [rel NovaOrynBootContext + 0x48], 0
    mov qword [rel NovaOrynBootContext + 0x50], 0
    mov dword [rel NovaOrynBootContext + 0x58], 0

    lea rcx, [rel NovaOrynBootContext + 0x40]
    lea rdx, [rel NovaOrynFinalMemoryMapBuffer]
    lea r8, [rel NovaOrynBootContext + 0x48]
    lea r9, [rel NovaOrynBootContext + 0x50]
    lea rax, [rel NovaOrynBootContext + 0x58]
    mov [rsp + 32], rax
    call rdi
    test rax, rax
    jnz .final_failed_status

    call NovaOrynValidateCapturedMap
    test al, al
    jz .final_failed_no_status

    ; No allocation or firmware operation occurs between the successful
    ; GetMemoryMap above and this ExitBootServices call.
    mov rcx, rbx
    mov rdx, [rel NovaOrynBootContext + 0x48]
    call r12
    test rax, rax
    jz .final_succeeded

    mov [rel NovaOrynBootContext + 0x60], rax
    mov rdx, 0x8000000000000002 ; EFI_INVALID_PARAMETER: stale map key.
    cmp rax, rdx
    je .retry_final_map
    jmp .final_failed

.final_succeeded:
    mov qword [rel NovaOrynBootContext + 0x60], 0
    mov qword [rel NovaOrynBootContext + 0x68], 1
    mov al, 1
    jmp .final_return

.final_failed_status:
    mov [rel NovaOrynBootContext + 0x60], rax
    jmp .final_failed

.final_failed_no_status:
    mov qword [rel NovaOrynBootContext + 0x60], -1
.final_failed:
    xor eax, eax
.final_return:
    add rsp, 40
    pop r14
    pop r13
    pop r12
    pop rdi
    pop rsi
    pop rbx
    ret

; Validates the map metadata currently stored in NovaOrynBootContext.
NovaOrynValidateCapturedMap:
    cmp qword [rel NovaOrynBootContext + 0x40], 0
    je .invalid
    cmp qword [rel NovaOrynBootContext + 0x40], NovaOrynFinalMemoryMapBufferEnd - NovaOrynFinalMemoryMapBuffer
    ja .invalid
    cmp qword [rel NovaOrynBootContext + 0x50], 40
    jb .invalid
    test qword [rel NovaOrynBootContext + 0x50], 7
    jnz .invalid
    mov rax, [rel NovaOrynBootContext + 0x40]
    xor edx, edx
    div qword [rel NovaOrynBootContext + 0x50]
    test rdx, rdx
    jnz .invalid
    test rax, rax
    jz .invalid
    mov al, 1
    ret
.invalid:
    xor eax, eax
    ret

; Calculates a conservative page-table workspace from the current UEFI map.
; RCX = map, RDX = map bytes, R8 = descriptor bytes. Returns pages in RAX.
; The plan assumes 2 MiB direct-map leaves; 1 GiB support can only reduce usage.
NovaOrynPlanBootstrapPageTables:
    push rbx
    push rsi
    push rdi
    push r12
    mov rsi, rcx
    mov rdi, rdx
    mov r12, r8
    test rsi, rsi
    jz .plan_fail
    cmp r12, 40
    jb .plan_fail
    mov rax, rdi
    xor edx, edx
    div r12
    test rdx, rdx
    jnz .plan_fail
    mov rbx, rax                ; descriptor count
    mov r10, 3                 ; private PML4 + two allocation-split edge PT pages
.plan_loop:
    test rbx, rbx
    jz .plan_done
    cmp dword [rsi], 7         ; EfiConventionalMemory
    jne .plan_next
    mov rax, [rsi + 0x20]      ; attributes
    bt rax, 63                 ; EFI_MEMORY_RUNTIME
    jc .plan_next
    mov r9, [rsi + 0x08]       ; physical start
    mov rax, [rsi + 0x18]      ; number of pages
    test rax, rax
    jz .plan_next
    mov rcx, rax
    shr rcx, 52
    test rcx, rcx
    jnz .plan_fail
    shl rax, 12
    mov r11, r9
    add r11, rax               ; exclusive physical end
    jc .plan_fail
    dec r11                    ; inclusive end

    ; One PDPT page per touched 512 GiB region (over-counting duplicates is safe).
    mov rax, r11
    shr rax, 39
    mov rcx, r9
    shr rcx, 39
    sub rax, rcx
    inc rax
    add r10, rax
    jc .plan_fail

    ; One PD page per touched 1 GiB region when using 2 MiB leaves.
    mov rax, r11
    shr rax, 30
    mov rcx, r9
    shr rcx, 30
    sub rax, rcx
    inc rax
    add r10, rax
    jc .plan_fail

    ; At most two PT pages are needed for unaligned 2 MiB edge fragments.
    test r9, 0x1FFFFF
    jz .plan_end_edge
    inc r10
.plan_end_edge:
    inc r11
    test r11, 0x1FFFFF
    jz .plan_next
    mov rax, r9
    shr rax, 21
    mov rcx, r11
    dec rcx
    shr rcx, 21
    cmp rax, rcx
    je .plan_next
    inc r10
.plan_next:
    add rsi, r12
    dec rbx
    jmp .plan_loop
.plan_done:
    ; Keep the pre-firmware allocation bounded to 256 MiB.
    test r10, r10
    jz .plan_fail
    cmp r10, 65536
    ja .plan_fail
    mov rax, r10
    jmp .plan_return
.plan_fail:
    xor eax, eax
.plan_return:
    pop r12
    pop rdi
    pop rsi
    pop rbx
    ret

NovaOrynCaptureUefiFramebuffer:
    ; RCX = EFI_SYSTEM_TABLE*. Preserve RBX and allocate shadow space plus
    ; one local qword used as the LocateProtocol output slot.
    push rbx
    sub rsp, 48
    mov qword [rsp + 32], 0

    test rcx, rcx
    jz .failed

    ; EFI_SYSTEM_TABLE.BootServices is at offset 0x60 on x64.
    mov rax, [rcx + 0x60]
    test rax, rax
    jz .failed

    ; EFI_BOOT_SERVICES.LocateProtocol is at offset 0x140.
    mov rax, [rax + 0x140]
    test rax, rax
    jz .failed

    lea rcx, [rel NovaOrynGraphicsOutputProtocolGuid]
    xor edx, edx
    lea r8, [rsp + 32]
    call rax
    test rax, rax
    jnz .failed

    mov rbx, [rsp + 32]
    test rbx, rbx
    jz .failed

    ; EFI_GRAPHICS_OUTPUT_PROTOCOL.Mode is at offset 0x18.
    mov rbx, [rbx + 0x18]
    test rbx, rbx
    jz .failed

    ; EFI_GRAPHICS_OUTPUT_PROTOCOL_MODE.Info is at 0x08.
    mov rdx, [rbx + 0x08]
    test rdx, rdx
    jz .failed

    ; FrameBufferBase and FrameBufferSize are at 0x18 and 0x20.
    mov rax, [rbx + 0x18]
    mov [rel NovaOrynBootContext + 0x08], rax
    mov rax, [rbx + 0x20]
    mov [rel NovaOrynBootContext + 0x10], rax

    ; EFI_GRAPHICS_OUTPUT_MODE_INFORMATION fields.
    mov eax, [rdx + 0x04]
    mov [rel NovaOrynBootContext + 0x18], eax
    mov eax, [rdx + 0x08]
    mov [rel NovaOrynBootContext + 0x1C], eax
    mov eax, [rdx + 0x20]
    mov [rel NovaOrynBootContext + 0x20], eax
    mov eax, [rdx + 0x0C]
    mov [rel NovaOrynBootContext + 0x24], eax
    mov eax, [rdx + 0x10]
    mov [rel NovaOrynBootContext + 0x28], eax
    mov eax, [rdx + 0x14]
    mov [rel NovaOrynBootContext + 0x2C], eax
    mov eax, [rdx + 0x18]
    mov [rel NovaOrynBootContext + 0x30], eax
    mov eax, [rdx + 0x1C]
    mov [rel NovaOrynBootContext + 0x34], eax

    mov al, 1
    add rsp, 48
    pop rbx
    ret

.failed:
    xor eax, eax
    add rsp, 48
    pop rbx
    ret
