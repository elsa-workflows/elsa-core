using FastEndpoints;
using JetBrains.Annotations;

namespace Elsa.Abstractions;

/// <summary>
/// An endpoint that maps a request to a response.
/// </summary>
public abstract class ElsaEndpointWithMapper<TRequest, TMapper> : EndpointWithMapper<TRequest, TMapper> where TMapper : class, IRequestMapper where TRequest : notnull
{
    protected void ConfigurePermissions(params string[] permissions)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            AllowAnonymous();
        else
            Permissions(new[] { PermissionNames.All }.Concat(permissions).ToArray());
    }
}

public abstract class ElsaEndpointWithoutRequest : EndpointWithoutRequest
{
    protected void ConfigurePermissions(params string[] permissions)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            AllowAnonymous();
        else
            Permissions(new[] { PermissionNames.All }.Concat(permissions).ToArray());
    }
}

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

public class ElsaEndpoint<TRequest, TResponse, TMapper> : Endpoint<TRequest, TResponse, TMapper> where TRequest : notnull, new() where TResponse : notnull where TMapper : class, IMapper, new()
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