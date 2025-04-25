using System.ComponentModel;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

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
            _instructionTable[0x24] = new Instruction("inc h", 1, 4, 0, _ => { _regs.H = INC__r8(_regs.H); });
            _instructionTable[0x40] = new Instruction("ld b, b", 1, 4, 0, _ => { _regs.B = _regs.B; });
            _instructionTable[0x47] = new Instruction("ld b, b", 1, 4, 0, _ => { _regs.B = _regs.A; });
            _instructionTable[0xC0] = new Instruction("ret nz", 1, 20, 8, _ => {
                if(!_regs.GetFlag(CPUFlags.Z))
                    _regs.PC = PopWord();
            });
            _instructionTable[0xC7] = new Instruction("rst 00H", 1, 16, 0, ibytes => {
                PushWord(_regs.PC);
                byte t = (byte)(ibytes[0] >> 3 & 0x07);
                _regs.PC = (ushort)(t * 8);
            });
            _instructionTable[0xC3] = new Instruction("jp imm16", 3, 16, 0, ibytes => {
                _regs.PC = (ushort)(ibytes[1] << 8 | ibytes[2]);
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

        private byte INC__r8(byte register)
        {
            bool halfCarry = ((register & 0xF) + (1 & 0xF)) > 0xF;
            byte result = (byte)(register + 1);

            _regs.SetFlag(CPUFlags.H, halfCarry);
            _regs.SetFlag(CPUFlags.N, false);
            _regs.SetFlag(CPUFlags.Z, result == 0);

            return result;
        }

        private void UnimplementedInstruction(byte[] ibytes)
        {
            throw new NotImplementedException($"Opcode 0x{ibytes[0]:X} has not been implemented yet.");
        }
    }
}