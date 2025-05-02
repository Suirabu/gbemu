namespace Emulator
{
    [Flags]
    public enum CPUFlags
    {
        Z = 0x80, // Zero
        N = 0x40, // Subtract
        H = 0x20, // Half Carry
        C = 0x10, // Carry
    }

    public class Registers
    {
        private byte _flags;
        public byte A, B, C, D, E, H, L;
        public byte F
        {
            get => (byte)(_flags & 0xF0);
            set => _flags = (byte)(value & 0xF0);
        }
        public ushort AF
        {
            get => (ushort)(A << 8 | F);
            set
            {
                A = (byte)(value >> 8);
                F = (byte)value;
            }
        }
        public ushort BC
        {
            get => (ushort)(B << 8 | C);
            set
            {
                B = (byte)(value >> 8);
                C = (byte)value;
            }
        }
        public ushort DE
        {
            get => (ushort)(D << 8 | E);
            set
            {
                D = (byte)(value >> 8);
                E = (byte)value;
            }
        }
        public ushort HL
        {
            get => (ushort)(H << 8 | L);
            set
            {
                H = (byte)(value >> 8);
                L = (byte)value;
            }
        }

        public ushort SP;
        public ushort PC;

        public void SetFlag(CPUFlags flag, bool value)
        {
            if (value)
                _flags |= (byte)flag;
            else
                _flags &= (byte)~flag;
        }

        public void ClearFlags()
        {
            _flags = 0;
        }

        public bool GetFlag(CPUFlags flag)
        {
            return (_flags & (byte)flag) != 0;
        }

        public void DumpRegisterValues()
        {
            Console.WriteLine("REGISTER DUMP");
            Console.WriteLine("pc   sp   a  f  b  c  d  e  h  l");
            Console.WriteLine($"{PC:X4} {SP:X4} {A:X2} {F:X2} {B:X2} {C:X2} {D:X2} {E:X2} {H:X2} {L:X2}");
            Console.WriteLine();
            Console.WriteLine("FLAGS DUMP");
            Console.WriteLine("Z N H C");
            Console.WriteLine($"{(GetFlag(CPUFlags.Z) ? 1 : 0)} {(GetFlag(CPUFlags.N) ? 1 : 0)} {(GetFlag(CPUFlags.H) ? 1 : 0)} {(GetFlag(CPUFlags.C) ? 1 : 0)}");
            Console.WriteLine();
        }
    }
}