using System.Threading.Tasks;
using Elsa.Http.Models;

namespace Elsa.Http.Services
{
    public interface IHttpEndpointAuthorizationHandler
    {
        ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context);
    }
}