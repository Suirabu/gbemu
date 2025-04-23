using System.Net.NetworkInformation;
using System.Text;

namespace Emulator
{
    public class ROM
    {
        public byte[] Data { get; }
        public string Title { get; }

        public ROM(byte[] data, string title)
        {
            Data = data;
            Title = title;
        }

        public static ROM FromFile(string path)
        {
            if(!File.Exists(path))
                throw new FileNotFoundException($"ROM file '{path}' not found.");

            byte[] data = File.ReadAllBytes(path);

            if(!ROM.HeaderIsValid(data))
                throw new InvalidDataException("Invalid ROM header.");

            string title = Encoding.ASCII.GetString(data, 0x0134, 16);

            return new ROM(data, title);
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
    }
}