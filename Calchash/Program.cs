using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

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

            currentDirectoryInfo = new DirectoryInfo(args[0]);
            currentFileInfo = new FileInfo(args[1]);
            
            if (!AskConfirmation(currentDirectoryInfo, currentFileInfo))
            {
                Console.WriteLine("Aborting");
                Environment.Exit(0);
            }

            var filesList = new List<FileInfo>();
            GatherFilesInformation(currentDirectoryInfo, ref filesList);

            Console.WriteLine(sw.ElapsedMilliseconds / 1000);

            long elapsedTime;
            var filesHash = CalculateHash(filesList, out elapsedTime);

            Console.WriteLine(sw.ElapsedMilliseconds / 1000);

            WriteResult(currentFileInfo, filesHash, elapsedTime);

            Console.WriteLine(sw.ElapsedMilliseconds / 1000);
        }

        private static void WriteResult(FileInfo fileInfo, ConcurrentDictionary<string, FileInfoStruct> filesHash, long elapsedTime)
        {
            using (StreamWriter sw = new StreamWriter(fileInfo.OpenWrite()))
            {
                long filesSize = 0;
                object lockObject = new object();

                Parallel.ForEach(
                    filesHash,
                    () => 0L,
                    (hash, loopState, partialFileSize) =>
                    {
                        lock (lockObject)
                        {
                            sw.WriteLine($"{hash.Key} {hash.Value.Path}");
                        }
                        partialFileSize += hash.Value.Size;

                        return partialFileSize;
                    },
                    partialFileSize => { Interlocked.Add(ref filesSize, partialFileSize); });

                sw.WriteLine($"Performance: {filesSize / 1000 / elapsedTime} MB/s (by CPU time)");
                sw.WriteLine($"Files: {filesHash.Count}, elapsed time: {elapsedTime / 1000}sec, files size: {filesSize / 1000000}MB");
            }
        }

        private static ConcurrentDictionary<string, FileInfoStruct> CalculateHash(List<FileInfo> filesList, out long elapsedTime)
        {
            var filesHash = new ConcurrentDictionary<string, FileInfoStruct>();
            var sha = new SHA256Managed();
            long elapsedTimeTemp = 0;

            Parallel.ForEach(
                filesList,
                () => 0L,
                (fileInfo, loopState, partialElapsedTime) =>
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    using (FileStream stream = fileInfo.OpenRead())
                    {
                        byte[] checksum = sha.ComputeHash(stream);
                        filesHash.GetOrAdd(BitConverter.ToString(checksum).Replace("-", String.Empty),
                            new FileInfoStruct(fileInfo.FullName, fileInfo.Length));
                    }
                    partialElapsedTime += sw.ElapsedMilliseconds;

                    return partialElapsedTime;
                },
                partialElapsedTime => { Interlocked.Add(ref elapsedTimeTemp, partialElapsedTime); });

            elapsedTime = elapsedTimeTemp;

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

        struct FileInfoStruct
        {
            public string Path { get; }
            public long Size { get; }

            public FileInfoStruct(string path, long size)
            {
                Path = path;
                Size = size;
            }
        }
    }
}