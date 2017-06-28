using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Calchash
{
    class Program
    {
        private static DirectoryInfo currentDirectoryInfo;
        private static FileInfo currentFileInfo;

        static void Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (CheckArgs(args) == false)
            {
                Environment.Exit(0);
            }
            Console.WriteLine(sw.ElapsedTicks);

            currentDirectoryInfo = new DirectoryInfo(args[0]);
            currentFileInfo = new FileInfo(args[1]);

            //if (!AskConfirmation(currentDirectoryInfo, currentFileInfo))
            //{
            //    Console.WriteLine("Aborting");
            //    Environment.Exit(0);
            //}

            var filesList = new List<FileInfo>();
            GatherFilesInformation(currentDirectoryInfo, ref filesList);
            Console.WriteLine(sw.ElapsedTicks);

            var filesHash = CalculateHash(filesList);
            Console.WriteLine(sw.ElapsedTicks);

            WriteResult(currentFileInfo, filesHash);
            Console.WriteLine(sw.ElapsedTicks);
        }

        private static void WriteResult(FileInfo fileInfo, Dictionary<string, string> filesHash)
        {
            using (FileStream stream = fileInfo.OpenWrite())
            {
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    foreach (var hash in filesHash)
                    {
                        sw.WriteLine($"{hash.Value} {hash.Key}");
                    }
                }
            }
        }

        private static Dictionary<string, string> CalculateHash(List<FileInfo> filesList)
        {
            var filesHash = new Dictionary<string, string>();
            var sha = new SHA256Managed();

            foreach (var fileInfo in filesList)
            {
                using (FileStream stream = fileInfo.OpenRead())
                {
                    byte[] checksum = sha.ComputeHash(stream);
                    filesHash.Add(fileInfo.FullName, BitConverter.ToString(checksum).Replace("-", String.Empty));
                }
            }

            return filesHash;
        }

        private static void GatherFilesInformation(DirectoryInfo directoryInfo, ref List<FileInfo> filesList)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                filesList.Add(fileInfo);
            }

            foreach (var directory in directoryInfo.GetDirectories())
            {
                try
                {
                    GatherFilesInformation(directory, ref filesList);
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        private static bool AskConfirmation(DirectoryInfo directoryInfo, FileInfo fileInfo)
        {
            Console.WriteLine($"Directory: {directoryInfo.FullName}");
            Console.WriteLine($"File: {fileInfo.FullName}");
            Console.WriteLine("Continue? (y/n)");

            int answer = Console.Read();

            if (answer == 'y')
            {
                return true;
            }

            return false;
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

            try
            {
                using (var stream = File.OpenWrite(args[1]))
                {
                    return stream.CanWrite;
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: file is read only");
                return false;
            }
        }
    }
}