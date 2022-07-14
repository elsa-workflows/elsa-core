using System.Threading.Tasks;
using Elsa.Activities.Http.Models;

namespace Elsa.Activities.Http.Contracts
{
    public interface IHttpEndpointAuthorizationHandler
    {
        ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context);
    }
}