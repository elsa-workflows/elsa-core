using Elsa.Http.Models;

namespace Elsa.Http.Contracts;

public interface IHttpEndpointAuthorizationHandler
{
    ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context);
}