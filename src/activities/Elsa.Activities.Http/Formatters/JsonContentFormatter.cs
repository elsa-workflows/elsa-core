using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Http.Formatters
{
    public class JsonContentFormatter : IContentFormatter
    {
        public int Priority => 0;
        public IEnumerable<string> SupportedContentTypes => new[] { "application/json", "text/json" };

        public Task<object> FormatAsync(string content, string contentType)
        {
            return Task.FromResult<object>(JToken.Parse(content));
        }
    }
}