using Elsa.Http.Contracts;
using Elsa.Http.Models;

namespace Elsa.Http.Handlers;

public class AllowAnonymousHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
{
    public ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context) => new(true);
}