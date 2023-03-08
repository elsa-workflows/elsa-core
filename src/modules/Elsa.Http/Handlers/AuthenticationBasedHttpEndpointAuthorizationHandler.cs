using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Http.Handlers;

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

        if (string.IsNullOrWhiteSpace(context.Policy))
            return identity.IsAuthenticated;

        var authorizationResult = await _authorizationService.AuthorizeAsync(user,
            new { workflowInstanceId = context.WorkflowInstanceId }, context.Policy!);

        return authorizationResult.Succeeded;
    }
}