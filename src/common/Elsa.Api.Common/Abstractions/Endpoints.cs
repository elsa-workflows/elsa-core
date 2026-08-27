using FastEndpoints;

namespace Elsa.Abstractions;

/// <summary>
/// An endpoint that maps a request to a response.
/// </summary>
public abstract class ElsaEndpointWithMapper<TRequest, TMapper> : EndpointWithMapper<TRequest, TMapper> where TMapper : class, IRequestMapper where TRequest : notnull
{
    /// <summary>Requires a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>.</summary>
    protected void RequirePermission(string resource, string verb) => EndpointSecurity.RequirePermission(Definition, resource, verb);

    /// <summary>Requires an authenticated caller but no permission.</summary>
    protected void RequireAuthenticatedOnly() => EndpointSecurity.RequireAuthenticatedOnly(Definition);

    /// <summary>Declares required permissions as legacy strings.</summary>
    protected void ConfigurePermissions(params string[] permissions) => EndpointSecurity.ConfigurePermissions(Definition, permissions);
}

public abstract class ElsaEndpointWithoutRequest : EndpointWithoutRequest
{
    /// <summary>Requires a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>.</summary>
    protected void RequirePermission(string resource, string verb) => EndpointSecurity.RequirePermission(Definition, resource, verb);

    /// <summary>Requires an authenticated caller but no permission.</summary>
    protected void RequireAuthenticatedOnly() => EndpointSecurity.RequireAuthenticatedOnly(Definition);

    /// <summary>Declares required permissions as legacy strings.</summary>
    protected void ConfigurePermissions(params string[] permissions) => EndpointSecurity.ConfigurePermissions(Definition, permissions);
}

public abstract class ElsaEndpointWithoutRequest<TResponse> : EndpointWithoutRequest<TResponse> where TResponse : notnull
{
    /// <summary>Requires a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>.</summary>
    protected void RequirePermission(string resource, string verb) => EndpointSecurity.RequirePermission(Definition, resource, verb);

    /// <summary>Requires an authenticated caller but no permission.</summary>
    protected void RequireAuthenticatedOnly() => EndpointSecurity.RequireAuthenticatedOnly(Definition);

    /// <summary>Declares required permissions as legacy strings.</summary>
    protected void ConfigurePermissions(params string[] permissions) => EndpointSecurity.ConfigurePermissions(Definition, permissions);
}

public class ElsaEndpoint<TRequest, TResponse> : Endpoint<TRequest, TResponse> where TRequest : notnull, new() where TResponse : notnull
{
    /// <summary>Requires a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>.</summary>
    protected void RequirePermission(string resource, string verb) => EndpointSecurity.RequirePermission(Definition, resource, verb);

    /// <summary>Requires an authenticated caller but no permission.</summary>
    protected void RequireAuthenticatedOnly() => EndpointSecurity.RequireAuthenticatedOnly(Definition);

    /// <summary>Declares required permissions as legacy strings.</summary>
    protected void ConfigurePermissions(params string[] permissions) => EndpointSecurity.ConfigurePermissions(Definition, permissions);
}

public class ElsaEndpoint<TRequest, TResponse, TMapper> : Endpoint<TRequest, TResponse, TMapper> where TRequest : notnull, new() where TResponse : notnull where TMapper : class, IMapper, new()
{
    /// <summary>Requires a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>.</summary>
    protected void RequirePermission(string resource, string verb) => EndpointSecurity.RequirePermission(Definition, resource, verb);

    /// <summary>Requires an authenticated caller but no permission.</summary>
    protected void RequireAuthenticatedOnly() => EndpointSecurity.RequireAuthenticatedOnly(Definition);

    /// <summary>Declares required permissions as legacy strings.</summary>
    protected void ConfigurePermissions(params string[] permissions) => EndpointSecurity.ConfigurePermissions(Definition, permissions);
}

public class ElsaEndpoint<TRequest> : Endpoint<TRequest> where TRequest : notnull, new()
{
    /// <summary>Requires a permission satisfying <paramref name="resource"/> and <paramref name="verb"/>.</summary>
    protected void RequirePermission(string resource, string verb) => EndpointSecurity.RequirePermission(Definition, resource, verb);

    /// <summary>Requires an authenticated caller but no permission.</summary>
    protected void RequireAuthenticatedOnly() => EndpointSecurity.RequireAuthenticatedOnly(Definition);

    /// <summary>Declares required permissions as legacy strings.</summary>
    protected void ConfigurePermissions(params string[] permissions) => EndpointSecurity.ConfigurePermissions(Definition, permissions);
}