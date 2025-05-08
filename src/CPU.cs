namespace Emulator
{
    public record Instruction
    (
        string Mnemonic,
        byte Length,
        byte Cycles,
        byte AdditionalCycles,
        Action<byte[]> Handler
    );

    enum CPUMode
    {
        Active,
        Halted,
        Stopped,
    }

    public class CPU
    {
        private readonly Bus _bus;
        private readonly Instruction[] _instructionTable = new Instruction[256];
        private Registers _regs = new Registers();
        private bool _ime = false;
        private CPUMode _mode;

        private int _ranInstructions;

        public CPU(Bus bus)
        {
            _bus = bus;
            InitializeInstructionTable();
            Reset();
        }

        private void InitializeInstructionTable()
        {
            // set all instructions to 'UNIMPLEMENTED' by default
            for(int i = 0; i < _instructionTable.Length; i++)
                _instructionTable[i] = new Instruction("UNIMPLEMENTED", 4, 4, 0, ibytes => UnimplementedInstruction(ibytes));

            // nop
            _instructionTable[0x00] = new Instruction("nop", 1, 4, 0, _ => { /* do nothing */ });
            _instructionTable[0x10] = new Instruction("stop 0", 2, 8, 0, _ => _mode = CPUMode.Stopped);
            _instructionTable[0x76] = new Instruction("halt", 1, 4, 0, _ => _mode = CPUMode.Halted);

            // ld r16,imm16
            _instructionTable[0x01] = new Instruction("ld bc,imm16", 3, 12, 0, ibytes => {
                _regs.BC = (ushort)(ibytes[2] << 8 | ibytes[1]);
            });
            _instructionTable[0x11] = new Instruction("ld de,imm16", 3, 12, 0, ibytes => {
                _regs.DE = (ushort)(ibytes[2] << 8 | ibytes[1]);
            });
            _instructionTable[0x21] = new Instruction("ld hl,imm16", 3, 12, 0, ibytes => {
                _regs.HL = (ushort)(ibytes[2] << 8 | ibytes[1]);
            });
            _instructionTable[0x31] = new Instruction("ld sp,imm16", 3, 12, 0, ibytes => {
                _regs.SP = (ushort)(ibytes[2] << 8 | ibytes[1]);
            });

            // ld r16mem,a
            _instructionTable[0x02] = new Instruction("ld (hl-),a", 1, 8, 0, _ => {
                _bus.WriteByte(_regs.BC, _regs.A);
            });
            _instructionTable[0x12] = new Instruction("ld (hl-),a", 1, 8, 0, _ => {
                _bus.WriteByte(_regs.DE, _regs.A);
            });
            _instructionTable[0x22] = new Instruction("ld (hl-),a", 1, 8, 0, _ => {
                _bus.WriteByte(_regs.HL++, _regs.A);
            });
            _instructionTable[0x32] = new Instruction("ld (hl-),a", 1, 8, 0, _ => {
                _bus.WriteByte(_regs.HL--, _regs.A);
            });

            // ld r8,imm8
            _instructionTable[0x06] = new Instruction("ld b,imm8", 2, 8, 0, ibytes => { _regs.B = ibytes[1]; });
            _instructionTable[0x0E] = new Instruction("ld c,imm8", 2, 8, 0, ibytes => { _regs.C = ibytes[1]; });
            _instructionTable[0x16] = new Instruction("ld d,imm8", 2, 8, 0, ibytes => { _regs.D = ibytes[1]; });
            _instructionTable[0x1E] = new Instruction("ld e,imm8", 2, 8, 0, ibytes => { _regs.E = ibytes[1]; });
            _instructionTable[0x26] = new Instruction("ld h,imm8", 2, 8, 0, ibytes => { _regs.H = ibytes[1]; });
            _instructionTable[0x2E] = new Instruction("ld l,imm8", 2, 8, 0, ibytes => { _regs.L = ibytes[1]; });
            _instructionTable[0x36] = new Instruction("ld [hl],imm8", 2, 8, 0, ibytes => {
                _bus.WriteByte(_regs.HL, ibytes[1]);
            });
            _instructionTable[0x3E] = new Instruction("ld a,imm8", 2, 8, 0, ibytes => { _regs.A = ibytes[1]; });

            // dec r8
            _instructionTable[0x05] = new Instruction("dec b", 1, 4, 0, _ => { DEC__r8(ref _regs.B); });
            _instructionTable[0x0D] = new Instruction("dec c", 1, 4, 0, _ => { DEC__r8(ref _regs.C); });
            _instructionTable[0x15] = new Instruction("dec d", 1, 4, 0, _ => { DEC__r8(ref _regs.D); });
            _instructionTable[0x1D] = new Instruction("dec e", 1, 4, 0, _ => { DEC__r8(ref _regs.E); });
            _instructionTable[0x25] = new Instruction("dec h", 1, 4, 0, _ => { DEC__r8(ref _regs.H); });
            _instructionTable[0x2D] = new Instruction("dec l", 1, 4, 0, _ => { DEC__r8(ref _regs.L); });
            _instructionTable[0x35] = new Instruction("dec (hl)", 1, 4, 0, _ => {
                DEC__r8(ref _bus.GetReferenceToByte(_regs.HL));
            });
            _instructionTable[0x3D] = new Instruction("dec a", 1, 4, 0, _ => { DEC__r8(ref _regs.A); });

            // inc r8
            _instructionTable[0x04] = new Instruction("inc b", 1, 4, 0, _ => { INC__r8(ref _regs.B); });
            _instructionTable[0x0C] = new Instruction("inc c", 1, 4, 0, _ => { INC__r8(ref _regs.C); });
            _instructionTable[0x14] = new Instruction("inc d", 1, 4, 0, _ => { INC__r8(ref _regs.D); });
            _instructionTable[0x1C] = new Instruction("inc e", 1, 4, 0, _ => { INC__r8(ref _regs.E); });
            _instructionTable[0x24] = new Instruction("inc h", 1, 4, 0, _ => { INC__r8(ref _regs.H); });
            _instructionTable[0x2C] = new Instruction("inc l", 1, 4, 0, _ => { INC__r8(ref _regs.L); });
            _instructionTable[0x34] = new Instruction("inc (hl)", 1, 4, 0, _ => {
                INC__r8(ref _bus.GetReferenceToByte(_regs.HL));
            });
            _instructionTable[0x3C] = new Instruction("inc a", 1, 4, 0, _ => { INC__r8(ref _regs.A); });
                        
            // ret
            _instructionTable[0xC0] = new Instruction("ret nz", 1, 20, 8, _ => {
                if(!_regs.GetFlag(CPUFlags.Z))
                    _regs.PC = PopWord();
            });

            // jr cond,imm8
            _instructionTable[0x18] = new Instruction("jr imm8", 2, 12, 0, ibytes => {
                sbyte offset = (sbyte)ibytes[1];
                _regs.PC = (ushort)(_regs.PC + offset);
            });
            _instructionTable[0x20] = new Instruction("jr nz,imm8", 2, 12, 8, ibytes => {
                sbyte offset = (sbyte)ibytes[1];
                if(!_regs.GetFlag(CPUFlags.Z))
                    _regs.PC = (ushort)(_regs.PC + offset);
            });
            _instructionTable[0x28] = new Instruction("jr z,imm8", 2, 12, 8, ibytes => {
                sbyte offset = (sbyte)ibytes[1];
                if(_regs.GetFlag(CPUFlags.Z))
                    _regs.PC = (ushort)(_regs.PC + offset);
            });
            _instructionTable[0x30] = new Instruction("jr nc,imm8", 2, 12, 8, ibytes => {
                sbyte offset = (sbyte)ibytes[1];
                if(!_regs.GetFlag(CPUFlags.C))
                    _regs.PC = (ushort)(_regs.PC + offset);
            });
            _instructionTable[0x38] = new Instruction("jr c,imm8", 2, 12, 8, ibytes => {
                sbyte offset = (sbyte)ibytes[1];
                if(_regs.GetFlag(CPUFlags.C))
                    _regs.PC = (ushort)(_regs.PC + offset);
            });


            ///// START - LD R8,R*8 /////

            // ld b,r8
            _instructionTable[0x40] = new Instruction("ld b, b", 1, 8, 0, _ => {});
            _instructionTable[0x41] = new Instruction("ld b, c", 1, 8, 0, _ => _regs.B = _regs.C);
            _instructionTable[0x42] = new Instruction("ld b, d", 1, 8, 0, _ => _regs.B = _regs.D);
            _instructionTable[0x43] = new Instruction("ld b, e", 1, 8, 0, _ => _regs.B = _regs.E);
            _instructionTable[0x44] = new Instruction("ld b, h", 1, 8, 0, _ => _regs.B = _regs.H);
            _instructionTable[0x45] = new Instruction("ld b, l", 1, 8, 0, _ => _regs.B = _regs.L);
            _instructionTable[0x46] = new Instruction("ld b, (hl)", 1, 8, 0, _ => _regs.B = _bus.ReadByte(_regs.HL));
            _instructionTable[0x47] = new Instruction("ld b, a", 1, 8, 0, _ => _regs.B = _regs.A);

            // ld c,r8
            _instructionTable[0x48] = new Instruction("ld c, b", 1, 8, 0, _ => _regs.C = _regs.B);
            _instructionTable[0x49] = new Instruction("ld c, c", 1, 8, 0, _ => {});
            _instructionTable[0x4A] = new Instruction("ld c, d", 1, 8, 0, _ => _regs.C = _regs.D);
            _instructionTable[0x4B] = new Instruction("ld c, e", 1, 8, 0, _ => _regs.C = _regs.E);
            _instructionTable[0x4C] = new Instruction("ld c, h", 1, 8, 0, _ => _regs.C = _regs.H);
            _instructionTable[0x4D] = new Instruction("ld c, l", 1, 8, 0, _ => _regs.C = _regs.L);
            _instructionTable[0x4E] = new Instruction("ld c, (hl)", 1, 8, 0, _ => _regs.C = _bus.ReadByte(_regs.HL));
            _instructionTable[0x4F] = new Instruction("ld c, a", 1, 8, 0, _ => _regs.C = _regs.A);

            // ld d,r8
            _instructionTable[0x50] = new Instruction("ld d, b", 1, 8, 0, _ => _regs.D = _regs.B);
            _instructionTable[0x51] = new Instruction("ld d, c", 1, 8, 0, _ => _regs.D = _regs.C);
            _instructionTable[0x52] = new Instruction("ld d, d", 1, 8, 0, _ => {});
            _instructionTable[0x53] = new Instruction("ld d, e", 1, 8, 0, _ => _regs.D = _regs.E);
            _instructionTable[0x54] = new Instruction("ld d, h", 1, 8, 0, _ => _regs.D = _regs.H);
            _instructionTable[0x55] = new Instruction("ld d, l", 1, 8, 0, _ => _regs.D = _regs.L);
            _instructionTable[0x56] = new Instruction("ld d, (hl)", 1, 8, 0, _ => _regs.D = _bus.ReadByte(_regs.HL));
            _instructionTable[0x57] = new Instruction("ld d, a", 1, 8, 0, _ => _regs.D = _regs.A);

            // ld e,r8
            _instructionTable[0x58] = new Instruction("ld e, b", 1, 8, 0, _ => _regs.E = _regs.B);
            _instructionTable[0x59] = new Instruction("ld e, c", 1, 8, 0, _ => _regs.E = _regs.C);
            _instructionTable[0x5A] = new Instruction("ld e, d", 1, 8, 0, _ => _regs.E = _regs.D);
            _instructionTable[0x5B] = new Instruction("ld e, e", 1, 8, 0, _ => {});
            _instructionTable[0x5C] = new Instruction("ld e, h", 1, 8, 0, _ => _regs.E = _regs.H);
            _instructionTable[0x5D] = new Instruction("ld e, l", 1, 8, 0, _ => _regs.E = _regs.L);
            _instructionTable[0x5E] = new Instruction("ld e, (hl)", 1, 8, 0, _ => _regs.E = _bus.ReadByte(_regs.HL));
            _instructionTable[0x5F] = new Instruction("ld e, a", 1, 8, 0, _ => _regs.E = _regs.A);

            // ld h,r8
            _instructionTable[0x60] = new Instruction("ld h, b", 1, 8, 0, _ => _regs.H = _regs.B);
            _instructionTable[0x61] = new Instruction("ld h, c", 1, 8, 0, _ => _regs.H = _regs.C);
            _instructionTable[0x62] = new Instruction("ld h, d", 1, 8, 0, _ => _regs.H = _regs.D);
            _instructionTable[0x63] = new Instruction("ld h, e", 1, 8, 0, _ => _regs.H = _regs.E);
            _instructionTable[0x64] = new Instruction("ld h, h", 1, 8, 0, _ => {});
            _instructionTable[0x65] = new Instruction("ld h, l", 1, 8, 0, _ => _regs.H = _regs.L);
            _instructionTable[0x66] = new Instruction("ld h, (hl)", 1, 8, 0, _ => _regs.H = _bus.ReadByte(_regs.HL));
            _instructionTable[0x67] = new Instruction("ld h, a", 1, 8, 0, _ => _regs.H = _regs.A);

            // ld l,r8
            _instructionTable[0x68] = new Instruction("ld l, b", 1, 8, 0, _ => _regs.L = _regs.B);
            _instructionTable[0x69] = new Instruction("ld l, c", 1, 8, 0, _ => _regs.L = _regs.C);
            _instructionTable[0x6A] = new Instruction("ld l, d", 1, 8, 0, _ => _regs.L = _regs.D);
            _instructionTable[0x6B] = new Instruction("ld l, e", 1, 8, 0, _ => _regs.L = _regs.E);
            _instructionTable[0x6C] = new Instruction("ld l, h", 1, 8, 0, _ => _regs.L = _regs.H);
            _instructionTable[0x6D] = new Instruction("ld l, l", 1, 8, 0, _ => {});
            _instructionTable[0x6E] = new Instruction("ld l, (hl)", 1, 8, 0, _ => _regs.L = _bus.ReadByte(_regs.HL));
            _instructionTable[0x6F] = new Instruction("ld l, a", 1, 8, 0, _ => _regs.L = _regs.A);

            // ld (hl),r8
            _instructionTable[0x70] = new Instruction("ld (hl), b", 1, 8, 0, _ => _bus.WriteByte(_regs.HL, _regs.B));
            _instructionTable[0x71] = new Instruction("ld (hl), c", 1, 8, 0, _ => _bus.WriteByte(_regs.HL, _regs.C));
            _instructionTable[0x72] = new Instruction("ld (hl), d", 1, 8, 0, _ => _bus.WriteByte(_regs.HL, _regs.D));
            _instructionTable[0x73] = new Instruction("ld (hl), e", 1, 8, 0, _ => _bus.WriteByte(_regs.HL, _regs.E));
            _instructionTable[0x74] = new Instruction("ld (hl), h", 1, 8, 0, _ => _bus.WriteByte(_regs.HL, _regs.H));
            _instructionTable[0x75] = new Instruction("ld (hl), l", 1, 8, 0, _ => _bus.WriteByte(_regs.HL, _regs.L));
            _instructionTable[0x77] = new Instruction("ld (hl), a", 1, 8, 0, _ => _bus.WriteByte(_regs.HL, _regs.A));

            // ld a,r8
            _instructionTable[0x78] = new Instruction("ld a, b", 1, 8, 0, _ => _regs.A = _regs.B);
            _instructionTable[0x79] = new Instruction("ld a, c", 1, 8, 0, _ => _regs.A = _regs.C);
            _instructionTable[0x7A] = new Instruction("ld a, d", 1, 8, 0, _ => _regs.A = _regs.D);
            _instructionTable[0x7B] = new Instruction("ld a, e", 1, 8, 0, _ => _regs.A = _regs.E);
            _instructionTable[0x7C] = new Instruction("ld a, h", 1, 8, 0, _ => _regs.A = _regs.H);
            _instructionTable[0x7D] = new Instruction("ld a, l", 1, 8, 0, _ => _regs.A = _regs.L);
            _instructionTable[0x7E] = new Instruction("ld a, (hl)", 1, 8, 0, _ => _regs.A = _bus.ReadByte(_regs.HL));
            _instructionTable[0x7F] = new Instruction("ld a, a", 1, 8, 0, _ => {});

            ///// END LD R8,R8 /////
            

            // rst XXH instructions
            _instructionTable[0xC7] = new Instruction("rst 00H", 1, 16, 0, _ => { RST__tgt3(0); });
            _instructionTable[0xCF] = new Instruction("rst 08H", 1, 16, 0, _ => { RST__tgt3(1); });
            _instructionTable[0xD7] = new Instruction("rst 10H", 1, 16, 0, _ => { RST__tgt3(2); });
            _instructionTable[0xDF] = new Instruction("rst 18H", 1, 16, 0, _ => { RST__tgt3(3); });
            _instructionTable[0xE7] = new Instruction("rst 20H", 1, 16, 0, _ => { RST__tgt3(4); });
            _instructionTable[0xEF] = new Instruction("rst 28H", 1, 16, 0, _ => { RST__tgt3(5); });
            _instructionTable[0xF7] = new Instruction("rst 30H", 1, 16, 0, _ => { RST__tgt3(6); });
            _instructionTable[0xFF] = new Instruction("rst 38H", 1, 16, 0, _ => { RST__tgt3(7); });

            // logical operations
            _instructionTable[0xAF] = new Instruction("xor a,a", 1, 4, 0, _ => { XOR(ref _regs.A, _regs.A); });

            // interupt enable/disable
            _instructionTable[0xF3] = new Instruction("di", 1, 4, 0, _ => { _ime = false; });
            _instructionTable[0xFB] = new Instruction("ei", 1, 4, 0, _ => { _ime = true; });

            _instructionTable[0xC3] = new Instruction("jp imm16", 3, 16, 0, ibytes => {
                _regs.PC = (ushort)(ibytes[2] << 8 | ibytes[1]);
            });
            _instructionTable[0xC9] = new Instruction("ret", 1, 16, 0, _ => { _regs.PC = PopWord(); });
        }

        public void Reset()
        {
            // default values found here: http://www.codeslinger.co.uk/pages/projects/gameboy/hardware.html
            _regs.PC = 0x0100;
            _regs.SP = 0xFFFE;
            _regs.AF = 0x01B0;
            _regs.BC = 0x0013;
            _regs.DE = 0x00D8;
            _regs.HL = 0x00D8;

            _ime = false; // disable maskable interupts
            _mode = CPUMode.Active;

            // ignoring these values for now since we don't have an IO or IR mapped
            // _bus.WriteByte(0xFF05, 0x00);
            // _bus.WriteByte(0xFF06, 0x00);
            // _bus.WriteByte(0xFF07, 0x00);
            // _bus.WriteByte(0xFF10, 0x80);
            // _bus.WriteByte(0xFF11, 0xBF);
            // _bus.WriteByte(0xFF12, 0xF3);
            // _bus.WriteByte(0xFF14, 0xBF);
            // _bus.WriteByte(0xFF16, 0x3F);
            // _bus.WriteByte(0xFF17, 0x00);
            // _bus.WriteByte(0xFF19, 0xBF);
            // _bus.WriteByte(0xFF1A, 0x7F);
            // _bus.WriteByte(0xFF1B, 0xFF);
            // _bus.WriteByte(0xFF1C, 0x9F);
            // _bus.WriteByte(0xFF1E, 0xBF);
            // _bus.WriteByte(0xFF20, 0xFF);
            // _bus.WriteByte(0xFF21, 0x00);
            // _bus.WriteByte(0xFF22, 0x00);
            // _bus.WriteByte(0xFF23, 0xBF);
            // _bus.WriteByte(0xFF24, 0x77);
            // _bus.WriteByte(0xFF25, 0xF3);
            // _bus.WriteByte(0xFF26, 0xF1);
            // _bus.WriteByte(0xFF40, 0x91);
            // _bus.WriteByte(0xFF42, 0x00);
            // _bus.WriteByte(0xFF43, 0x00);
            // _bus.WriteByte(0xFF45, 0x00);
            // _bus.WriteByte(0xFF47, 0xFC);
            // _bus.WriteByte(0xFF48, 0xFF);
            // _bus.WriteByte(0xFF49, 0xFF);
            // _bus.WriteByte(0xFF4A, 0x00);
            // _bus.WriteByte(0xFF4B, 0x00);
            // _bus.WriteByte(0xFFFF, 0x00);
        }

        public void Step()
        {
            ushort instructionStart = _regs.PC;
            byte opcode = _bus.ReadByte(_regs.PC);
            Instruction instruction = _instructionTable[opcode];

            byte[] instructionBytes = new byte[instruction.Length];
            for(int i = 0; i < instructionBytes.Length; i++)
            {
                // read instruction bytes and increase pc
                instructionBytes[i] = _bus.ReadByte(_regs.PC++);
            }

            string formattedInstructionBytes = string.Join(" ", instructionBytes.Select(b => b.ToString("X2")));
            Console.WriteLine($"{instructionStart:X4}: {formattedInstructionBytes} ({instruction.Mnemonic})");

            try 
            {
                instruction.Handler(instructionBytes);
                _ranInstructions++;
            }
            catch(Exception e)
            {
                _regs.DumpRegisterValues();
                Console.WriteLine($"Ran {_ranInstructions} instructions");
                throw;
            }
        }

        public void BeginExecution()
        {
            Reset();

            while(_mode == CPUMode.Active)
                Step();
        }

        private void PushWord(ushort word)
        {
            _bus.WriteByte(--_regs.SP, (byte)(word >> 8));
            _bus.WriteByte(--_regs.SP, (byte)word);
        }

        private ushort PopWord()
        {
            return (ushort)(_bus.ReadByte(_regs.SP++) | _bus.ReadByte(_regs.SP++) << 8);
        }

        private void INC__r8(ref byte register)
        {
            bool halfCarry = ((register & 0xF) + (1 & 0xF)) > 0xF;
            byte result = (byte)(register + 1);

            _regs.SetFlag(CPUFlags.H, halfCarry);
            _regs.SetFlag(CPUFlags.N, false);
            _regs.SetFlag(CPUFlags.Z, result == 0);

            register = result;
        }

        private void DEC__r8(ref byte register)
        {
            bool halfCarry = ((register & 0xF) - (1 & 0xF)) < 0;
            byte result = (byte)(register - 1);

            _regs.SetFlag(CPUFlags.H, halfCarry);
            _regs.SetFlag(CPUFlags.N, true);
            _regs.SetFlag(CPUFlags.Z, result == 0);

            register = result;
        }

        private void RST__tgt3(byte tgt3)
        {
            PushWord(_regs.PC);
            _regs.PC = (ushort)(tgt3 * 8);
        }

        private void XOR(ref byte register, byte value)
        {
            register ^= value;

            _regs.ClearFlags();
            _regs.SetFlag(CPUFlags.Z, register == 0);
        }

        private void UnimplementedInstruction(byte[] ibytes)
        {
            // Little hack: In this CPU implementation PC is incremented past the executed instruction
            // before the instruction is executed. That means that when this instruction is called,
            // PC will already be pointing to the bytes of the next instruction to be executed. Because
            // of this, we decrement PC by 4 before dumping register values so that the value of PC
            // accurately reflects where execution halted.
            _regs.PC -= 4;
            throw new NotImplementedException($"Opcode 0x{ibytes[0]:X} has not been implemented yet.");
        }
    }
}