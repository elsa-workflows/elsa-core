using System.Net;
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
        if(!IsLocal(_httpContextAccessor.HttpContext!.Request))
            context.Fail(new AuthorizationFailureReason(this, "Only requests from localhost are allowed"));
        
        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private static bool IsLocal(HttpRequest request)
    {
        var connection = request.HttpContext.Connection;
        return connection.RemoteIpAddress != null
            ? connection.LocalIpAddress != null
                ? connection.RemoteIpAddress.Equals(connection.LocalIpAddress)
                : IPAddress.IsLoopback(connection.RemoteIpAddress)
            : connection.RemoteIpAddress == null && connection.LocalIpAddress == null;
    }
}