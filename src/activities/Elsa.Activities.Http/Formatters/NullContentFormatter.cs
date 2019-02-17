using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.Http.Formatters
{
    public class NullContentFormatter : IContentFormatter
    {
        public int Priority => -1;
        public IEnumerable<string> SupportedContentTypes => new[] { "", default(string) };

        public Task<object> FormatAsync(string content, string contentType)
        {
            return null;
        }
    }
}