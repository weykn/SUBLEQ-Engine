# SUBLEQ Engine

**SUBLEQ Engine** is a SUBLEQ compiler and virtual machine with a higher-level ISA that compiles down to pure SUBLEQ. It allows structured programming on top of a one-instruction architecture.

---

# ISA Status

- [x] Core Primitive
  - [x] subleq
- [ ] Arithmetic
  - [x] add
  - [x] ~~add_r~~
  - [x] ~~add_c~~
  - [x] sub
  - [ ] mul
  - [ ] div
  - [ ] mod
  - [ ] inc
  - [ ] dec
  - [ ] neg
  - [ ] abs
- [x] Data Definitions
  - [x] db
  - [x] dw
  - [x] dd
  - [x] dq
  - [x] do
  - [x] dws
  - [x] das
  - [x] equ
- [x] Memory Reservation
  - [x] resb
  - [x] resw
  - [x] resd
  - [x] resq
  - [x] reso
  - [x] resws
  - [x] resas
- [ ] Comparison 
  - [ ] cmp
  - [ ] test
- [ ] Unconditional Control Flow
  - [x] jmp
  - [x] hlt
  - [x] nop
  - [ ] call
  - [ ] ret
- [ ] Conditional Branching
  - [ ] je
  - [ ] jne
  - [ ] jl
  - [ ] jle
  - [ ] jg
  - [ ] jge
  - [ ] jz
  - [ ] jnz
- [ ] Bitwise Operations
  - [ ] and
  - [ ] or
  - [ ] xor
  - [ ] not
- [ ] Shift / Rotate
  - [ ] shl
  - [ ] shr
  - [ ] sar
  - [ ] rol
  - [ ] ror
- [ ] Stack Operations
  - [ ] push
  - [ ] pop
- [ ] Data Movement
  - [ ] mov
  - [ ] lea

