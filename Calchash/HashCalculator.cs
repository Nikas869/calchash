using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Calchash
{
    class HashCalculator
    {
        private readonly IEnumerable<FileInfo> files;

        public HashCalculator(IEnumerable<FileInfo> files)
        {
            this.files = files;
        }

        public IDictionary<string, FileInfoStruct> Calculate(out long elapsedTime)
        {
            var filesHash = new ConcurrentDictionary<string, FileInfoStruct>();
            long elapsedTimeTemp = 0;

            Parallel.ForEach(
                files,
                () => 0L,
                (fileInfo, loopState, partialElapsedTime) =>
                {
                    var sw = new ExecutionStopwatch();
                    sw.Start();

                    using (var stream = fileInfo.OpenRead())
                    {
                        var sha = new SHA256Managed();
                        var hash = sha.ComputeHash(stream);
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