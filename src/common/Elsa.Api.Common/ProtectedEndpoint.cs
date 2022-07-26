using FastEndpoints;

namespace Elsa.Api.Common;

public class ProtectedEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse> where TRequest : notnull, new() where TResponse : notnull, new()
{
    protected void ConfigureSecurity(IEnumerable<string> permissions, IEnumerable<string> policies, IEnumerable<string> roles)
    {
        if (ApiSecurityOptions.AllowAnonymous)
        {
            AllowAnonymous();
            return;
        }

        if (ApiSecurityOptions.ValidatePermissions)
            Permissions(new[] { PermissionNames.Everything }.Concat(permissions).ToArray());

        if (ApiSecurityOptions.ValidatePolicies)
            Policies(new[] { PolicyNames.Admin }.Concat(policies).ToArray());

        if (ApiSecurityOptions.ValidateRoles)
            Roles(new[] { RoleNames.Admin }.Concat(roles).ToArray());
    }
}

public class ProtectedEndpoint<TRequest> : Endpoint<TRequest> where TRequest : notnull, new()
{
    protected void ConfigureSecurity(IEnumerable<string> permissions, IEnumerable<string> policies, IEnumerable<string> roles)
    {
        if (ApiSecurityOptions.AllowAnonymous)
        {
            AllowAnonymous();
            return;
        }

        if (ApiSecurityOptions.ValidatePermissions)
            Permissions(new[] { PermissionNames.Everything }.Concat(permissions).ToArray());

        if (ApiSecurityOptions.ValidatePolicies)
            Policies(new[] { PolicyNames.Admin }.Concat(policies).ToArray());

        if (ApiSecurityOptions.ValidateRoles)
            Roles(new[] { RoleNames.Admin }.Concat(roles).ToArray());
    }
}