#once
#bits 16
#ruledef register {
    A => 0x0
	B => 0x1
}
#ruledef
{
    NOP 								=> 0x00 @ 0x00
	LDM.{reg: register} {mem: u8}  		=> (0x01 + reg)`8 @ mem
	LDI.{reg: register} {value: u8}		=> (0x03 + reg)`8 @ value
	LDBWA							    => 0x3E @ 0x00
    ADD   								=> 0x05 @ 0x00
    SUB   								=> 0x06 @ 0x00
	MUL									=> 0x38 @ 0x00
	DIV									=> 0x39 @ 0x00
	MUL.{reg: register}	{value: u8}		=> (0x3A + reg)`8 @ value
	DIV.{reg: register}	{value: u8}		=> (0x3C + reg)`8 @ value
	JMP {address: u8}   				=> 0x09 @ address
	JGT {address: u8}   				=> 0x0A @ address
	JLT {address: u8}   				=> 0x0B @ address
	JEQ {address: u8}   				=> 0x0C @ address
	JNEQ {address: u8}  				=> 0x0D @ address
	JC {address: u8}    				=> 0x0E @ address
	JZ {address: u8}    				=> 0x0F @ address
	HLT									=> 0x10 @ 0x00
	INT									=> 0x11 @ 0x00
	STM.{reg: register} {address: u8} 	=> (0x12 + reg)`8 @ address
	STM.B2A								=> 0x36 @ 0x00
	STM.I2A	{value: u8}					=> 0x37 @ value
	AND 								=> 0x14 @ 0x00
	OR									=> 0x15 @ 0x00
	XOR									=> 0x16 @ 0x00
	NOT									=> 0x17 @ 0x00
	AND {value: u8}						=> 0x18 @ value
	OR {value: u8}						=> 0x19 @ value
	XOR {value: u8}						=> 0x1A @ value
	CALL {address: u8}					=> 0x1B @ address
	RET 								=> 0x1C @ 0x00
	PUSH {value: u8}					=> 0x1D @ value
	POP {reg: register}					=> (0x1E + reg)`8 @ 0x00
	PUSH {reg: register}				=> (0x20 + reg)`8 @ 0x00
	INC.{reg: register} {value: u8}		=> (0x24 + reg)`8 @ value
	DEC.{reg: register} {value: u8}		=> (0x26 + reg)`8 @ value
	ROL.{reg: register} {value: u8}		=> (0x28 + reg)`8 @ value
	ROR.{reg: register} {value: u8}		=> (0x2A + reg)`8 @ value
	LSHFT.{reg: register} {value: u8}	=> (0x2C + reg)`8 @ value
	RSHFT.{reg: register} {value: u8}	=> (0x2E + reg)`8 @ value
	STARG.{arg: u4}	{value: u8}			=> (0xB0 + arg)`8 @ value
	STARG.{arg: u4}	{reg: register}		=> (0xC0 + (reg * 0x10) + arg)`8 @ 0x00
	LDARG.{arg: u4} {reg: register}		=> (0xE0 + (reg * 0x10) + arg)`8 @ 0x00
	LDS.L {reg: register}				=> (0x07 + reg)`8 @ 0x00
	LDS.H {reg: register}				=> (0x22 + reg)`8 @ 0x00
	SER.L {value: u8}					=> 0x30 @ value
	SER.H {value: u8}					=> 0x31 @ value
	SER.L {reg: register} 				=> (0x32 + reg)`8 @ 0x00
	SER.H {reg: register}				=> (0x34 + reg)`8 @ 0x00
}