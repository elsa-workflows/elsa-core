using System.Threading.Tasks;
using Elsa.Modules.Http.Models;

namespace Elsa.Modules.Http.Services
{
    public interface IHttpEndpointAuthorizationHandler
    {
        ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context);
    }
}