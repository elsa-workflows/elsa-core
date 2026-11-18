using System.Net;
using System.Text;
using System.Text.Json;
using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.BlazorServer.Extensions;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.Extensions;
using Elsa.Studio.Login.HttpMessageHandlers;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;
using Xunit;
using ElsaIdentityValidator = Elsa.Studio.Authentication.ElsaIdentity.Services.ElsaIdentityCredentialsValidator;
using ElsaIdentityTokenNames = Elsa.Studio.Authentication.ElsaIdentity.TokenNames;
using LegacyTokenNames = Elsa.Studio.Login.TokenNames;
using LegacyValidator = Elsa.Studio.Login.Services.ElsaIdentityCredentialsValidator;

namespace Elsa.Studio.Environments.Tests;

/// <summary>
/// Exercises anonymous login (#9072) through the real DI registrations and HTTP client pipeline:
/// <see cref="IAnonymousBackendApiClientProvider"/> must use a client without authentication delegating
/// handlers, so a 401 from <c>/identity/login</c> neither refreshes tokens nor injects a bearer header,
/// even when a previous session's access and refresh tokens are stored.
/// </summary>
public class AnonymousLoginPipelineTests
{
    [Fact]
    public async Task AnonymousLogin401IsInvalidCredentialsWithoutRefreshForLegacyValidator()
    {
        var recorder = new RecordingBackendHandler(() => JsonResponse(HttpStatusCode.Unauthorized, new { isAuthenticated = false }));
        using var provider = BuildServices<AuthenticatingApiHttpMessageHandler>(recorder);
        await SeedPreviousSessionAsync(provider);
        var validator = provider.GetRequiredService<LegacyValidator>();

        var result = await validator.ValidateCredentialsAsync("admin", "wrong-password");

        Assert.False(result.IsAuthenticated);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        AssertAnonymousSingleLogin(recorder);
    }

    [Fact]
    public async Task AnonymousLogin401IsInvalidCredentialsWithoutRefreshForElsaIdentityValidator()
    {
        var recorder = new RecordingBackendHandler(() => JsonResponse(HttpStatusCode.Unauthorized, new { isAuthenticated = false }));
        using var provider = BuildServices<ElsaIdentityAuthenticatingApiHttpMessageHandler>(recorder);
        await SeedPreviousSessionAsync(provider);
        var validator = provider.GetRequiredService<ElsaIdentityValidator>();

        var result = await validator.ValidateCredentialsAsync("admin", "wrong-password");

        Assert.False(result.IsValid);
        Assert.Null(result.AccessToken);
        Assert.Null(result.RefreshToken);
        AssertAnonymousSingleLogin(recorder);
    }

    [Fact]
    public async Task AnonymousLoginSuccessReturnsTokensWithoutBearerHeaderOrRefreshForLegacyValidator()
    {
        var recorder = new RecordingBackendHandler(() => JsonResponse(HttpStatusCode.OK, new
        {
            isAuthenticated = true,
            accessToken = "access-new",
            refreshToken = "refresh-new"
        }));
        using var provider = BuildServices<AuthenticatingApiHttpMessageHandler>(recorder);
        await SeedPreviousSessionAsync(provider);
        var validator = provider.GetRequiredService<LegacyValidator>();

        var result = await validator.ValidateCredentialsAsync("admin", "password");

        Assert.True(result.IsAuthenticated);
        Assert.Equal("access-new", result.AccessToken);
        Assert.Equal("refresh-new", result.RefreshToken);
        AssertAnonymousSingleLogin(recorder);
    }

    [Fact]
    public async Task AnonymousLoginSuccessReturnsTokensWithoutBearerHeaderOrRefreshForElsaIdentityValidator()
    {
        var recorder = new RecordingBackendHandler(() => JsonResponse(HttpStatusCode.OK, new
        {
            isAuthenticated = true,
            accessToken = "access-new",
            refreshToken = "refresh-new"
        }));
        using var provider = BuildServices<ElsaIdentityAuthenticatingApiHttpMessageHandler>(recorder);
        await SeedPreviousSessionAsync(provider);
        var validator = provider.GetRequiredService<ElsaIdentityValidator>();

        var result = await validator.ValidateCredentialsAsync("admin", "password");

        Assert.True(result.IsValid);
        Assert.Equal("access-new", result.AccessToken);
        Assert.Equal("refresh-new", result.RefreshToken);
        AssertAnonymousSingleLogin(recorder);
    }

    [Fact]
    public async Task AnonymousLoginServerErrorOtherThan401Propagates()
    {
        var recorder = new RecordingBackendHandler(() => JsonResponse(HttpStatusCode.InternalServerError, new { message = "boom" }));
        using var provider = BuildServices<AuthenticatingApiHttpMessageHandler>(recorder);
        await SeedPreviousSessionAsync(provider);
        var legacy = provider.GetRequiredService<LegacyValidator>();
        var elsaIdentity = provider.GetRequiredService<ElsaIdentityValidator>();

        await Assert.ThrowsAsync<ApiException>(() => legacy.ValidateCredentialsAsync("admin", "password").AsTask());
        await Assert.ThrowsAsync<ApiException>(() => elsaIdentity.ValidateCredentialsAsync("admin", "password").AsTask());

        Assert.Equal(2, recorder.Requests.Count);
        Assert.All(recorder.Requests, request =>
        {
            Assert.Equal("POST", request.Method);
            Assert.EndsWith("/identity/login", request.PathAndQuery);
            Assert.Null(request.AuthorizationScheme);
            Assert.Null(request.AuthorizationParameter);
        });
        Assert.DoesNotContain(recorder.Requests, request => request.PathAndQuery.Contains("/identity/refresh-token"));
    }

