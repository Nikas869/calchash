using System;
using System.Collections.Concurrent;
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

            var inputDirectory = new DirectoryInfo(args[0]);
            var outputFile = new FileInfo(args[1]);

            var filesFinder = new FilesFinder(inputDirectory);

            var hashCalc = new HashCalculator(filesFinder.GetAllFiles());
            long elapsedCpuTime;
            var hashes = hashCalc.Calculate(out elapsedCpuTime);

            var fileWriter = new FileWriter(outputFile);
            fileWriter.Write(hashes, elapsedCpuTime);
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