using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Requirements;

/// <summary>
/// Represents an authorization requirement for localhost requests, meaning that if a request comes from the localhost, the requirement is met.
/// </summary>
public class LocalHostRequirement : IAuthorizationRequirement
{
}

/// <inheritdoc />
[PublicAPI]
public class LocalHostRequirementHandler : AuthorizationHandler<LocalHostRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <inheritdoc />
    public LocalHostRequirementHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalHostRequirement requirement)
    {
        if (_httpContextAccessor.HttpContext?.Request.IsLocal() == false)
            context.Fail(new AuthorizationFailureReason(this, "Only requests from localhost are allowed"));

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}