using System.Threading.Tasks;
using Elsa.Modules.Http.Models;

namespace Elsa.Modules.Http.Contracts
{
    public interface IHttpEndpointAuthorizationHandler
    {
        ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context);
    }
}