bits 64
default rel
extern NovaOrynManagedInterruptDispatch

section .data align=8
NovaOrynX64InterruptDispatcher: dq 0
NovaOrynX64InterruptStackSwitch: times 256 db 0

section .text
global NovaOrynX64LoadInterruptDescriptorTable
global NovaOrynX64InstallManagedInterruptDispatcher
NovaOrynX64InstallManagedInterruptDispatcher:
    lea rax, [rel NovaOrynManagedInterruptDispatch]
    mov [rel NovaOrynX64InterruptDispatcher], rax
    mov al, 1
    ret

global NovaOrynX64SetInterruptDispatcher
global NovaOrynX64GetInterruptStub
global NovaOrynX64SetInterruptStackSwitch
global NovaOrynX64StopProcessor
global NovaOrynX64InterruptStubTable

NovaOrynX64LoadInterruptDescriptorTable:
    sub rsp, 16
    mov [rsp], dx
    mov [rsp + 2], rcx
    lidt [rsp]
    add rsp, 16
    mov al, 1
    ret

NovaOrynX64SetInterruptDispatcher:
    mov [rel NovaOrynX64InterruptDispatcher], rcx
    mov al, 1
    ret

NovaOrynX64SetInterruptStackSwitch:
    movzx eax, cl
    lea r8, [rel NovaOrynX64InterruptStackSwitch]
    mov [r8 + rax], dl
    mov al, 1
    ret

NovaOrynX64GetInterruptStub:
    movzx eax, cl
    lea rdx, [rel NovaOrynX64InterruptStubTable]
    mov rax, [rdx + rax * 8]
    ret

NovaOrynX64StopProcessor:
    cli
.halt:
    hlt
    jmp .halt

