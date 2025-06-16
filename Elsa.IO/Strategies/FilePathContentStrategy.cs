using Elsa.IO.Contracts;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.IO.Strategies
{
    /// <summary>
    /// Handles file path content.
    /// </summary>
    public class FilePathContentStrategy : IContentStrategy
    {
        public bool CanHandle(object content)
        {
            return content is string str && File.Exists(str);
        }

        public async Task<Stream> ParseContentAsync(object content, CancellationToken cancellationToken)
        {
            var path = content as string;
            return File.OpenRead(path);
        }
    }
}

