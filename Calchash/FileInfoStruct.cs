namespace Calchash
{
    partial class Program
    {
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