namespace Emulator
{ 
    public interface IMemoryDevice
    {
        bool Contains(ushort address);
        byte ReadByte(ushort address);
        void WriteByte(ushort address, byte value);
        ref byte GetReferenceToByte(ushort address);
        bool IsReadOnly();
    }
}