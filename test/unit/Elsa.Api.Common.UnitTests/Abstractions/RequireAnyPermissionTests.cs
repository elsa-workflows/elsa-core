using System.Net;
using System.Reflection;
using Elsa.Abstractions;
using Elsa.Authorization;
using Elsa.Testing.Shared.Authorization;
using FastEndpoints;

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

    private AuthorizationTestHost _host = null!;

    public async Task InitializeAsync() => _host = await AuthorizationTestHost.StartAsync<AnyOfEndpoint>(endpoint => endpoint == typeof(AnyOfEndpoint));

    public async Task DisposeAsync() => await _host.DisposeAsync();

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
        await _host.RestartWithSecurityDisabledAsync();

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeclaringNoPermissions_IsRejected_WhetherOrNotSecurityIsEnabled(bool securityIsEnabled)
    {
        // The empty declaration is rejected before the security check, so a host running without security still surfaces the mistake.
        var wasSecurityEnabled = EndpointSecurityOptions.SecurityIsEnabled;
        EndpointSecurityOptions.SecurityIsEnabled = securityIsEnabled;

        try
        {
            var endpoint = new EmptyDeclarationEndpoint();
            typeof(EmptyDeclarationEndpoint)
                .GetProperty("Definition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(endpoint, new EndpointDefinition(typeof(EmptyDeclarationEndpoint), typeof(EmptyRequest), typeof(string)));

            Assert.Throws<ArgumentException>(endpoint.Configure);
        }
        finally
        {
            EndpointSecurityOptions.SecurityIsEnabled = wasSecurityEnabled;
        }
    }

    private Task<HttpResponseMessage> SendAsync(string? permissions = null, bool authenticated = false) =>
        _host.SendAsync(HttpMethod.Get, AnyOfEndpoint.Route, permissions, authenticated);

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

    public sealed class EmptyDeclarationEndpoint : ElsaEndpointWithoutRequest<string>
    {
        public override void Configure()
        {
            Get("/tests/empty");
            RequireAnyPermission();
        }

        public override Task<string> ExecuteAsync(CancellationToken ct) => Task.FromResult("ok");
    }
}

[CollectionDefinition(nameof(EndpointSecurityCollection), DisableParallelization = true)]
public class EndpointSecurityCollection;
