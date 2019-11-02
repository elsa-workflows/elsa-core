using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Services
{
    public interface IHttpRequestBodyParser
    {
        int Priority { get; }
        IEnumerable<string> SupportedContentTypes { get; }
        Task<object> ParseAsync(HttpRequest request, CancellationToken cancellationToken);
    }
}