using FastEndpoints;

namespace Elsa.Abstractions;

public abstract class ElsaEndpointWithoutRequest<TResponse> : EndpointWithoutRequest<TResponse> where TResponse : notnull
{
    protected void ConfigurePermissions(params string[] permissions)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            AllowAnonymous();
        else
            Permissions(new[] { PermissionNames.All }.Concat(permissions).ToArray());
    }
}

public class ElsaEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse> where TRequest : notnull, new() where TResponse : notnull
{
    protected void ConfigurePermissions(params string[] permissions)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            AllowAnonymous();
        else
            Permissions(new[] { PermissionNames.All }.Concat(permissions).ToArray());
    }
}

public class ElsaEndpoint<TRequest, TResponse, TMapper> : Endpoint<TRequest, TResponse, TMapper> where TRequest : notnull, new() where TResponse : notnull where TMapper : IMapper, new()
{
    protected void ConfigurePermissions(params string[] permissions)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            AllowAnonymous();
        else
            Permissions(new[] { PermissionNames.All }.Concat(permissions).ToArray());
    }
}

public class ElsaEndpoint<TRequest> : Endpoint<TRequest> where TRequest : notnull, new()
{
    protected void ConfigurePermissions(params string[] permissions)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            AllowAnonymous();
        else
            Permissions(new[] { PermissionNames.All }.Concat(permissions).ToArray());
    }
}