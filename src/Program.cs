namespace Emulator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load GB ROM from file
            if (args.Length < 1)
                throw new ArgumentException("Expected a path to a GameBoy ROM file.");
        
            string romPath = args[0];
            ROMDevice rom = ROMDevice.FromFile(romPath);

            // Create bus and map memory devices
            Bus bus = new Bus();
            bus.MapMemoryDevice(rom);
            bus.MapMemoryDevice(new HRAMDevice());

            // Create CPU and begin executing instructions
            CPU cpu = new CPU(bus);
            cpu.BeginExecution();
        }
    }
}