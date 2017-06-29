using System;
using System.Collections.Concurrent;
using System.IO;

namespace Calchash
{
    internal class FileWriter
    {
        private readonly FileInfo outputFile;

        public FileWriter(FileInfo outputFile)
        {
            this.outputFile = outputFile;
        }

        public void Write(ConcurrentDictionary<string, FileInfoStruct> filesHashes, long elapsedTime)
        {
            try
            {
                using (var sw = new StreamWriter(outputFile.OpenWrite()))
                {
                    long filesSize = 0;

                    foreach (var hash in filesHashes)
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
    }
}