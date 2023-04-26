#include "8bitcpu.v4.asm"

CALL setup

loop:
	JMP loop

RetrieveCellData:
	PUSH A
	PUSH B
	LDARG.0 A
	LDBWA
	STARG.1 B
	INC.A 0x1
	LDBWA
	STARG.2 B
	INC.A 0x1
	LDBWA
	STARG.3 B
	POP B
	POP A
	RET
AssignCellData:
	PUSH A
	PUSH B
	LDARG.0 A
	LSHFT.A 0x3
	OR 0x81
	LDARG.1 B
	SER.H A
	SER.L B
	LDARG.2 B
	INC.A 0x1
	SER.H A
	SER.L B
	LDARG.3 B
	INC.A 0x2
	SER.H A
	SER.L B
	SER.H 0x0
	SER.L 0x0
	LDARG.0 A
	MUL.A 0x3
	LDARG.1 B
	STM.B2A
	LDARG.2 B
	INC.A 0x1
	STM.B2A
	LDARG.3 B
	INC.A 0x1
	STM.B2A
	POP B
	POP A
	RET

setup:
	STARG.0 0x0
	STARG.1 0x0
	STARG.2 0x0
	STARG.3 0x0
	CALL AssignCellData
	STARG.0 0x1
	CALL AssignCellData
	STARG.0 0x2
	CALL AssignCellData
	STARG.0 0x3
	CALL AssignCellData
	STARG.0 0x4
	CALL AssignCellData
	STARG.0 0x5
	CALL AssignCellData
	STARG.0 0x6
	CALL AssignCellData
	STARG.0 0x7
	CALL AssignCellData
	STARG.0 0x8
	CALL AssignCellData
	RET
