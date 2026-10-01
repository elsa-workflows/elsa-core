using System.Net;
using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Testing.Shared.Authorization;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Api.Common.UnitTests.Abstractions;

/// <summary>
/// An endpoint declaring <c>RequireAnyPermission</c> opens to a caller holding any one of the permissions it names, and is
/// otherwise enforced, recorded and switched off exactly as <c>RequirePermission</c> is.
/// </summary>
[Collection(nameof(EndpointSecurityCollection))]
public class RequireAnyPermissionTests : IAsyncLifetime
{
    private static readonly Permission Alpha = new("tests/alpha", CoreVerbs.View);
    private static readonly Permission Beta = new("tests/beta", CoreVerbs.View);

    /// <summary>Grants satisfying one of the two permissions, directly or through a wildcard.</summary>
    public static readonly TheoryData<string> SatisfyingGrants = new(Alpha.ToString(), Beta.ToString(), "tests/*:view", "*:view", "tests/beta:*", "*");

    /// <summary>Grants close to one of the two permissions that satisfy neither.</summary>
    public static readonly TheoryData<string> UnsatisfyingGrants = new("tests/gamma:view", "tests/alpha:delete", "tests/alpha/child:view", "*:delete");

    private readonly bool _wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
    private WebApplication _app;

    public RequireAnyPermissionTests()
    {
        EndpointSecurityOptions.SecurityIsEnabled = true;
        _app = CreateApp();
    }

    public Task InitializeAsync() => _app.StartAsync();

    public async Task DisposeAsync()
    {
        EndpointSecurityOptions.SecurityIsEnabled = _wasSecurityEnabled;
        await StopAppAsync();
    }

    [Theory]
    [MemberData(nameof(SatisfyingGrants))]
    public async Task Caller_HoldingAGrantThatSatisfiesEitherPermission_IsAllowed(string grant)
    {
        var response = await SendAsync(grant);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(UnsatisfyingGrants))]
    public async Task Caller_HoldingAGrantThatSatisfiesNeitherPermission_IsForbidden(string grant)
    {
        var response = await SendAsync(grant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Caller_HoldingNoPermission_IsForbidden()
    {
        var response = await SendAsync(authenticated: true);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousCaller_IsChallenged()
    {
        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousCaller_WithSecurityDisabled_IsAllowed()
    {
        // Disabled the way a deployment disables it: before the host maps its endpoints, which is when it is read.
        await StopAppAsync();
        EndpointSecurityOptions.SecurityIsEnabled = false;
        _app = CreateApp();
        await _app.StartAsync();

        var response = await SendAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Declaration_IsRecordedAsOneRequirementNamingBothPermissions()
    {
        var requirement = EndpointPermissionRegistry.FindRequirement(typeof(AnyOfEndpoint));

        Assert.NotNull(requirement);
        Assert.Equal([Alpha, Beta], requirement.AnyOf);
        Assert.Null(EndpointPermissionRegistry.Find(typeof(AnyOfEndpoint)));
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddFastEndpoints(options =>
        {
            options.Assemblies = [typeof(AnyOfEndpoint).Assembly];
            options.Filter = endpoint => endpoint == typeof(AnyOfEndpoint);
        });
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

    private async Task StopAppAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private Task<HttpResponseMessage> SendAsync(string? permissions = null, bool authenticated = false)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, AnyOfEndpoint.Route);

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

    public sealed class AnyOfEndpoint : ElsaEndpointWithoutRequest<string>
    {
        public const string Route = "/tests/any-of";

        public override void Configure()
        {
            Get(Route);
            RequireAnyPermission((Alpha.Resource, Alpha.Verb), (Beta.Resource, Beta.Verb));
        }

        public override Task<string> ExecuteAsync(CancellationToken ct) => Task.FromResult("ok");
    }
}

[CollectionDefinition(nameof(EndpointSecurityCollection), DisableParallelization = true)]
public class EndpointSecurityCollection;