global NovaOrynX64InterruptStub0
NovaOrynX64InterruptStub0:
    push qword 0
    push qword 0
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub1
NovaOrynX64InterruptStub1:
    push qword 0
    push qword 1
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub2
NovaOrynX64InterruptStub2:
    push qword 0
    push qword 2
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub3
NovaOrynX64InterruptStub3:
    push qword 0
    push qword 3
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub4
NovaOrynX64InterruptStub4:
    push qword 0
    push qword 4
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub5
NovaOrynX64InterruptStub5:
    push qword 0
    push qword 5
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub6
NovaOrynX64InterruptStub6:
    push qword 0
    push qword 6
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub7
NovaOrynX64InterruptStub7:
    push qword 0
    push qword 7
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub8
NovaOrynX64InterruptStub8:
    push qword 8
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub9
NovaOrynX64InterruptStub9:
    push qword 0
    push qword 9
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub10
NovaOrynX64InterruptStub10:
    push qword 10
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub11
NovaOrynX64InterruptStub11:
    push qword 11
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub12
NovaOrynX64InterruptStub12:
    push qword 12
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub13
NovaOrynX64InterruptStub13:
    push qword 13
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub14
NovaOrynX64InterruptStub14:
    push qword 14
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub15
NovaOrynX64InterruptStub15:
    push qword 0
    push qword 15
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub16
NovaOrynX64InterruptStub16:
    push qword 0
    push qword 16
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub17
NovaOrynX64InterruptStub17:
    push qword 17
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub18
NovaOrynX64InterruptStub18:
    push qword 0
    push qword 18
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub19
NovaOrynX64InterruptStub19:
    push qword 0
    push qword 19
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub20
NovaOrynX64InterruptStub20:
    push qword 0
    push qword 20
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub21
NovaOrynX64InterruptStub21:
    push qword 21
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub22
NovaOrynX64InterruptStub22:
    push qword 0
    push qword 22
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub23
NovaOrynX64InterruptStub23:
    push qword 0
    push qword 23
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub24
NovaOrynX64InterruptStub24:
    push qword 0
    push qword 24
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub25
NovaOrynX64InterruptStub25:
    push qword 0
    push qword 25
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub26
NovaOrynX64InterruptStub26:
    push qword 0
    push qword 26
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub27
NovaOrynX64InterruptStub27:
    push qword 0
    push qword 27
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub28
NovaOrynX64InterruptStub28:
    push qword 0
    push qword 28
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub29
NovaOrynX64InterruptStub29:
    push qword 29
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub30
NovaOrynX64InterruptStub30:
    push qword 30
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub31
NovaOrynX64InterruptStub31:
    push qword 0
    push qword 31
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub32
NovaOrynX64InterruptStub32:
    push qword 0
    push qword 32
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub33
NovaOrynX64InterruptStub33:
    push qword 0
    push qword 33
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub34
NovaOrynX64InterruptStub34:
    push qword 0
    push qword 34
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub35
NovaOrynX64InterruptStub35:
    push qword 0
    push qword 35
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub36
NovaOrynX64InterruptStub36:
    push qword 0
    push qword 36
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub37
NovaOrynX64InterruptStub37:
    push qword 0
    push qword 37
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub38
NovaOrynX64InterruptStub38:
    push qword 0
    push qword 38
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub39
NovaOrynX64InterruptStub39:
    push qword 0
    push qword 39
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub40
NovaOrynX64InterruptStub40:
    push qword 0
    push qword 40
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub41
NovaOrynX64InterruptStub41:
    push qword 0
    push qword 41
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub42
NovaOrynX64InterruptStub42:
    push qword 0
    push qword 42
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub43
NovaOrynX64InterruptStub43:
    push qword 0
    push qword 43
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub44
NovaOrynX64InterruptStub44:
    push qword 0
    push qword 44
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub45
NovaOrynX64InterruptStub45:
    push qword 0
    push qword 45
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub46
NovaOrynX64InterruptStub46:
    push qword 0
    push qword 46
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub47
NovaOrynX64InterruptStub47:
    push qword 0
    push qword 47
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub48
NovaOrynX64InterruptStub48:
    push qword 0
    push qword 48
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub49
NovaOrynX64InterruptStub49:
    push qword 0
    push qword 49
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub50
NovaOrynX64InterruptStub50:
    push qword 0
    push qword 50
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub51
NovaOrynX64InterruptStub51:
    push qword 0
    push qword 51
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub52
NovaOrynX64InterruptStub52:
    push qword 0
    push qword 52
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub53
NovaOrynX64InterruptStub53:
    push qword 0
    push qword 53
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub54
NovaOrynX64InterruptStub54:
    push qword 0
    push qword 54
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub55
NovaOrynX64InterruptStub55:
    push qword 0
    push qword 55
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub56
NovaOrynX64InterruptStub56:
    push qword 0
    push qword 56
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub57
NovaOrynX64InterruptStub57:
    push qword 0
    push qword 57
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub58
NovaOrynX64InterruptStub58:
    push qword 0
    push qword 58
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub59
NovaOrynX64InterruptStub59:
    push qword 0
    push qword 59
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub60
NovaOrynX64InterruptStub60:
    push qword 0
    push qword 60
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub61
NovaOrynX64InterruptStub61:
    push qword 0
    push qword 61
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub62
NovaOrynX64InterruptStub62:
    push qword 0
    push qword 62
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub63
NovaOrynX64InterruptStub63:
    push qword 0
    push qword 63
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub64
NovaOrynX64InterruptStub64:
    push qword 0
    push qword 64
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub65
NovaOrynX64InterruptStub65:
    push qword 0
    push qword 65
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub66
NovaOrynX64InterruptStub66:
    push qword 0
    push qword 66
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub67
NovaOrynX64InterruptStub67:
    push qword 0
    push qword 67
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub68
NovaOrynX64InterruptStub68:
    push qword 0
    push qword 68
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub69
NovaOrynX64InterruptStub69:
    push qword 0
    push qword 69
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub70
NovaOrynX64InterruptStub70:
    push qword 0
    push qword 70
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub71
NovaOrynX64InterruptStub71:
    push qword 0
    push qword 71
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub72
NovaOrynX64InterruptStub72:
    push qword 0
    push qword 72
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub73
NovaOrynX64InterruptStub73:
    push qword 0
    push qword 73
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub74
NovaOrynX64InterruptStub74:
    push qword 0
    push qword 74
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub75
NovaOrynX64InterruptStub75:
    push qword 0
    push qword 75
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub76
NovaOrynX64InterruptStub76:
    push qword 0
    push qword 76
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub77
NovaOrynX64InterruptStub77:
    push qword 0
    push qword 77
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub78
NovaOrynX64InterruptStub78:
    push qword 0
    push qword 78
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub79
NovaOrynX64InterruptStub79:
    push qword 0
    push qword 79
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub80
NovaOrynX64InterruptStub80:
    push qword 0
    push qword 80
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub81
NovaOrynX64InterruptStub81:
    push qword 0
    push qword 81
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub82
NovaOrynX64InterruptStub82:
    push qword 0
    push qword 82
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub83
NovaOrynX64InterruptStub83:
    push qword 0
    push qword 83
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub84
NovaOrynX64InterruptStub84:
    push qword 0
    push qword 84
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub85
NovaOrynX64InterruptStub85:
    push qword 0
    push qword 85
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub86
NovaOrynX64InterruptStub86:
    push qword 0
    push qword 86
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub87
NovaOrynX64InterruptStub87:
    push qword 0
    push qword 87
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub88
NovaOrynX64InterruptStub88:
    push qword 0
    push qword 88
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub89
NovaOrynX64InterruptStub89:
    push qword 0
    push qword 89
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub90
NovaOrynX64InterruptStub90:
    push qword 0
    push qword 90
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub91
NovaOrynX64InterruptStub91:
    push qword 0
    push qword 91
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub92
NovaOrynX64InterruptStub92:
    push qword 0
    push qword 92
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub93
NovaOrynX64InterruptStub93:
    push qword 0
    push qword 93
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub94
NovaOrynX64InterruptStub94:
    push qword 0
    push qword 94
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub95
NovaOrynX64InterruptStub95:
    push qword 0
    push qword 95
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub96
NovaOrynX64InterruptStub96:
    push qword 0
    push qword 96
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub97
NovaOrynX64InterruptStub97:
    push qword 0
    push qword 97
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub98
NovaOrynX64InterruptStub98:
    push qword 0
    push qword 98
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub99
NovaOrynX64InterruptStub99:
    push qword 0
    push qword 99
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub100
NovaOrynX64InterruptStub100:
    push qword 0
    push qword 100
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub101
NovaOrynX64InterruptStub101:
    push qword 0
    push qword 101
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub102
NovaOrynX64InterruptStub102:
    push qword 0
    push qword 102
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub103
NovaOrynX64InterruptStub103:
    push qword 0
    push qword 103
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub104
NovaOrynX64InterruptStub104:
    push qword 0
    push qword 104
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub105
NovaOrynX64InterruptStub105:
    push qword 0
    push qword 105
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub106
NovaOrynX64InterruptStub106:
    push qword 0
    push qword 106
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub107
NovaOrynX64InterruptStub107:
    push qword 0
    push qword 107
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub108
NovaOrynX64InterruptStub108:
    push qword 0
    push qword 108
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub109
NovaOrynX64InterruptStub109:
    push qword 0
    push qword 109
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub110
NovaOrynX64InterruptStub110:
    push qword 0
    push qword 110
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub111
NovaOrynX64InterruptStub111:
    push qword 0
    push qword 111
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub112
NovaOrynX64InterruptStub112:
    push qword 0
    push qword 112
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub113
NovaOrynX64InterruptStub113:
    push qword 0
    push qword 113
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub114
NovaOrynX64InterruptStub114:
    push qword 0
    push qword 114
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub115
NovaOrynX64InterruptStub115:
    push qword 0
    push qword 115
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub116
NovaOrynX64InterruptStub116:
    push qword 0
    push qword 116
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub117
NovaOrynX64InterruptStub117:
    push qword 0
    push qword 117
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub118
NovaOrynX64InterruptStub118:
    push qword 0
    push qword 118
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub119
NovaOrynX64InterruptStub119:
    push qword 0
    push qword 119
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub120
NovaOrynX64InterruptStub120:
    push qword 0
    push qword 120
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub121
NovaOrynX64InterruptStub121:
    push qword 0
    push qword 121
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub122
NovaOrynX64InterruptStub122:
    push qword 0
    push qword 122
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub123
NovaOrynX64InterruptStub123:
    push qword 0
    push qword 123
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub124
NovaOrynX64InterruptStub124:
    push qword 0
    push qword 124
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub125
NovaOrynX64InterruptStub125:
    push qword 0
    push qword 125
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub126
NovaOrynX64InterruptStub126:
    push qword 0
    push qword 126
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub127
NovaOrynX64InterruptStub127:
    push qword 0
    push qword 127
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub128
NovaOrynX64InterruptStub128:
    push qword 0
    push qword 128
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub129
NovaOrynX64InterruptStub129:
    push qword 0
    push qword 129
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub130
NovaOrynX64InterruptStub130:
    push qword 0
    push qword 130
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub131
NovaOrynX64InterruptStub131:
    push qword 0
    push qword 131
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub132
NovaOrynX64InterruptStub132:
    push qword 0
    push qword 132
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub133
NovaOrynX64InterruptStub133:
    push qword 0
    push qword 133
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub134
NovaOrynX64InterruptStub134:
    push qword 0
    push qword 134
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub135
NovaOrynX64InterruptStub135:
    push qword 0
    push qword 135
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub136
NovaOrynX64InterruptStub136:
    push qword 0
    push qword 136
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub137
NovaOrynX64InterruptStub137:
    push qword 0
    push qword 137
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub138
NovaOrynX64InterruptStub138:
    push qword 0
    push qword 138
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub139
NovaOrynX64InterruptStub139:
    push qword 0
    push qword 139
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub140
NovaOrynX64InterruptStub140:
    push qword 0
    push qword 140
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub141
NovaOrynX64InterruptStub141:
    push qword 0
    push qword 141
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub142
NovaOrynX64InterruptStub142:
    push qword 0
    push qword 142
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub143
NovaOrynX64InterruptStub143:
    push qword 0
    push qword 143
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub144
NovaOrynX64InterruptStub144:
    push qword 0
    push qword 144
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub145
NovaOrynX64InterruptStub145:
    push qword 0
    push qword 145
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub146
NovaOrynX64InterruptStub146:
    push qword 0
    push qword 146
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub147
NovaOrynX64InterruptStub147:
    push qword 0
    push qword 147
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub148
NovaOrynX64InterruptStub148:
    push qword 0
    push qword 148
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub149
NovaOrynX64InterruptStub149:
    push qword 0
    push qword 149
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub150
NovaOrynX64InterruptStub150:
    push qword 0
    push qword 150
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub151
NovaOrynX64InterruptStub151:
    push qword 0
    push qword 151
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub152
NovaOrynX64InterruptStub152:
    push qword 0
    push qword 152
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub153
NovaOrynX64InterruptStub153:
    push qword 0
    push qword 153
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub154
NovaOrynX64InterruptStub154:
    push qword 0
    push qword 154
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub155
NovaOrynX64InterruptStub155:
    push qword 0
    push qword 155
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub156
NovaOrynX64InterruptStub156:
    push qword 0
    push qword 156
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub157
NovaOrynX64InterruptStub157:
    push qword 0
    push qword 157
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub158
NovaOrynX64InterruptStub158:
    push qword 0
    push qword 158
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub159
NovaOrynX64InterruptStub159:
    push qword 0
    push qword 159
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub160
NovaOrynX64InterruptStub160:
    push qword 0
    push qword 160
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub161
NovaOrynX64InterruptStub161:
    push qword 0
    push qword 161
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub162
NovaOrynX64InterruptStub162:
    push qword 0
    push qword 162
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub163
NovaOrynX64InterruptStub163:
    push qword 0
    push qword 163
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub164
NovaOrynX64InterruptStub164:
    push qword 0
    push qword 164
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub165
NovaOrynX64InterruptStub165:
    push qword 0
    push qword 165
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub166
NovaOrynX64InterruptStub166:
    push qword 0
    push qword 166
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub167
NovaOrynX64InterruptStub167:
    push qword 0
    push qword 167
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub168
NovaOrynX64InterruptStub168:
    push qword 0
    push qword 168
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub169
NovaOrynX64InterruptStub169:
    push qword 0
    push qword 169
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub170
NovaOrynX64InterruptStub170:
    push qword 0
    push qword 170
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub171
NovaOrynX64InterruptStub171:
    push qword 0
    push qword 171
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub172
NovaOrynX64InterruptStub172:
    push qword 0
    push qword 172
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub173
NovaOrynX64InterruptStub173:
    push qword 0
    push qword 173
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub174
NovaOrynX64InterruptStub174:
    push qword 0
    push qword 174
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub175
NovaOrynX64InterruptStub175:
    push qword 0
    push qword 175
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub176
NovaOrynX64InterruptStub176:
    push qword 0
    push qword 176
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub177
NovaOrynX64InterruptStub177:
    push qword 0
    push qword 177
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub178
NovaOrynX64InterruptStub178:
    push qword 0
    push qword 178
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub179
NovaOrynX64InterruptStub179:
    push qword 0
    push qword 179
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub180
NovaOrynX64InterruptStub180:
    push qword 0
    push qword 180
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub181
NovaOrynX64InterruptStub181:
    push qword 0
    push qword 181
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub182
NovaOrynX64InterruptStub182:
    push qword 0
    push qword 182
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub183
NovaOrynX64InterruptStub183:
    push qword 0
    push qword 183
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub184
NovaOrynX64InterruptStub184:
    push qword 0
    push qword 184
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub185
NovaOrynX64InterruptStub185:
    push qword 0
    push qword 185
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub186
NovaOrynX64InterruptStub186:
    push qword 0
    push qword 186
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub187
NovaOrynX64InterruptStub187:
    push qword 0
    push qword 187
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub188
NovaOrynX64InterruptStub188:
    push qword 0
    push qword 188
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub189
NovaOrynX64InterruptStub189:
    push qword 0
    push qword 189
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub190
NovaOrynX64InterruptStub190:
    push qword 0
    push qword 190
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub191
NovaOrynX64InterruptStub191:
    push qword 0
    push qword 191
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub192
NovaOrynX64InterruptStub192:
    push qword 0
    push qword 192
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub193
NovaOrynX64InterruptStub193:
    push qword 0
    push qword 193
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub194
NovaOrynX64InterruptStub194:
    push qword 0
    push qword 194
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub195
NovaOrynX64InterruptStub195:
    push qword 0
    push qword 195
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub196
NovaOrynX64InterruptStub196:
    push qword 0
    push qword 196
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub197
NovaOrynX64InterruptStub197:
    push qword 0
    push qword 197
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub198
NovaOrynX64InterruptStub198:
    push qword 0
    push qword 198
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub199
NovaOrynX64InterruptStub199:
    push qword 0
    push qword 199
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub200
NovaOrynX64InterruptStub200:
    push qword 0
    push qword 200
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub201
NovaOrynX64InterruptStub201:
    push qword 0
    push qword 201
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub202
NovaOrynX64InterruptStub202:
    push qword 0
    push qword 202
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub203
NovaOrynX64InterruptStub203:
    push qword 0
    push qword 203
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub204
NovaOrynX64InterruptStub204:
    push qword 0
    push qword 204
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub205
NovaOrynX64InterruptStub205:
    push qword 0
    push qword 205
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub206
NovaOrynX64InterruptStub206:
    push qword 0
    push qword 206
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub207
NovaOrynX64InterruptStub207:
    push qword 0
    push qword 207
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub208
NovaOrynX64InterruptStub208:
    push qword 0
    push qword 208
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub209
NovaOrynX64InterruptStub209:
    push qword 0
    push qword 209
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub210
NovaOrynX64InterruptStub210:
    push qword 0
    push qword 210
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub211
NovaOrynX64InterruptStub211:
    push qword 0
    push qword 211
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub212
NovaOrynX64InterruptStub212:
    push qword 0
    push qword 212
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub213
NovaOrynX64InterruptStub213:
    push qword 0
    push qword 213
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub214
NovaOrynX64InterruptStub214:
    push qword 0
    push qword 214
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub215
NovaOrynX64InterruptStub215:
    push qword 0
    push qword 215
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub216
NovaOrynX64InterruptStub216:
    push qword 0
    push qword 216
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub217
NovaOrynX64InterruptStub217:
    push qword 0
    push qword 217
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub218
NovaOrynX64InterruptStub218:
    push qword 0
    push qword 218
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub219
NovaOrynX64InterruptStub219:
    push qword 0
    push qword 219
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub220
NovaOrynX64InterruptStub220:
    push qword 0
    push qword 220
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub221
NovaOrynX64InterruptStub221:
    push qword 0
    push qword 221
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub222
NovaOrynX64InterruptStub222:
    push qword 0
    push qword 222
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub223
NovaOrynX64InterruptStub223:
    push qword 0
    push qword 223
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub224
NovaOrynX64InterruptStub224:
    push qword 0
    push qword 224
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub225
NovaOrynX64InterruptStub225:
    push qword 0
    push qword 225
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub226
NovaOrynX64InterruptStub226:
    push qword 0
    push qword 226
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub227
NovaOrynX64InterruptStub227:
    push qword 0
    push qword 227
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub228
NovaOrynX64InterruptStub228:
    push qword 0
    push qword 228
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub229
NovaOrynX64InterruptStub229:
    push qword 0
    push qword 229
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub230
NovaOrynX64InterruptStub230:
    push qword 0
    push qword 230
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub231
NovaOrynX64InterruptStub231:
    push qword 0
    push qword 231
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub232
NovaOrynX64InterruptStub232:
    push qword 0
    push qword 232
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub233
NovaOrynX64InterruptStub233:
    push qword 0
    push qword 233
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub234
NovaOrynX64InterruptStub234:
    push qword 0
    push qword 234
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub235
NovaOrynX64InterruptStub235:
    push qword 0
    push qword 235
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub236
NovaOrynX64InterruptStub236:
    push qword 0
    push qword 236
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub237
NovaOrynX64InterruptStub237:
    push qword 0
    push qword 237
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub238
NovaOrynX64InterruptStub238:
    push qword 0
    push qword 238
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub239
NovaOrynX64InterruptStub239:
    push qword 0
    push qword 239
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub240
NovaOrynX64InterruptStub240:
    push qword 0
    push qword 240
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub241
NovaOrynX64InterruptStub241:
    push qword 0
    push qword 241
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub242
NovaOrynX64InterruptStub242:
    push qword 0
    push qword 242
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub243
NovaOrynX64InterruptStub243:
    push qword 0
    push qword 243
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub244
NovaOrynX64InterruptStub244:
    push qword 0
    push qword 244
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub245
NovaOrynX64InterruptStub245:
    push qword 0
    push qword 245
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub246
NovaOrynX64InterruptStub246:
    push qword 0
    push qword 246
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub247
NovaOrynX64InterruptStub247:
    push qword 0
    push qword 247
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub248
NovaOrynX64InterruptStub248:
    push qword 0
    push qword 248
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub249
NovaOrynX64InterruptStub249:
    push qword 0
    push qword 249
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub250
NovaOrynX64InterruptStub250:
    push qword 0
    push qword 250
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub251
NovaOrynX64InterruptStub251:
    push qword 0
    push qword 251
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub252
NovaOrynX64InterruptStub252:
    push qword 0
    push qword 252
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub253
NovaOrynX64InterruptStub253:
    push qword 0
    push qword 253
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub254
NovaOrynX64InterruptStub254:
    push qword 0
    push qword 254
    jmp NovaOrynX64InterruptCommon

