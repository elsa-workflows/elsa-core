using System.Security.Claims;
using Elsa.Authorization;
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
    // The three grants a local first-run needs to stand an instance up, spelled in the structured
    // {resource}:{verb} vocabulary. These were previously the legacy strings "create:application",
    // "create:user" and "create:role", which no longer authorize anything: a legacy string parses to a
    // different pair entirely -- "create:user" reads as resource "create", verb "user" -- so the endpoints
    // this grant exists to unlock (identity/applications:create, identity/users:create,
    // identity/roles:create) all refused it. The literals are spelled out rather than referenced from
    // Elsa.Identity because this assembly sits beneath it.
    private static readonly Permission[] BootstrapPermissions =
    [
        new("identity/applications", CoreVerbs.Create),
        new("identity/users", CoreVerbs.Create),
        new("identity/roles", CoreVerbs.Create)
    ];

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

        if (context.User.Identities.Any(x => x.IsAuthenticated))
        {
            if (HasBootstrapPermissions(context.User))
                context.Succeed(requirement);

            return Task.CompletedTask;
        }

        var identity = new ClaimsIdentity(JwtBearerDefaults.AuthenticationScheme);
        identity.AddClaims(BootstrapPermissions.Select(permission => new Claim(PermissionNames.ClaimType, permission.ToString())));
        context.User.AddIdentity(identity);

        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    // Evaluated rather than compared by claim value, so a caller holding a wildcard that covers these --
    // "*" as before, but now also "identity/*:create" -- satisfies the requirement.
    private static bool HasBootstrapPermissions(ClaimsPrincipal user) =>
        PermissionEvaluator.Shared.HasAllPermissions(user, BootstrapPermissions);
}
