using System.Threading.Tasks;
using Elsa.Activities.Http.Contracts;
using Elsa.Activities.Http.Models;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Activities.Http.Services
{
    public class AuthenticationBasedHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
    {
        private readonly IAuthorizationService _authorizationService;
        public AuthenticationBasedHttpEndpointAuthorizationHandler(IAuthorizationService authorizationService) => _authorizationService = authorizationService;

        public async ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context)
        {
            var httpContext = context.HttpContext;
            var user = httpContext.User;
            var identity = user.Identity;

            if (identity == null)
                return false;
            
            if (identity.IsAuthenticated == false)
                return false;

            var cancellationToken = context.CancellationToken;
            var httpEndpoint = context.HttpEndpointActivity;
            var policyName = await httpEndpoint.EvaluatePropertyValueAsync(x => x.Policy, cancellationToken);

            if (string.IsNullOrWhiteSpace(policyName))
                return identity.IsAuthenticated;

            var resource = new HttpWorkflowResource(context.WorkflowBlueprint, httpEndpoint.ActivityBlueprint, context.WorkflowInstanceId);
            var authorizationResult = await _authorizationService.AuthorizeAsync(user, resource, policyName);
            return authorizationResult.Succeeded;
        }
    }
}