global NovaOrynX64InterruptStub255
NovaOrynX64InterruptStub255:
    push qword 0
    push qword 255
    jmp NovaOrynX64InterruptCommon

NovaOrynX64InterruptCommon:
    sub rsp, 224
    mov [rsp + 104], rax
    mov [rsp + 112], rbx
    mov [rsp + 120], rcx
    mov [rsp + 128], rdx
    mov [rsp + 136], rsi
    mov [rsp + 144], rdi
    mov [rsp + 152], rbp
    mov [rsp + 160], r8
    mov [rsp + 168], r9
    mov [rsp + 176], r10
    mov [rsp + 184], r11
    mov [rsp + 192], r12
    mov [rsp + 200], r13
    mov [rsp + 208], r14
    mov [rsp + 216], r15
    lea r10, [rsp + 224]
    mov rax, [r10 + 0]
    mov [rsp + 0], rax
    mov rax, [r10 + 8]
    mov [rsp + 8], rax
    mov rax, [r10 + 16]
    mov [rsp + 16], rax
    mov rax, [r10 + 24]
    mov [rsp + 24], rax
    mov rax, [r10 + 32]
    mov [rsp + 32], rax
    mov rax, cr0
    mov [rsp + 56], rax
    mov rax, cr2
    mov [rsp + 64], rax
    mov rax, cr3
    mov [rsp + 72], rax
    mov rax, cr4
    mov [rsp + 80], rax
    mov rax, [rsp + 24]
    and eax, 3
    setnz al
    movzx eax, al
    mov rdx, [rsp + 0]
    lea r8, [rel NovaOrynX64InterruptStackSwitch]
    cmp byte [r8 + rdx], 0
    je .stack_policy_ready
    or eax, 2
