namespace Emulator
{
    public class OAMDevice : IMemoryDevice
    {
        const uint MEMORY_OFFSET = 0xFE00;
        const uint SIZE = 0xFEA0 - MEMORY_OFFSET;

        public byte[] Data { get; } = new byte[SIZE];

        public bool Contains(ushort address)
        {
            return address >= MEMORY_OFFSET && address < MEMORY_OFFSET + SIZE;
        }

        public byte ReadByte(ushort address)
        {
            return Data[address - MEMORY_OFFSET];
        }

        public void WriteByte(ushort address, byte value)
        {
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