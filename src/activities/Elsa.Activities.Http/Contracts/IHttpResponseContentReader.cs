using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Http.Contracts
{
    public interface IHttpResponseContentReader
    {
        string Name { get; }
        int Priority { get; }
        bool GetSupportsContentType(string contentType);
        Task<object> ReadAsync(HttpResponseMessage response, object context, CancellationToken cancellationToken);
    }
}