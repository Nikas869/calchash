using System;
using System.Collections.Concurrent;
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
            if (CheckArgs(args) == false)
            {
                Environment.Exit(0);
            }

            currentDirectoryInfo = new DirectoryInfo(args[0]);
            currentFileInfo = new FileInfo(args[1]);

            var filesList = new ConcurrentBag<FileInfo>();
            GatherFilesInformation(currentDirectoryInfo, filesList);

            long elapsedTime;
            var filesHash = CalculateHash(filesList, out elapsedTime);

            WriteResult(currentFileInfo, filesHash, elapsedTime);
        }

        private static void WriteResult(FileInfo fileInfo, ConcurrentDictionary<string, FileInfoStruct> filesHash, long elapsedTime)
        {
            using (StreamWriter sw = new StreamWriter(fileInfo.OpenWrite()))
            {
                long filesSize = 0;

                foreach (var hash in filesHash)
                {
                    sw.WriteLine($"{hash.Key} {hash.Value.Path}");
                    filesSize += hash.Value.Size;
                }

                sw.WriteLine($"Performance: {filesSize / 1000 / elapsedTime} MB/s (by CPU time)");
            }
        }

        private static ConcurrentDictionary<string, FileInfoStruct> CalculateHash(ConcurrentBag<FileInfo> filesList, out long elapsedTime)
        {
            var filesHash = new ConcurrentDictionary<string, FileInfoStruct>();
            var sha = new SHA256Managed();
            long elapsedTimeTemp = 0;
            var lockObject = new object();

            Parallel.ForEach(
                filesList,
                () => 0L,
                (fileInfo, loopState, partialElapsedTime) =>
                {
                    var sw = new ExecutionStopwatch();
                    sw.Start();
                    using (FileStream stream = fileInfo.OpenRead())
                    {
                        byte[] hash;
                        lock (lockObject)
                        {
                            hash = sha.ComputeHash(stream);
                        }
                        filesHash.GetOrAdd(BitConverter.ToString(hash).Replace("-", String.Empty),
                            new FileInfoStruct(fileInfo.FullName, fileInfo.Length));
                    }
                    sw.Stop();
                    partialElapsedTime += sw.Elapsed;

                    return partialElapsedTime;
                },
                partialElapsedTime => { Interlocked.Add(ref elapsedTimeTemp, partialElapsedTime); });

            elapsedTime = elapsedTimeTemp;

            return filesHash;
        }

        private static void GatherFilesInformation(DirectoryInfo directoryInfo, ConcurrentBag<FileInfo> filesList)
        {
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                filesList.Add(fileInfo);
            }

            Parallel.ForEach(
                directoryInfo.GetDirectories(),
                directory =>
                {
                    try
                    {
                        GatherFilesInformation(directory, filesList);
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                });
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
                    stream.SetLength(0);
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