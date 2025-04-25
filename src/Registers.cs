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

        public bool GetFlag(CPUFlags flag)
        {
            return (_flags & (byte)flag) != 0;
        }
    }
}