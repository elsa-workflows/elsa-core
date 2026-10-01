using Elsa.Authorization;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Shared.Authorization;

/// <summary>
/// A FastEndpoints host exercising real endpoint authorization: callers name themselves and their permissions in headers
/// (see <see cref="PermissionHeaderAuthenticationHandler"/>), and only the endpoints <c>endpointFilter</c> admits are mapped.
/// </summary>
/// <remarks>
/// Owns <see cref="EndpointSecurityOptions.SecurityIsEnabled"/> for its lifetime: it is pinned when the host starts and
/// restored on disposal. Endpoints read it when the host maps them, so a host with security disabled is built that way,
/// which is what <see cref="RestartWithSecurityDisabledAsync"/> does. The flag is process-global, so test classes using
/// this host must share a non-parallel xunit collection (declared per test assembly).
/// </remarks>
public sealed class AuthorizationTestHost : IAsyncDisposable
{
    private readonly bool _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
    private readonly Type _endpointAssemblyMarker;
    private readonly Func<Type, bool> _endpointFilter;
    private readonly Action<IServiceCollection>? _configureServices;
    private WebApplication _app;

    private AuthorizationTestHost(Type endpointAssemblyMarker, Func<Type, bool> endpointFilter, Action<IServiceCollection>? configureServices, WebApplication app)
    {
        _endpointAssemblyMarker = endpointAssemblyMarker;
        _endpointFilter = endpointFilter;
        _configureServices = configureServices;
        _app = app;
    }

    /// <summary>Starts a host, with security enabled, mapping the endpoints of the assembly declaring <typeparamref name="TAssemblyMarker"/> that pass <paramref name="endpointFilter"/>.</summary>
    public static async Task<AuthorizationTestHost> StartAsync<TAssemblyMarker>(Func<Type, bool> endpointFilter, Action<IServiceCollection>? configureServices = null)
    {
        var wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = true;

        try
        {
            var app = Build(typeof(TAssemblyMarker), endpointFilter, configureServices);
            await app.StartAsync();
            return new(typeof(TAssemblyMarker), endpointFilter, configureServices, app);
        }
        catch
        {
            EndpointSecurityOptions.SecurityIsEnabled = wasSecurityEnabled;
            throw;
        }
    }

    /// <summary>Replaces the host with an identically configured one built while security is disabled, the way a deployment disables it.</summary>
    public async Task RestartWithSecurityDisabledAsync()
    {
        await StopAsync();
        EndpointSecurityOptions.SecurityIsEnabled = false;
        _app = Build(_endpointAssemblyMarker, _endpointFilter, _configureServices);
        await _app.StartAsync();
    }

    /// <summary>
    /// Sends a request. A request names a user, and so is authenticated, when <paramref name="authenticated"/> is set or
    /// <paramref name="permissions"/> (comma-separated grants) is given; otherwise it is anonymous.
    /// </summary>
    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? permissions = null, bool authenticated = false, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, path) { Content = content };

        if (permissions != null)
        {
            request.Headers.Add(PermissionHeaderAuthenticationHandler.PermissionsHeaderName, permissions);
        }

        if (authenticated || permissions != null)
        {
            request.Headers.Add(PermissionHeaderAuthenticationHandler.UserHeaderName, "test-user");
        }

        return _app.GetTestClient().SendAsync(request);
    }

    public async ValueTask DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        await StopAsync();
    }

    private async Task StopAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private static WebApplication Build(Type endpointAssemblyMarker, Func<Type, bool> endpointFilter, Action<IServiceCollection>? configureServices)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [endpointAssemblyMarker.Assembly];
            options.Filter = endpointFilter;
        });
        configureServices?.Invoke(builder.Services);
        builder.Services
            .AddAuthentication(PermissionHeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, PermissionHeaderAuthenticationHandler>(PermissionHeaderAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseFastEndpoints();
        return app;
    }
}
