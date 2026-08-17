.section .text,"ax",@progbits
.global NovaOrynX64DisableInterrupts
.type NovaOrynX64DisableInterrupts,@function
NovaOrynX64DisableInterrupts:
    cli
    movb $1, %al
    ret

.global NovaOrynX64EnableInterrupts
.type NovaOrynX64EnableInterrupts,@function
NovaOrynX64EnableInterrupts:
    sti
    movb $1, %al
    ret

.global NovaOrynX64AreInterruptsEnabled
.type NovaOrynX64AreInterruptsEnabled,@function
NovaOrynX64AreInterruptsEnabled:
    pushfq
    popq %rax
    shrq $9, %rax
    andq $1, %rax
    ret

.global NovaOrynX64Halt
.type NovaOrynX64Halt,@function
NovaOrynX64Halt:
    cli
.LNovaOrynHaltForever:
    hlt
    jmp .LNovaOrynHaltForever
.size NovaOrynX64Halt, .-NovaOrynX64Halt

.global NovaOrynX64WaitForInterrupt
.type NovaOrynX64WaitForInterrupt,@function
NovaOrynX64WaitForInterrupt:
    hlt
    movb $1, %al
    ret
.size NovaOrynX64WaitForInterrupt, .-NovaOrynX64WaitForInterrupt

.global NovaOrynX64Pause
.type NovaOrynX64Pause,@function
NovaOrynX64Pause:
    pause
    movb $1, %al
    ret
.size NovaOrynX64Pause, .-NovaOrynX64Pause


.global NovaOrynX64ReadTimestampCounter
.type NovaOrynX64ReadTimestampCounter,@function
NovaOrynX64ReadTimestampCounter:
    lfence
    rdtsc
    shlq $32, %rdx
    orq %rdx, %rax
    ret
.size NovaOrynX64ReadTimestampCounter, .-NovaOrynX64ReadTimestampCounter

.global NovaOrynX64SupportsTsc
.type NovaOrynX64SupportsTsc,@function
NovaOrynX64SupportsTsc:
    pushq %rbx
    movl $1, %eax
    cpuid
    btl $4, %edx
    setc %al
    movzbl %al, %eax
    popq %rbx
    ret
.size NovaOrynX64SupportsTsc, .-NovaOrynX64SupportsTsc

.global NovaOrynX64SupportsInvariantTsc
.type NovaOrynX64SupportsInvariantTsc,@function
NovaOrynX64SupportsInvariantTsc:
    pushq %rbx
    movl $0x80000000, %eax
    cpuid
    cmpl $0x80000007, %eax
    jb .LNoInvariantTsc
    movl $0x80000007, %eax
    cpuid
    btl $8, %edx
    setc %al
    movzbl %al, %eax
    popq %rbx
    ret
.LNoInvariantTsc:
    xorl %eax, %eax
    popq %rbx
    ret
.size NovaOrynX64SupportsInvariantTsc, .-NovaOrynX64SupportsInvariantTsc

.global NovaOrynX64WritePort8
.type NovaOrynX64WritePort8,@function
NovaOrynX64WritePort8:
    movw %di, %dx
    movb %sil, %al
    outb %al, %dx
    movb $1, %al
    ret

.global NovaOrynX64ReadPort8
.type NovaOrynX64ReadPort8,@function
NovaOrynX64ReadPort8:
    movw %di, %dx
    inb %dx, %al
    movb %al, (%rsi)
    movb $1, %al
    ret

.global NovaOrynX64WritePort16
.type NovaOrynX64WritePort16,@function
NovaOrynX64WritePort16:
    movw %di, %dx
    movw %si, %ax
    outw %ax, %dx
    movb $1, %al
    ret

.global NovaOrynX64ReadPort16
.type NovaOrynX64ReadPort16,@function
NovaOrynX64ReadPort16:
    movw %di, %dx
    inw %dx, %ax
    movw %ax, (%rsi)
    movb $1, %al
    ret

.global NovaOrynX64WritePort32
.type NovaOrynX64WritePort32,@function
NovaOrynX64WritePort32:
    movw %di, %dx
    movl %esi, %eax
    outl %eax, %dx
    movb $1, %al
    ret

.global NovaOrynX64ReadPort32
.type NovaOrynX64ReadPort32,@function
NovaOrynX64ReadPort32:
    movw %di, %dx
    inl %dx, %eax
    movl %eax, (%rsi)
    movb $1, %al
    ret
