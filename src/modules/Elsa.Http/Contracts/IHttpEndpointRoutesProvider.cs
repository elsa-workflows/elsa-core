using Elsa.Http.Contexts;

namespace Elsa.Http.Contracts;

public interface IHttpEndpointRoutesProvider
{
    Task<IEnumerable<string>> GetRoutesAsync(HttpEndpointRouteProviderContext context);
}