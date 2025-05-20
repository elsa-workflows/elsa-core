namespace Elsa.Http.Handlers;

/// <summary>
/// A default <see cref="IHttpEndpointAuthorizationHandler"/> that allows all requests.
/// </summary>
public class AllowAnonymousHttpEndpointAuthorizationHandler : IHttpEndpointAuthorizationHandler
{
    /// <inheritdoc />
    public ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context) => new(true);
}