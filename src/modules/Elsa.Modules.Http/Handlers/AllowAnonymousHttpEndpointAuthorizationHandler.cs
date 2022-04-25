using System.Threading.Tasks;
using Elsa.Modules.Http.Models;
using Elsa.Modules.Http.Services;

namespace Elsa.Modules.Http.Handlers
{
    public class AllowAnonymousHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
    {
        public ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context) => new(true);
    }
}