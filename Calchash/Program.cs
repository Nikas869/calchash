using System;
using System.IO;

namespace Calchash
{
    class Program
    {
        static void Main(string[] args)
        {
            if (CheckArgs(args) == false)
            {
                Environment.Exit(0);
            }

            var currentDirectoryInfo = new DirectoryInfo(args[0]);
            var currentFileInfo = new FileInfo(args[1]);

            var hashCalc = new HashCalculator(currentDirectoryInfo, currentFileInfo);
            hashCalc.Calculate();
        }

        
        private static bool CheckArgs(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Arguments error");
                return false;
            }

            if (!Directory.Exists(args[0]))
            {
                Console.WriteLine("Error: folder doesn`t exists");
                return false;
            }

            return TryWriteToFile(args);
        }

        private static bool TryWriteToFile(string[] args)
        {
            try
            {
                using (var stream = File.OpenWrite(args[1]))
                {
                    stream.SetLength(0);
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: result file is read only");
                return false;
            }
        }
    }
}