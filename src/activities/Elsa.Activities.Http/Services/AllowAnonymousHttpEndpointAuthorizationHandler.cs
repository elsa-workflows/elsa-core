using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Models;

namespace Elsa.Activities.Http.Services
{
    public class AllowAnonymousHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
    {
        public ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context) => new(true);
    }
}