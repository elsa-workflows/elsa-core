using Elsa.Http.Contracts;
using Elsa.Http.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Http.Handlers;

/// <summary>
/// An <see cref="IHttpEndpointAuthorizationHandler"/> that uses the <see cref="IAuthorizationService"/> to authorize an inbound HTTP request.
/// </summary>
[PublicAPI]
public class AuthenticationBasedHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
{
    private readonly IAuthorizationService _authorizationService;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationBasedHttpEndpointAuthorizationHandler"/> class.
    /// </summary>
    public AuthenticationBasedHttpEndpointAuthorizationHandler(IAuthorizationService authorizationService) => _authorizationService = authorizationService;

    /// <inheritdoc />
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