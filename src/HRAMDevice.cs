namespace Emulator
{
    public class HRAMDevice : IMemoryDevice
    {
        const uint MEMORY_OFFSET = 0xFF80;
        const uint SIZE = 0xFFFF - MEMORY_OFFSET;

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