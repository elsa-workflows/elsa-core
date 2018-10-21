using System.IO;
using System.Threading.Tasks;
using OrchardCore.FileStorage;

namespace Flowsharp.Web.Persistence.FileSystem.Extensions
{
    public static class FileStoreExtensions
    {
        public static async Task<string> ReadToEndAsync(this IFileStore fileStore, string path)
        {
            using (var stream = await fileStore.GetFileStreamAsync(path))
            using(var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}