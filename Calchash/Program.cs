using System;
using System.Collections.Generic;
using System.IO;

namespace Calchash
{
    class Program
    {
        private static DirectoryInfo currentDirectoryInfo;
        private static FileInfo currentFileInfo;

        static void Main(string[] args)
        {
            if (CheckArgs(args) == false)
            {
                Environment.Exit(0);
            }

            currentDirectoryInfo = new DirectoryInfo(args[0]);
            currentFileInfo = new FileInfo(args[1]);

            if (!AskConfirmation(currentDirectoryInfo, currentFileInfo))
            {
                Console.WriteLine("Aborting");
                Environment.Exit(0);
            }

            var filesList = new List<FileInfo>();
            GatherFilesInformation(currentDirectoryInfo, ref filesList);

            foreach (var fileInfo in filesList)
            {
                Console.WriteLine(fileInfo.FullName);
            }
        }

        private static void GatherFilesInformation(DirectoryInfo directoryInfo, ref List<FileInfo> filesList)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                filesList.Add(fileInfo);
            }

            foreach (var directory in directoryInfo.GetDirectories())
            {
                GatherFilesInformation(directory, ref filesList);
            }
        }

        private static bool AskConfirmation(DirectoryInfo directoryInfo, FileInfo fileInfo)
        {
            Console.WriteLine($"Directory: {directoryInfo.FullName}");
            Console.WriteLine($"File: {fileInfo.FullName}");
            Console.WriteLine("Continue? (y/n)");

            int answer = Console.Read();

            if (answer == 121)
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

            if (!File.OpenWrite(args[1]).CanWrite)
            {
                Console.WriteLine("Error: file is read only");
                return false;
            }

            return true;
        }
    }
}