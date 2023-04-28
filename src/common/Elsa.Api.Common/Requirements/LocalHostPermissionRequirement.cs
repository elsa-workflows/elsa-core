using System.Security.Claims;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Elsa.Requirements;

/// <summary>
/// Adda the "create:application" permission to the current user if the request is local.
/// </summary>
public class LocalHostPermissionRequirement : IAuthorizationRequirement
{
}

/// <inheritdoc />
[PublicAPI]
public class LocalHostPermissionRequirementHandler : AuthorizationHandler<LocalHostPermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <inheritdoc />
    public LocalHostPermissionRequirementHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalHostPermissionRequirement requirement)
    {
        if (_httpContextAccessor.HttpContext?.Request.IsLocal() == false)
            return Task.CompletedTask;
        
        var currentIdentity = context.User.Identity;

        if (currentIdentity?.IsAuthenticated == false)
        {
            var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim("permissions", "create:application"));
            identity.AddClaim(new Claim("permissions", "create:user"));
            identity.AddClaim(new Claim("permissions", "create:role"));
            context.User.AddIdentity(identity);
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}