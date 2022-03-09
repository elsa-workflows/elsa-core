using System.Threading.Tasks;
using Elsa.Modules.Http.Contracts;
using Elsa.Modules.Http.Models;

namespace Elsa.Modules.Http.Handlers
{
    public class AllowAnonymousHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
    {
        public ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context) => new(true);
    }
}