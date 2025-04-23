using System.Dynamic;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.Intrinsics.X86;

namespace Emulator
{
    enum CPUFlags
    {
        Zero = 1 << 7,
        Sub = 1 << 6,
        HalfCarry = 1 << 5,
        Carry = 1 << 4,
    }

    class RegisterPair
    {
        public ushort Value
        {
            get => Value;
            set => Value = value;
        }

        public byte High
        {
            get => (byte)(Value >> 8);
            set => Value = (ushort)(Value & 0x00FF | value << 8);
        }

        public byte Low
        {
            get => (byte)Value;
            set => Value = (ushort)(Value & 0xFF00 | value);
        }
    }

    public class CPU
    {
        private readonly Bus _bus;

        // register pairs
        private RegisterPair _af = new RegisterPair();
        private RegisterPair _bc = new RegisterPair();
        private RegisterPair _de = new RegisterPair();
        private RegisterPair _hl = new RegisterPair();
        private byte a
        {
            get => _af.High;
            set => _af.High = value;
        }
        private byte f
        {
            get => _af.Low;
            set => _af.Low = (byte)(value & 0xF0);
        }
        private byte b
        {
            get => _bc.High;
            set => _bc.High = value;
        }
        private byte c
        {
            get => _bc.Low;
            set => _bc.Low = value;
        }
        private byte d
        {
            get => _de.High;
            set => _de.High = value;
        }
        private byte e
        {
            get => _de.Low;
            set => _de.Low = value;
        }
        private byte h
        {
            get => _hl.High;
            set => _hl.High = value;
        }
        private byte l
        {
            get => _hl.Low;
            set => _hl.Low = value;
        }
        private ushort _pc;
        private ushort _sp;

        public CPU(Bus bus)
        {
            _bus = bus;
        }

        public void Reset()
        {
            _pc = 0x0100;
        }

        public void Step()
        {
            var opcode = _bus.ReadByte(_pc++);

            Console.WriteLine($"{_pc - 1:X4}: 0x{opcode:X2}");

            // nop
            if(opcode == 0)
                return;
            // jp imm16
            else if(opcode == 0xC3)
                _pc = (ushort)(_bus.ReadByte(_pc++) << 8 | _bus.ReadByte(_pc++));
            else
                throw new NotImplementedException($"Opcode 0x{opcode:X2} has not been implemented yet.");

        }

        public void BeginExecution()
        {
            Reset();

            while(true)
                Step();
        }
    }
}