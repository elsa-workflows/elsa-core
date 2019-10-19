using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;

namespace Elsa.Activities.Http.Formatters
{
    public class DefaultContentFormatter : IContentFormatter
    {
        public int Priority => -1;
        public IEnumerable<string> SupportedContentTypes => new[] { "", default };

        public Task<object> ParseAsync(byte[] content, string contentType)
        {
            return Task.FromResult<object>(content);
        }
    }
}