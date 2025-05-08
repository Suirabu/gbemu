namespace Emulator
{
    public class IMEDevice : IMemoryDevice
    {
        const uint MEMORY_OFFSET = 0xFFFF;

        public byte RegisterValue;

        public bool Contains(ushort address)
        {
            return address == MEMORY_OFFSET;
        }

        public byte ReadByte(ushort address)
        {
            return RegisterValue;
        }

        public void WriteByte(ushort address, byte value)
        {
            RegisterValue = value;
        }

        public ref byte GetReferenceToByte(ushort address)
        {
            return ref RegisterValue;
        }

        public bool IsReadOnly()
        {
            return false;
        }
    }
}