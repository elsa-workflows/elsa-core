using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Http.Extensions
{
    public static class StreamExtensions
    {
        public static async Task<byte[]> ReadBytesToEndAsync(this Stream input, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream())
            {
                await input.CopyToAsync(ms, 16 * 1024, cancellationToken);
                return ms.ToArray();
            }
        }
    }
}