.stack_policy_ready:
    mov [rsp + 96], rax
    test eax, eax
    jz .same_privilege
    mov rax, [r10 + 40]
    mov [rsp + 40], rax
    mov rax, [r10 + 48]
    mov [rsp + 48], rax
    jmp .stack_done
.same_privilege:
    lea rax, [r10 + 40]
    mov [rsp + 40], rax
    xor eax, eax
    mov ax, ss
    mov [rsp + 48], rax
.stack_done:
    mov eax, 1
    cpuid
    shr ebx, 24
    mov [rsp + 88], rbx
    mov rax, [rel NovaOrynX64InterruptDispatcher]
    test rax, rax
    jz NovaOrynX64StopProcessor
    mov rcx, rsp
    sub rsp, 40
    call rax
    add rsp, 40
    cmp eax, 1
    je .resume
    cmp eax, 2
    je .resume
    jmp NovaOrynX64StopProcessor
.resume:
    lea r10, [rsp + 224]
    mov rax, [rsp + 16]
    mov [r10 + 16], rax
    mov rax, [rsp + 24]
    mov [r10 + 24], rax
    mov rax, [rsp + 32]
    mov [r10 + 32], rax
    cmp qword [rsp + 96], 0
    je .restore_registers
    mov rax, [rsp + 40]
    mov [r10 + 40], rax
    mov rax, [rsp + 48]
    mov [r10 + 48], rax
