namespace Emulator
{
    public class Bus
    {
        private List<IMemoryDevice> _devices = new List<IMemoryDevice>();

        // Right now there is nothing stopping us from mapping devices with overlapping address ranges
        // This shouldn't be an issue for now since everything we're adding has well-defined non-overlapping
        // ranges.
        public void MapMemoryDevice(IMemoryDevice device)
        {
            _devices.Add(device);
        }

        public byte ReadByte(ushort address)
        {
            IMemoryDevice device = GetDeviceWithAddress(address);
            return device.ReadByte(address);
        }

        void WriteByte(ushort address, byte value)
        {
            IMemoryDevice device = GetDeviceWithAddress(address);
            device.WriteByte(address, value);
        }

        private IMemoryDevice GetDeviceWithAddress(ushort address)
        {
            foreach(var device in _devices)
            {
                if(device.Contains(address))
                    return device;
            }

            throw new InvalidOperationException($"No device mapped at address 0x{address:X4}");
        }
    }
}