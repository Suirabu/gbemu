using System.ComponentModel;
using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Emulator
{
    enum CPUFlag
    {
        Zero = 1 << 7,
        Sub = 1 << 6,
        HalfCarry = 1 << 5,
        Carry = 1 << 4,
    }

    class RegisterPair
    {
        private ushort _value;
        public ushort Value
        {
            get => _value;
            set => _value = value;
        }

        public byte High
        {
            get => (byte)(_value >> 8);
            set => _value = (ushort)(_value & 0x00FF | value << 8);
        }

        public byte Low
        {
            get => (byte)_value;
            set => _value = (ushort)(_value & 0xFF00 | value);
        }
    }

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

        // register pairs
        private RegisterPair _af = new RegisterPair();
        private RegisterPair _bc = new RegisterPair();
        private RegisterPair _de = new RegisterPair();
        private RegisterPair _hl = new RegisterPair();
        private byte _a
        {
            get => _af.High;
            set => _af.High = value;
        }
        private byte _f
        {
            get => _af.Low;
            set => _af.Low = (byte)(value & 0xF0);
        }
        private byte _b
        {
            get => _bc.High;
            set => _bc.High = value;
        }
        private byte _c
        {
            get => _bc.Low;
            set => _bc.Low = value;
        }
        private byte _d
        {
            get => _de.High;
            set => _de.High = value;
        }
        private byte _e
        {
            get => _de.Low;
            set => _de.Low = value;
        }
        private byte _h
        {
            get => _hl.High;
            set => _hl.High = value;
        }
        private byte _l
        {
            get => _hl.Low;
            set => _hl.Low = value;
        }
        private ushort _pc;
        private ushort _sp = 0xFFFE;

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
                _instructionTable[i] = new Instruction("UNIMPLEMENTED", 4, 4, 0, (ibytes) => UnimplementedInstruction(ibytes));

            _instructionTable[0x00] = new Instruction("nop", 1, 4, 0, (ibytes) => { /* do nothing */ });
            _instructionTable[0x40] = new Instruction("ld b, b", 1, 4, 0, (ibytes) => { _b = _b; });
            _instructionTable[0x47] = new Instruction("ld b, b", 1, 4, 0, (ibytes) => { _b = _a; });
            _instructionTable[0xC0] = new Instruction("ret nz", 1, 20, 8, (ibytes) => {
                if(!GetFlag(CPUFlag.Zero))
                    _pc = PopWord();
            });
            _instructionTable[0xC7] = new Instruction("rsp 00H", 1, 16, 0, (ibytes) => {
                PushWord(_pc);
                byte t = (byte)(ibytes[0] >> 3 & 0x07);
                _pc = (ushort)(t * 8);
            });
            _instructionTable[0xC3] = new Instruction("jp imm16", 3, 16, 0, (ibytes) => {
                _pc = (ushort)(ibytes[1] << 8 | ibytes[2]);
            });
        }

        public void Reset()
        {
            // default values found here: http://www.codeslinger.co.uk/pages/projects/gameboy/hardware.html
            _pc = 0x0100;
            _sp = 0xFFFE;
            _af.Value = 0x01B0;
            _bc.Value = 0x0013;
            _de.Value = 0x00D8;
            _hl.Value = 0x00D8;

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
            ushort instructionStart = _pc;
            byte opcode = _bus.ReadByte(_pc);
            Instruction instruction = _instructionTable[opcode];

            byte[] instructionBytes = new byte[instruction.Length];
            for(int i = 0; i < instructionBytes.Length; i++)
            {
                // read instruction bytes and increase pc
                instructionBytes[i] = _bus.ReadByte(_pc++);
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

        private bool GetFlag(CPUFlag flag)
        {
            return (_f & (byte)flag) != 0;
        }

        private void SetFlag(CPUFlag flag)
        {
            _f |= (byte)flag;
        }

        private void PushWord(ushort word)
        {
            _bus.WriteByte(--_sp, (byte)(word >> 8));
            _bus.WriteByte(--_sp, (byte)word);
        }

        private ushort PopWord()
        {
            return (ushort)(_bus.ReadByte(_sp++) | _bus.ReadByte(_sp++) << 8);
        }

        private ushort GetWord()
        {
            return (ushort)(_bus.ReadByte(_pc++) << 8 | _bus.ReadByte(_pc++));
        }

        private void UnimplementedInstruction(byte[] ibytes)
        {
            throw new NotImplementedException($"Opcode 0x{ibytes[0]:X} has not been implemented yet.");
        }
    }
}