.restore_registers:
    mov rax, [rsp + 104]
    mov rbx, [rsp + 112]
    mov rcx, [rsp + 120]
    mov rdx, [rsp + 128]
    mov rsi, [rsp + 136]
    mov rdi, [rsp + 144]
    mov rbp, [rsp + 152]
    mov r8, [rsp + 160]
    mov r9, [rsp + 168]
    mov r10, [rsp + 176]
    mov r11, [rsp + 184]
    mov r12, [rsp + 192]
    mov r13, [rsp + 200]
    mov r14, [rsp + 208]
    mov r15, [rsp + 216]
    add rsp, 224
    add rsp, 16
    iretq



section .bss align=16
NovaOrynX64BootstrapIdt: resb 4096

section .text
global NovaOrynX64InitializeBootstrapInterrupts

; Builds all 256 gates and installs the bootstrap processor IDT.
NovaOrynX64InitializeBootstrapInterrupts:
    lea r10, [rel NovaOrynX64BootstrapIdt]
    lea r11, [rel NovaOrynX64InterruptStubTable]
    lea r9, [rel NovaOrynX64InterruptStackSwitch]
    xor r8d, r8d
.bootstrap_idt_loop:
    mov rax, [r11 + r8 * 8]
    mov rdx, r8
    shl rdx, 4
    add rdx, r10
    mov [rdx + 0], ax
    mov word [rdx + 2], 0x08
    mov byte [rdx + 4], 0
    cmp r8d, 8
    jne .check_nmi
    mov byte [rdx + 4], 1
    mov byte [r9 + r8], 1
    jmp .ist_done
