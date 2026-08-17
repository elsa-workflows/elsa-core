using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Elsa.Identity.Contracts;
using Elsa.Identity.Constants;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Compatibility;

/// <summary>
/// Protects the existing direct local-credential contracts while the broker-local flow remains additive.
/// </summary>
public sealed class LegacyIdentityEndpointTests : IAsyncLifetime
{
    private readonly IUserCredentialsValidator _credentialsValidator = Substitute.For<IUserCredentialsValidator>();
    private readonly IUserProvider _userProvider = Substitute.For<IUserProvider>();
    private readonly IAccessTokenIssuer _tokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private WebApplication? _app;
    private HttpClient? _client;
    private bool _wasSecurityEnabled;

    public async Task InitializeAsync()
    {
        _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = false;

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(Elsa.Identity.Features.IdentityFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace is "Elsa.Identity.Endpoints.Login" or "Elsa.Identity.Endpoints.RefreshToken";
        });
        builder.Services.AddSingleton(_credentialsValidator);
        builder.Services.AddSingleton(_userProvider);
        builder.Services.AddSingleton(_tokenIssuer);
        builder.Services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, RefreshTokenAuthenticationHandler>(IdentityAuthenticationSchemes.RefreshToken, _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "legacy-refresh"));
            await next(context);
        });
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        _client?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task IdentityLoginRetainsItsRouteCredentialValidationAndResponseShape()
    {
        var user = new User { Id = "user-a", Name = "admin" };
        _credentialsValidator.ValidateAsync("admin", "password", Arg.Any<CancellationToken>()).Returns(user);
        _tokenIssuer.IssueTokensAsync(user, Arg.Any<CancellationToken>()).Returns(new IssuedTokens("access-a", "refresh-a"));

        var response = await _client!.PostAsJsonAsync("/identity/login", new { username = " admin ", password = " password " });
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal("access-a", document.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal("refresh-a", document.RootElement.GetProperty("refreshToken").GetString());
        await _credentialsValidator.Received(1).ValidateAsync("admin", "password", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IdentityLoginRetainsItsGenericUnauthenticatedResponse()
    {
        _credentialsValidator.ValidateAsync("unknown", "wrong", Arg.Any<CancellationToken>()).Returns((User?)null);

        var response = await _client!.PostAsJsonAsync("/identity/login", new { username = "unknown", password = "wrong" });
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(document.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("accessToken").ValueKind);
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("refreshToken").ValueKind);
    }

    [Fact]
    public async Task IdentityRefreshTokenRetainsItsRouteAndLocalTokenContract()
    {
        var user = new User { Id = "user-a", Name = "admin" };
        _userProvider.FindAsync(Arg.Is<UserFilter>(filter => filter.Name == "admin"), Arg.Any<CancellationToken>()).Returns(user);
        _tokenIssuer.IssueTokensAsync(user, Arg.Any<CancellationToken>()).Returns(new IssuedTokens("access-b", "refresh-b"));

        var response = await _client!.PostAsync("/identity/refresh-token", null);
        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.GetProperty("isAuthenticated").GetBoolean());
        Assert.Equal("access-b", document.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal("refresh-b", document.RootElement.GetProperty("refreshToken").GetString());
    }

    private sealed class RefreshTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
