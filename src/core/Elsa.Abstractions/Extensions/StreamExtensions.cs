using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa
{
    public static class StreamExtensions
    {
        public static async Task<byte[]> ReadBytesToEndAsync(this Stream input, CancellationToken cancellationToken = default)
        {
            await using var ms = new MemoryStream();
            await input.CopyToAsync(ms, 16 * 1024, cancellationToken);
            return ms.ToArray();
        }
        
        public static async Task<string> ReadStringToEndAsync(this Stream input, CancellationToken cancellationToken = default)
        {
            var bytes = await input.ReadBytesToEndAsync(cancellationToken);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}