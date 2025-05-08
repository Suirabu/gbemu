namespace Emulator
{
    public class UnusableDevice : IMemoryDevice
    {
        const uint MEMORY_OFFSET = 0xFEA0;
        const uint SIZE = 0xFF00 - MEMORY_OFFSET;

        public byte[] Data { get; } = new byte[SIZE];

        public bool Contains(ushort address)
        {
            return address >= MEMORY_OFFSET && address < MEMORY_OFFSET + SIZE;
        }

        public byte ReadByte(ushort address)
        {
            // On DMG, MGB, SGB, and SGB2, reads during OAM block trigger OAM corruption. Reads otherwise return $00.
            // https://gbdev.io/pandocs/Memory_Map.html#fea0feff-range
            return 0;
        }

        public void WriteByte(ushort address, byte value)
        {
            // Not sure if this is useful for anything
            Data[address - MEMORY_OFFSET] = value;
        }

        public ref byte GetReferenceToByte(ushort address)
        {
            return ref Data[address - MEMORY_OFFSET];
        }

        public bool IsReadOnly()
        {
            return false;
        }
    }
}