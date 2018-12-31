using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Elsa.Persistence.FileSystem.Extensions
{
    public static class FileInfoBaseExtensions
    {
        public static Task<string> ReadStringToEndAsync(this FileInfoBase fileInfo)
        {
            using (var reader = new StreamReader(fileInfo.OpenRead()))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                return reader.ReadToEndAsync();
            }
        }
    }
}