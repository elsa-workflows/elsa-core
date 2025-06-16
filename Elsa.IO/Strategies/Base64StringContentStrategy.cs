using Elsa.IO.Contracts;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.IO.Strategies
{
    /// <summary>
    /// Handles Base64-encoded string content.
    /// </summary>
    public class Base64StringContentStrategy : IContentStrategy
    {
        public bool CanHandle(object content)
        {
            return content is string str && IsBase64String(str);
        }

        public async Task<Stream> ParseContentAsync(object content, CancellationToken cancellationToken)
        {
            var base64 = content as string;
            var bytes = Convert.FromBase64String(base64);
            return new MemoryStream(bytes);
        }

        private bool IsBase64String(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;
            Span<byte> buffer = new Span<byte>(new byte[str.Length]);
            return Convert.TryFromBase64String(str, buffer, out _);
        }
    }
}

