using Elsa.Http.Contexts;

namespace Elsa.Http;

public interface IHttpEndpointRoutesProvider
{
    Task<IEnumerable<HttpRouteData>> GetRoutesAsync(HttpEndpointRouteProviderContext context);
}