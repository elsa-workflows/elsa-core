using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Elsa.Features.Contracts;
using Elsa.Features.Models;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using WorkflowsApiFeature = Elsa.Workflows.Api.Features.WorkflowsApiFeature;

namespace Elsa.Workflows.Api.UnitTests.Endpoints.Features;

/// <summary>
/// Studio decides which modules to render from the installed features, so any signed-in user must be able to read
/// them. Requiring <c>system/features:view</c> hid every feature-gated module, including ones the caller holds
/// permissions for, from anyone without that grant.
/// </summary>
public class InstalledFeaturesAuthorizationTests : IAsyncLifetime
{
    private const string FeatureFullName = "Elsa.Secrets.SecretsFeature";
    private readonly WebApplication _app;

    public InstalledFeaturesAuthorizationTests()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(WorkflowsApiFeature).Assembly];
            options.Filter = endpoint => endpoint.Namespace?.StartsWith("Elsa.Workflows.Api.Endpoints.Features", StringComparison.Ordinal) == true;
        });

        var feature = new FeatureDescriptor("SecretsFeature", "Elsa.Secrets", "Secrets");
        var installedFeatureProvider = Substitute.For<IInstalledFeatureProvider>();
        installedFeatureProvider.List().Returns([feature]);
        installedFeatureProvider.Find(FeatureFullName).Returns(feature);
        builder.Services.AddSingleton(installedFeatureProvider);

        builder.Services
            .AddAuthentication(HeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(HeaderAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
    }

    public Task InitializeAsync() => _app.StartAsync();

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Theory]
    [InlineData("/features/installed")]
    [InlineData("/features/installed/" + FeatureFullName)]
    public async Task AuthenticatedUserWithoutAnyPermission_IsAllowed(string path)
    {
        var response = await SendAsync(path, authenticated: true);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(FeatureFullName, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/features/installed")]
    [InlineData("/features/installed/" + FeatureFullName)]
    public async Task AnonymousCaller_IsRejected(string path)
    {
        var response = await SendAsync(path, authenticated: false);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(string path, bool authenticated)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);

        if (authenticated)
            request.Headers.Add(HeaderAuthenticationHandler.HeaderName, "user-without-grants");

        return _app.GetTestClient().SendAsync(request);
    }

    /// <summary>
    /// Authenticates a caller that names itself in a header, with no permission claims at all; a request without the
    /// header stays anonymous.
    /// </summary>
    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Header";
        public const string HeaderName = "X-Test-User";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var user))
                return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString())], SchemeName);

            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