.check_nmi:
    cmp r8d, 2
    jne .check_machine_check
    mov byte [rdx + 4], 2
    mov byte [r9 + r8], 1
    jmp .ist_done
.check_machine_check:
    cmp r8d, 18
    jne .ist_done
    mov byte [rdx + 4], 3
    mov byte [r9 + r8], 1
.ist_done:
    mov byte [rdx + 5], 0x8E
    shr rax, 16
    mov [rdx + 6], ax
    shr rax, 16
    mov [rdx + 8], eax
    mov dword [rdx + 12], 0
    inc r8d
    cmp r8d, 256
    jne .bootstrap_idt_loop
    sub rsp, 16
    mov word [rsp], 4095
    mov [rsp + 2], r10
    lidt [rsp]
    add rsp, 16
    mov eax, 1
    ret

align 8
NovaOrynX64InterruptStubTable:
    dq NovaOrynX64InterruptStub0
    dq NovaOrynX64InterruptStub1
    dq NovaOrynX64InterruptStub2
    dq NovaOrynX64InterruptStub3
    dq NovaOrynX64InterruptStub4
    dq NovaOrynX64InterruptStub5
    dq NovaOrynX64InterruptStub6
    dq NovaOrynX64InterruptStub7
    dq NovaOrynX64InterruptStub8
    dq NovaOrynX64InterruptStub9
    dq NovaOrynX64InterruptStub10
    dq NovaOrynX64InterruptStub11
    dq NovaOrynX64InterruptStub12
    dq NovaOrynX64InterruptStub13
    dq NovaOrynX64InterruptStub14
    dq NovaOrynX64InterruptStub15
    dq NovaOrynX64InterruptStub16
    dq NovaOrynX64InterruptStub17
    dq NovaOrynX64InterruptStub18
    dq NovaOrynX64InterruptStub19
    dq NovaOrynX64InterruptStub20
    dq NovaOrynX64InterruptStub21
    dq NovaOrynX64InterruptStub22
    dq NovaOrynX64InterruptStub23
    dq NovaOrynX64InterruptStub24
    dq NovaOrynX64InterruptStub25
    dq NovaOrynX64InterruptStub26
    dq NovaOrynX64InterruptStub27
    dq NovaOrynX64InterruptStub28
    dq NovaOrynX64InterruptStub29
    dq NovaOrynX64InterruptStub30
    dq NovaOrynX64InterruptStub31
    dq NovaOrynX64InterruptStub32
    dq NovaOrynX64InterruptStub33
    dq NovaOrynX64InterruptStub34
    dq NovaOrynX64InterruptStub35
    dq NovaOrynX64InterruptStub36
    dq NovaOrynX64InterruptStub37
    dq NovaOrynX64InterruptStub38
    dq NovaOrynX64InterruptStub39
    dq NovaOrynX64InterruptStub40
    dq NovaOrynX64InterruptStub41
    dq NovaOrynX64InterruptStub42
    dq NovaOrynX64InterruptStub43
    dq NovaOrynX64InterruptStub44
    dq NovaOrynX64InterruptStub45
    dq NovaOrynX64InterruptStub46
    dq NovaOrynX64InterruptStub47
    dq NovaOrynX64InterruptStub48
    dq NovaOrynX64InterruptStub49
    dq NovaOrynX64InterruptStub50
    dq NovaOrynX64InterruptStub51
    dq NovaOrynX64InterruptStub52
    dq NovaOrynX64InterruptStub53
    dq NovaOrynX64InterruptStub54
    dq NovaOrynX64InterruptStub55
    dq NovaOrynX64InterruptStub56
    dq NovaOrynX64InterruptStub57
    dq NovaOrynX64InterruptStub58
    dq NovaOrynX64InterruptStub59
    dq NovaOrynX64InterruptStub60
    dq NovaOrynX64InterruptStub61
    dq NovaOrynX64InterruptStub62
    dq NovaOrynX64InterruptStub63
    dq NovaOrynX64InterruptStub64
    dq NovaOrynX64InterruptStub65
    dq NovaOrynX64InterruptStub66
    dq NovaOrynX64InterruptStub67
    dq NovaOrynX64InterruptStub68
    dq NovaOrynX64InterruptStub69
    dq NovaOrynX64InterruptStub70
    dq NovaOrynX64InterruptStub71
    dq NovaOrynX64InterruptStub72
    dq NovaOrynX64InterruptStub73
    dq NovaOrynX64InterruptStub74
    dq NovaOrynX64InterruptStub75
    dq NovaOrynX64InterruptStub76
    dq NovaOrynX64InterruptStub77
    dq NovaOrynX64InterruptStub78
    dq NovaOrynX64InterruptStub79
    dq NovaOrynX64InterruptStub80
    dq NovaOrynX64InterruptStub81
    dq NovaOrynX64InterruptStub82
    dq NovaOrynX64InterruptStub83
    dq NovaOrynX64InterruptStub84
    dq NovaOrynX64InterruptStub85
    dq NovaOrynX64InterruptStub86
    dq NovaOrynX64InterruptStub87
    dq NovaOrynX64InterruptStub88
    dq NovaOrynX64InterruptStub89
    dq NovaOrynX64InterruptStub90
    dq NovaOrynX64InterruptStub91
    dq NovaOrynX64InterruptStub92
    dq NovaOrynX64InterruptStub93
    dq NovaOrynX64InterruptStub94
    dq NovaOrynX64InterruptStub95
    dq NovaOrynX64InterruptStub96
    dq NovaOrynX64InterruptStub97
    dq NovaOrynX64InterruptStub98
    dq NovaOrynX64InterruptStub99
    dq NovaOrynX64InterruptStub100
    dq NovaOrynX64InterruptStub101
    dq NovaOrynX64InterruptStub102
    dq NovaOrynX64InterruptStub103
    dq NovaOrynX64InterruptStub104
    dq NovaOrynX64InterruptStub105
    dq NovaOrynX64InterruptStub106
    dq NovaOrynX64InterruptStub107
    dq NovaOrynX64InterruptStub108
    dq NovaOrynX64InterruptStub109
    dq NovaOrynX64InterruptStub110
    dq NovaOrynX64InterruptStub111
    dq NovaOrynX64InterruptStub112
    dq NovaOrynX64InterruptStub113
    dq NovaOrynX64InterruptStub114
    dq NovaOrynX64InterruptStub115
    dq NovaOrynX64InterruptStub116
    dq NovaOrynX64InterruptStub117
    dq NovaOrynX64InterruptStub118
    dq NovaOrynX64InterruptStub119
    dq NovaOrynX64InterruptStub120
    dq NovaOrynX64InterruptStub121
    dq NovaOrynX64InterruptStub122
    dq NovaOrynX64InterruptStub123
    dq NovaOrynX64InterruptStub124
    dq NovaOrynX64InterruptStub125
    dq NovaOrynX64InterruptStub126
    dq NovaOrynX64InterruptStub127
    dq NovaOrynX64InterruptStub128
    dq NovaOrynX64InterruptStub129
    dq NovaOrynX64InterruptStub130
    dq NovaOrynX64InterruptStub131
    dq NovaOrynX64InterruptStub132
    dq NovaOrynX64InterruptStub133
    dq NovaOrynX64InterruptStub134
    dq NovaOrynX64InterruptStub135
    dq NovaOrynX64InterruptStub136
    dq NovaOrynX64InterruptStub137
    dq NovaOrynX64InterruptStub138
    dq NovaOrynX64InterruptStub139
    dq NovaOrynX64InterruptStub140
    dq NovaOrynX64InterruptStub141
    dq NovaOrynX64InterruptStub142
    dq NovaOrynX64InterruptStub143
    dq NovaOrynX64InterruptStub144
    dq NovaOrynX64InterruptStub145
    dq NovaOrynX64InterruptStub146
    dq NovaOrynX64InterruptStub147
    dq NovaOrynX64InterruptStub148
    dq NovaOrynX64InterruptStub149
    dq NovaOrynX64InterruptStub150
    dq NovaOrynX64InterruptStub151
    dq NovaOrynX64InterruptStub152
    dq NovaOrynX64InterruptStub153
    dq NovaOrynX64InterruptStub154
    dq NovaOrynX64InterruptStub155
    dq NovaOrynX64InterruptStub156
    dq NovaOrynX64InterruptStub157
    dq NovaOrynX64InterruptStub158
    dq NovaOrynX64InterruptStub159
    dq NovaOrynX64InterruptStub160
    dq NovaOrynX64InterruptStub161
    dq NovaOrynX64InterruptStub162
    dq NovaOrynX64InterruptStub163
    dq NovaOrynX64InterruptStub164
    dq NovaOrynX64InterruptStub165
    dq NovaOrynX64InterruptStub166
    dq NovaOrynX64InterruptStub167
    dq NovaOrynX64InterruptStub168
    dq NovaOrynX64InterruptStub169
    dq NovaOrynX64InterruptStub170
    dq NovaOrynX64InterruptStub171
    dq NovaOrynX64InterruptStub172
    dq NovaOrynX64InterruptStub173
    dq NovaOrynX64InterruptStub174
    dq NovaOrynX64InterruptStub175
    dq NovaOrynX64InterruptStub176
    dq NovaOrynX64InterruptStub177
    dq NovaOrynX64InterruptStub178
    dq NovaOrynX64InterruptStub179
    dq NovaOrynX64InterruptStub180
    dq NovaOrynX64InterruptStub181
    dq NovaOrynX64InterruptStub182
    dq NovaOrynX64InterruptStub183
    dq NovaOrynX64InterruptStub184
    dq NovaOrynX64InterruptStub185
    dq NovaOrynX64InterruptStub186
    dq NovaOrynX64InterruptStub187
    dq NovaOrynX64InterruptStub188
    dq NovaOrynX64InterruptStub189
    dq NovaOrynX64InterruptStub190
    dq NovaOrynX64InterruptStub191
    dq NovaOrynX64InterruptStub192
    dq NovaOrynX64InterruptStub193
    dq NovaOrynX64InterruptStub194
    dq NovaOrynX64InterruptStub195
    dq NovaOrynX64InterruptStub196
    dq NovaOrynX64InterruptStub197
    dq NovaOrynX64InterruptStub198
    dq NovaOrynX64InterruptStub199
    dq NovaOrynX64InterruptStub200
    dq NovaOrynX64InterruptStub201
    dq NovaOrynX64InterruptStub202
    dq NovaOrynX64InterruptStub203
    dq NovaOrynX64InterruptStub204
    dq NovaOrynX64InterruptStub205
    dq NovaOrynX64InterruptStub206
    dq NovaOrynX64InterruptStub207
    dq NovaOrynX64InterruptStub208
    dq NovaOrynX64InterruptStub209
    dq NovaOrynX64InterruptStub210
    dq NovaOrynX64InterruptStub211
    dq NovaOrynX64InterruptStub212
    dq NovaOrynX64InterruptStub213
    dq NovaOrynX64InterruptStub214
    dq NovaOrynX64InterruptStub215
    dq NovaOrynX64InterruptStub216
    dq NovaOrynX64InterruptStub217
    dq NovaOrynX64InterruptStub218
    dq NovaOrynX64InterruptStub219
    dq NovaOrynX64InterruptStub220
    dq NovaOrynX64InterruptStub221
    dq NovaOrynX64InterruptStub222
    dq NovaOrynX64InterruptStub223
    dq NovaOrynX64InterruptStub224
    dq NovaOrynX64InterruptStub225
    dq NovaOrynX64InterruptStub226
    dq NovaOrynX64InterruptStub227
    dq NovaOrynX64InterruptStub228
    dq NovaOrynX64InterruptStub229
    dq NovaOrynX64InterruptStub230
    dq NovaOrynX64InterruptStub231
    dq NovaOrynX64InterruptStub232
    dq NovaOrynX64InterruptStub233
    dq NovaOrynX64InterruptStub234
    dq NovaOrynX64InterruptStub235
    dq NovaOrynX64InterruptStub236
    dq NovaOrynX64InterruptStub237
    dq NovaOrynX64InterruptStub238
    dq NovaOrynX64InterruptStub239
    dq NovaOrynX64InterruptStub240
    dq NovaOrynX64InterruptStub241
    dq NovaOrynX64InterruptStub242
    dq NovaOrynX64InterruptStub243
    dq NovaOrynX64InterruptStub244
    dq NovaOrynX64InterruptStub245
    dq NovaOrynX64InterruptStub246
    dq NovaOrynX64InterruptStub247
    dq NovaOrynX64InterruptStub248
    dq NovaOrynX64InterruptStub249
    dq NovaOrynX64InterruptStub250
    dq NovaOrynX64InterruptStub251
    dq NovaOrynX64InterruptStub252
    dq NovaOrynX64InterruptStub253
    dq NovaOrynX64InterruptStub254
    dq NovaOrynX64InterruptStub255
