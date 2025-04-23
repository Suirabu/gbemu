namespace Emulator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
                throw new ArgumentException("Expected a path to a GameBoy ROM file.");
        
            var romPath = args[0];
            ROM rom = ROM.FromFile(romPath);
        }
    }
}