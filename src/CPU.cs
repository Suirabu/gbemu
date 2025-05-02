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

    public class CPU
    {
        private readonly Bus _bus;
        private readonly Instruction[] _instructionTable = new Instruction[256];
        private Registers _regs = new Registers();
        private bool _ime = false;

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

            _instructionTable[0x00] = new Instruction("nop", 1, 4, 0, _ => { /* do nothing */ });

            // direct 16-bit register loads
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

            _instructionTable[0x24] = new Instruction("inc h", 1, 4, 0, _ => { INC__r8(ref _regs.H); });
            _instructionTable[0x40] = new Instruction("ld b,b", 1, 4, 0, _ => {});
            _instructionTable[0x47] = new Instruction("ld b,a", 1, 4, 0, _ => { _regs.B = _regs.A; });
            _instructionTable[0x57] = new Instruction("ld d,a", 1, 4, 0, _ => { _regs.D = _regs.A; });
            _instructionTable[0xC0] = new Instruction("ret nz", 1, 20, 8, _ => {
                if(!_regs.GetFlag(CPUFlags.Z))
                    _regs.PC = PopWord();
            });

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
            instruction.Handler(instructionBytes);
        }

        public void BeginExecution()
        {
            Reset();

            while(true)
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

            _regs.DumpRegisterValues();
            throw new NotImplementedException($"Opcode 0x{ibytes[0]:X} has not been implemented yet.");
        }
    }
}