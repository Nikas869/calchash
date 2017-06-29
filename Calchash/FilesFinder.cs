using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Calchash
{
    internal class FilesFinder
    {
        private readonly DirectoryInfo inputDirectory;

        public FilesFinder(DirectoryInfo inputDirectory)
        {
            this.inputDirectory = inputDirectory;
        }

        public IEnumerable<FileInfo> GetAllFiles()
        {
            var files = new ConcurrentBag<FileInfo>();

            GatherFilesInformation(inputDirectory, files);

            return files;
        }

        private void GatherFilesInformation(DirectoryInfo directoryInfo, ConcurrentBag<FileInfo> filesList)
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