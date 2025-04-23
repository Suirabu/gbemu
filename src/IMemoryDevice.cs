namespace Emulator
{ 
    public interface IMemoryDevice
    {
        bool Contains(ushort address);
        byte ReadByte(ushort address);
        void WriteByte(ushort address, byte value);
        bool IsReadOnly();
    }
}