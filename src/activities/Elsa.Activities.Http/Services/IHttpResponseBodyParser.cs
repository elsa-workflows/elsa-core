using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Http.Services
{
    public interface IHttpResponseBodyParser
    {
        int Priority { get; }
        IEnumerable<string> SupportedContentTypes { get; }
        Task<object> ParseAsync(HttpResponseMessage response, CancellationToken cancellationToken);
    }
}