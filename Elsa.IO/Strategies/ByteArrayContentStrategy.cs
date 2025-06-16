using Elsa.IO.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.IO.Strategies
{
    /// <summary>
    /// Handles byte array content.
    /// </summary>
    public class ByteArrayContentStrategy : IContentStrategy
    {
        public bool CanHandle(object content)
        {
            return content is byte[];
        }

        public async Task<Stream> ParseContentAsync(object content, CancellationToken cancellationToken)
        {
            var bytes = content as byte[];
            return new MemoryStream(bytes);
        }
    }
}

