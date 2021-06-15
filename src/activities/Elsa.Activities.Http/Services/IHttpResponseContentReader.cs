using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Http.Services
{
    public interface IHttpResponseContentReader
    {
        int Priority { get; }
        bool GetSupportsContentType(string contentType);
        Task<object> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken);
    }
}