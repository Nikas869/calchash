using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Calchash
{
    class HashCalculator
    {
        public DirectoryInfo DirectoryInfo { get; }
        public FileInfo FileInfo { get; }

        public HashCalculator(DirectoryInfo directoryInfo, FileInfo fileInfo)
        {
            DirectoryInfo = directoryInfo;
            FileInfo = fileInfo;
        }


        public void Calculate()
        {
            var filesList = new ConcurrentBag<FileInfo>();
            GatherFilesInformation(DirectoryInfo, filesList);

            var filesHash = CalculateHash(filesList, out long elapsedTime);

            WriteResult(FileInfo, filesHash, elapsedTime);
        }

        private static void WriteResult(
            FileInfo fileInfo,
            ConcurrentDictionary<string, FileInfoStruct> filesHash,
            long elapsedTime)
        {
            try
            {
                using (var sw = new StreamWriter(fileInfo.OpenWrite()))
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
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static ConcurrentDictionary<string, FileInfoStruct> CalculateHash(
            ConcurrentBag<FileInfo> filesList,
            out long elapsedTime)
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

                    using (var stream = fileInfo.OpenRead())
                    {
                        byte[] hash;
                        lock (lockObject)
                        {
                            hash = sha.ComputeHash(stream);
                        }
                        filesHash.GetOrAdd(BitConverter.ToString(hash).Replace("-", string.Empty),
                            new FileInfoStruct(fileInfo.FullName, fileInfo.Length));
                    }

                    sw.Stop();

                    return sw.Elapsed;
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
    }
}