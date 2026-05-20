using System.Security.Claims;
using Elsa.Extensions;
using Elsa.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Requirements;

/// <summary>
/// Adds security-root bootstrap permissions to the current user when explicit localhost permission grants are enabled and the request is local.
/// </summary>
public class LocalHostPermissionRequirement : IAuthorizationRequirement
{
}

/// <inheritdoc />
[PublicAPI]
public class LocalHostPermissionRequirementHandler : AuthorizationHandler<LocalHostPermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<LocalHostPermissionRequirementOptions> _options;

    /// <inheritdoc />
    public LocalHostPermissionRequirementHandler(IHttpContextAccessor httpContextAccessor) : this(
        httpContextAccessor,
        Microsoft.Extensions.Options.Options.Create(new LocalHostPermissionRequirementOptions()))
    {
    }

    /// <inheritdoc />
    public LocalHostPermissionRequirementHandler(IHttpContextAccessor httpContextAccessor, IOptions<LocalHostPermissionRequirementOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LocalHostPermissionRequirement requirement)
    {
        if (!_options.Value.EnableLocalHostPermissionGrant)
            return Task.CompletedTask;

        if (_httpContextAccessor.HttpContext?.Request.IsLocal() != true)
            return Task.CompletedTask;

        var currentIdentity = context.User.Identity;

        if (currentIdentity?.IsAuthenticated != true)
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
