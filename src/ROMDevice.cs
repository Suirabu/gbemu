using System.Text;

namespace Emulator
{
    public class ROMDevice : IMemoryDevice
    {
        public byte[] Data { get; }
        public string Title { get; }
        public bool Loaded { get; }
        public uint Size { get; }

        public ROMDevice(byte[] data, string title, uint size, bool loaded = true)
        {
            Data = data;
            Title = title;
            Loaded = loaded;
        }

        public static ROMDevice FromFile(string path)
        {
            if(!File.Exists(path))
                throw new FileNotFoundException($"ROM file '{path}' not found.");

            byte[] data = File.ReadAllBytes(path);

            if(!ROMDevice.HeaderIsValid(data))
                throw new InvalidDataException("Invalid ROM header.");

            // get rom title
            string title = Encoding.ASCII.GetString(data, 0x0134, 16);

            // get rom size
            uint size = (uint)(0x8000 * (data[0x0148] << 1)); // 32KiB * 2^banks

            return new ROMDevice(data, title, size);
        }

        private static bool HeaderIsValid(byte[] data)
        {
            // validate logo bytes
            byte[] expectedLogoBytes = {
                0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
                0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
                0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E,
            };

            if(!data.Skip(0x104).Take(48).SequenceEqual(expectedLogoBytes))
                return false;
            
            // validate header checksum
            byte[] headerBytes = data.Skip(0x0134).Take(25).ToArray();
            byte expectedChecksum = data[0x014D];

            int checksum = 0;
            foreach(byte headerByte in headerBytes)
                checksum = checksum - headerByte - 1;

            byte finalChecksum = (byte)(checksum & 0xFF);

            if(finalChecksum != expectedChecksum)
                return false;

            return true;
        }

        public bool Contains(ushort address)
        {
            // 0000-3FFF: ROM bank 00
            // 4000-7FFF: ROM bank 01-NN (switchable)
            return address >= 0x0000 && address < 0x8000;
        }

        public byte ReadByte(ushort address)
        {
            return Data[address];
        }

        public void WriteByte(ushort address, byte value)
        {
            throw new InvalidOperationException("Cannot write to ROM; range is read-only.");
        }

        public bool IsReadOnly()
        {
            return true;
        }
    }
}