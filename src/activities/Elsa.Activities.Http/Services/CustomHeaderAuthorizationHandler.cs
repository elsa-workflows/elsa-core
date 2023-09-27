using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Models;

namespace Elsa.Activities.Http.Services
{
    public class CustomHeaderAuthorizationHandler : IHttpEndpointAuthorizationHandler
    {
        public async ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context)
        {
            var httpContext = context.HttpContext;

            var cancellationToken = context.CancellationToken;
            var httpEndpoint = context.HttpEndpointActivity;
            var headerName = await httpEndpoint.EvaluatePropertyValueAsync(x => x.CustomHeaderName, cancellationToken);
            var expectedHeaderValue = await httpEndpoint.EvaluatePropertyValueAsync(x => x.CustomHeaderValue, cancellationToken);

            if (string.IsNullOrWhiteSpace(headerName) || string.IsNullOrWhiteSpace(expectedHeaderValue))
                return true;

            return httpContext.Request.Headers[headerName] == expectedHeaderValue;
        }
    }
}