    /// <summary>
    /// Builds a service provider from the real production registrations: <see cref="ServiceCollectionExtensions.AddRemoteBackend"/>
    /// (per-API named clients wired with the authentication handler), the real validators and the real refresh services, while a
    /// recording primary handler serves every request so the test never reaches the network.
    /// </summary>
    private static ServiceProvider BuildServices<TAuthHandler>(RecordingBackendHandler recorder)
        where TAuthHandler : DelegatingHandler
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();

        services.AddRemoteBackend(new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/elsa/api/"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(TAuthHandler)
        });
        services.AddTransient<TAuthHandler>();

        // Serve all HTTP requests of the DI pipeline (per-API named clients, the anonymous client and the default
        // refresh client) from the recorder, mirroring a real backend.
        services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(() => recorder));

        // Real legacy login module: the validating handler would call the real refresh service on 401.
        services.AddLoginModuleCore();
        services.UseElsaIdentity();

        // Real ElsaIdentity module: validator, refresh service and authenticating handler dependencies.
        services.AddElsaIdentity();

        // Both modules store tokens behind IJwtAccessor; use a shared memory-backed store.
        var tokenStore = new MemoryTokenStore();
        services.AddScoped<Elsa.Studio.Login.Contracts.IJwtAccessor>(_ => tokenStore.Accessor);
        services.Replace(ServiceDescriptor.Scoped<Elsa.Studio.Authentication.ElsaIdentity.Contracts.IJwtAccessor>(_ => tokenStore.Accessor));

        // Make the concrete validators resolvable by type (production registers them as ICredentialsValidator).
        services.AddScoped<Elsa.Studio.Login.Services.ElsaIdentityCredentialsValidator>();
        services.AddScoped<Elsa.Studio.Authentication.ElsaIdentity.Services.ElsaIdentityCredentialsValidator>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Seeds a previous session with a valid, non-expired access token and a refresh token, exactly the state a
    /// browser holds when the user submits fresh login credentials from the sign-in page.
    /// </summary>
    private static async Task SeedPreviousSessionAsync(IServiceProvider provider)
    {
        var legacyAccessor = provider.GetRequiredService<Elsa.Studio.Login.Contracts.IJwtAccessor>();
        await legacyAccessor.WriteTokenAsync(LegacyTokenNames.AccessToken, ValidJwt);
        await legacyAccessor.WriteTokenAsync(LegacyTokenNames.RefreshToken, "refresh-previous");

        var identityAccessor = provider.GetRequiredService<Elsa.Studio.Authentication.ElsaIdentity.Contracts.IJwtAccessor>();
        await identityAccessor.WriteTokenAsync(ElsaIdentityTokenNames.AccessToken, ValidJwt);
        await identityAccessor.WriteTokenAsync(ElsaIdentityTokenNames.RefreshToken, "refresh-previous");
    }

    private static void AssertAnonymousSingleLogin(RecordingBackendHandler recorder)
    {
        // Exactly one request: the anonymous login. No refresh-token request followed it.
        var login = Assert.Single(recorder.Requests);
        Assert.Equal("POST", login.Method);
        Assert.EndsWith("/identity/login", login.PathAndQuery);

        // A previous access token was stored; if the authenticating handler had executed, it would have attached a
        // bearer header and attempted a refresh. Their absence proves the anonymous client skips the handler.
        Assert.Null(login.AuthorizationScheme);
        Assert.Null(login.AuthorizationParameter);
        Assert.DoesNotContain(recorder.Requests, request => request.PathAndQuery.Contains("/identity/refresh-token"));
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, object body)
    {
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    /// <summary>A minimal JWT with an exp far in the future, accepted as a current access token.</summary>
    private static string ValidJwt { get; } =
        $"header.{Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"name":"admin","exp":9000000000}"""))}.signature";

    private sealed record RecordedRequest(string Method, string PathAndQuery, string? AuthorizationScheme, string? AuthorizationParameter);

    /// <summary>Replaces the real backend for every client in the pipeline while recording each request.</summary>
    private sealed class RecordingBackendHandler(Func<HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new(
                request.Method.ToString(),
                request.RequestUri?.PathAndQuery ?? string.Empty,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter));

            // The real transport stamps the response with its request; Refit needs it to build ApiException instances.
            var response = responder();
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Memory-backed token storage shared by both <c>IJwtAccessor</c> contracts.
    /// </summary>
    private sealed class MemoryTokenStore
    {
        public MemoryJwtAccessor Accessor { get; } = new();
    }

    private sealed class MemoryJwtAccessor :
        Elsa.Studio.Login.Contracts.IJwtAccessor,
        Elsa.Studio.Authentication.ElsaIdentity.Contracts.IJwtAccessor
    {
        private readonly Dictionary<string, string> _tokens = [];

        public ValueTask<string?> ReadTokenAsync(string name) =>
            ValueTask.FromResult(_tokens.TryGetValue(name, out var token) ? token : null);

        public ValueTask WriteTokenAsync(string name, string token)
        {
            _tokens[name] = token;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearTokenAsync(string name)
        {
            _tokens.Remove(name);
            return ValueTask.CompletedTask;
        }
    }
}
