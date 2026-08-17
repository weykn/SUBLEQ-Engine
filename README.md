# SUBLEQ Engine

**SUBLEQ Engine** is a SUBLEQ compiler and virtual machine that layers a higher-level ISA on top of the one-instruction architecture, letting you write structured programs that compile down to pure SUBLEQ. It supports 8-, 16-, 32-, 64-, and 128-bit word sizes.

## Usage

```
Usage: subleq [options] <input file>
                                           
General Options:                                   
  -h, --help                               Show this help message and exit. 
  -v, --version                            Display version information.
                                                   
Compiler Options:                                  
  -o, --output <file>                      Specify the output file name.
  -w, --word <bits>                        Specify the default word size in bits (default: 8).
  -f, --fileless                           Compile and run without writing an output file.
      --entry <label>                      Set the starting label for execution.
      --address <bits>                     Force addressing size in bits.
      --flow-protection                    Prevent return-hijacking via shadow stack.

Virtual Machine Options:                           
  -m, --memory <bytes>                     Set virtual machine memory capacity in bytes (default: 1024).
  -r, --run                                Run program in virtual machine.
                                                   
Binary Options:                                    
  -i, --info                               Display information about the binary and exit.                                                                                                                                                                                                                        
                                                                                                                                                                                                                                                                                                                 
Debug/Developer Options:                                                                                                                                                                                                                                                                                         
  -g, --debug                              Include debug symbols in the generated binary.                                                                                                                                                                                                                        
      --verbose                            Enable verbose logging.                                                                                                                                                                                                                                               
      --try-sign                           Attempt to find a signature to an unsigned file.                                                                                                                                                                                                                      
      --no-signature                       Do not append the signature/metadata.                                                                                                                                                                                                                                 
      --read-signature                     Print signature information and exit.                                                                                                                                                                                                                                 
      --ignore-signature                   Ignore binary signature when loading.                                                                                                                                                                                                                                 
      --read-debug                         Display debug symbols from binary.                                                                                                                                                                                                                                    
      --debug-monitor <label/address>      Print the last state of a byte by label or address.                                                                                                                                                                                                                   
      --debug-exit-code <code>             Exit virtual machine on code.                                                                                                                                                                                                                                         
      --experimental-print                 Enable experimental terminal output optimizations.                                                                                                                                                                                                                    
      --rainbow                            Enable rainbow text.                     
```

## ISA Status

- [x] Core Primitive
  - [x] subleq
- [ ] Arithmetic
  - [x] add
  - [x] ~~add_r~~
  - [x] ~~add_c~~
  - [x] sub
  - [X] inc
  - [X] dec
  - [ ] mul
  - [ ] div
  - [ ] mod
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
  - [ ] ~~register~~
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

