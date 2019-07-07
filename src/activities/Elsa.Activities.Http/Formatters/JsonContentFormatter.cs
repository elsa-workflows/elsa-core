using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using Elsa.Activities.Http.Services;
using Newtonsoft.Json;

namespace Elsa.Activities.Http.Formatters
{
    public class JsonContentFormatter : IContentFormatter
    {
        public int Priority => 0;
        public IEnumerable<string> SupportedContentTypes => new[] { "application/json", "text/json" };

        public Task<object> ParseAsync(string content, string contentType)
        {
            return Task.FromResult<object>(JsonConvert.DeserializeObject<ExpandoObject>(content));
        }
    }
}