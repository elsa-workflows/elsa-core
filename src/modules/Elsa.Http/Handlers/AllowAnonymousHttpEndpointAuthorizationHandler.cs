using Elsa.Http.Models;
using Elsa.Http.Services;

namespace Elsa.Http.Handlers;

public class AllowAnonymousHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
{
    public ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context) => new(true);
}