﻿namespace Emulator
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
            
            Console.WriteLine($"Loaded ROM '{rom.Title}' ({rom.Size / 1024}KiB)");

            // Create bus and map memory devices
            Bus bus = new Bus();
            bus.MapMemoryDevice(rom);
            bus.MapMemoryDevice(new XRAMDevice());
            bus.MapMemoryDevice(new WRAMDevice());
            bus.MapMemoryDevice(new OAMDevice());
            bus.MapMemoryDevice(new UnusableDevice());
            bus.MapMemoryDevice(new HRAMDevice());
            bus.MapMemoryDevice(new IMEDevice());

            // Create CPU and begin executing instructions
            CPU cpu = new CPU(bus);
            cpu.BeginExecution();
        }
    }
}