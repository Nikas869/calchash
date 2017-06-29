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
        private readonly ConcurrentBag<FileInfo> files;

        public HashCalculator(ConcurrentBag<FileInfo> files)
        {
            this.files = files;
        }

        public ConcurrentDictionary<string, FileInfoStruct> Calculate(out long elapsedTime)
        {
            var filesHash = new ConcurrentDictionary<string, FileInfoStruct>();
            var sha = new SHA256Managed();
            long elapsedTimeTemp = 0;
            var lockObject = new object();

            Parallel.ForEach(
                files,
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
    }
}