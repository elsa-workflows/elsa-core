using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa;
using Elsa.Authorization;
using Elsa.Common.Services;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Features;
using Elsa.Identity.HostedServices;
using Elsa.Identity.Models;
using Elsa.Identity.Permissions;
using Elsa.UnitTests.Shared;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Endpoints;

/// <summary>
/// A zero-grant user's Elsa tokens must carry a surviving <c>permissions</c> claim so Studio can tell
/// "known empty" from "unknown", while gated endpoints stay 403.
/// </summary>
[Collection(nameof(FastEndpointsCollection))]
public sealed class EmptyPermissionsClaimTests : IAsyncLifetime
{
    private static readonly User NoPerm = new() { Id = "noperm-id", Name = "noperm", Roles = ["noperm"] };
    private static readonly User Reader = new() { Id = "reader-id", Name = "reader", Roles = ["reader"] };
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        var module = Substitute.For<IModule>();
        module.Services.Returns(builder.Services);
        new IdentityFeature(module) { TokenOptions = options => options.SigningKey = IdentityTokenTestConstants.SigningKey }.Apply();
        new DefaultAuthenticationFeature(module).Apply();

        foreach (var diagnostic in builder.Services.Where(x => x.ImplementationType?.Namespace == typeof(StoredPermissionValidator).Namespace).ToList())
            builder.Services.Remove(diagnostic);

        builder.Services
            .AddSingleton<Elsa.Common.ISystemClock, Elsa.Common.Services.SystemClock>()
            .AddElsaAuthorization()
            .AddPermissionDescriptors<IdentityPermissionsDescriptorProvider>()
            .AddScoped<IUserCredentialsValidator, NameOnlyCredentialsValidator>()
            .AddFastEndpoints(options =>
            {
                options.Assemblies = [typeof(IdentityFeature).Assembly];
                options.Filter = x => x.Namespace is "Elsa.Identity.Endpoints.Login"
                    or "Elsa.Identity.Endpoints.RefreshToken"
                    or "Elsa.Identity.Endpoints.Me.Permissions"
                    or "Elsa.Identity.Endpoints.Users.List";
            });

        _app = builder.Build();
        _app.Services.GetRequiredService<MemoryStore<User>>().Save(NoPerm, x => x.Id);
        _app.Services.GetRequiredService<MemoryStore<User>>().Save(Reader, x => x.Id);
        _app.Services.GetRequiredService<MemoryStore<Role>>().Save(new Role { Id = "noperm", Name = "noperm" }, x => x.Id);
        _app.Services.GetRequiredService<MemoryStore<Role>>().Save(new Role { Id = "reader", Name = "reader", Permissions = ["identity/users:view"] }, x => x.Id);
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        FastEndpointsResolver.Reset();
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task LoginIssuesTheEmptySetSentinelForAZeroGrantUser()
    {
        var tokens = await LoginAsync(NoPerm);

        AssertKnownEmptySentinel(tokens.AccessToken);
        AssertKnownEmptySentinel(tokens.RefreshToken);
    }

    [Fact]
    public async Task RefreshReissuesTheEmptySetSentinel()
    {
        var refreshed = await RefreshTokensAsync((await LoginAsync(NoPerm)).RefreshToken);

        AssertKnownEmptySentinel(refreshed.AccessToken);
        AssertKnownEmptySentinel(refreshed.RefreshToken);
    }

    [Fact]
    public async Task AGrantedUserStillReceivesTheirPermissions()
    {
        var token = (await LoginAsync(Reader)).AccessToken;
        var payload = StudioWasmJwtParser.ReadPayload(token);

        Assert.Equal("identity/users:view", payload.GetProperty(PermissionNames.ClaimType).GetString());
        Assert.DoesNotContain(StudioWasmJwtParser.Parse(token), x => x.Type == PermissionNames.ClaimType && x.Value == PermissionNames.None);
    }

    [Fact]
    public async Task AZeroGrantUserIsForbiddenOnGatedEndpoints()
    {
        var tokens = await LoginAsync(NoPerm);

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAuthorizedAsync(HttpMethod.Get, "/identity/users", tokens.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAuthorizedAsync(HttpMethod.Get, "/identity/users", (await LoginAsync(Reader)).AccessToken)).StatusCode);
    }

    [Fact]
    public async Task MePermissionsReportsNoVerbsForAZeroGrantUser()
    {
        var response = await SendAuthorizedAsync(HttpMethod.Get, "/identity/me/permissions", (await LoginAsync(NoPerm)).AccessToken);
        var body = await response.Content.ReadFromJsonAsync<MePermissionsResponse>(JsonSerializerOptions.Web);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(body.Grants, x => x.Resource == IdentityPermissions.Users);
        Assert.All(body.Grants, grant => Assert.Empty(grant.Verbs));
    }

    [Fact]
    public async Task MePermissionsReportsHeldVerbsForAGrantedUser()
    {
        var response = await SendAuthorizedAsync(HttpMethod.Get, "/identity/me/permissions", (await LoginAsync(Reader)).AccessToken);
        var body = await response.Content.ReadFromJsonAsync<MePermissionsResponse>(JsonSerializerOptions.Web);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Grants, x => x.Resource == IdentityPermissions.Users && x.Verbs.Contains(CoreVerbs.View));
    }

    private static void AssertKnownEmptySentinel(string token)
    {
        var payload = StudioWasmJwtParser.ReadPayload(token);
        Assert.Equal(JsonValueKind.String, payload.GetProperty(PermissionNames.ClaimType).ValueKind);
        Assert.Equal(PermissionNames.None, payload.GetProperty(PermissionNames.ClaimType).GetString());

        var wasmClaims = StudioWasmJwtParser.Parse(token).Where(x => x.Type == PermissionNames.ClaimType).ToList();
        Assert.NotEmpty(wasmClaims);
        Assert.All(wasmClaims, claim => Assert.False(Permission.TryParse(claim.Value, out _)));
    }

    private async Task<IssuedTokens> LoginAsync(User user) =>
        await ReadTokensAsync(await _client.PostAsJsonAsync("/identity/login", new { username = user.Name, password = "unchecked" }));

    private async Task<IssuedTokens> RefreshTokensAsync(string refreshToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/identity/refresh-token");
        request.Headers.Authorization = new("Bearer", refreshToken);
        return await ReadTokensAsync(await _client.SendAsync(request));
    }

    private Task<HttpResponseMessage> SendAuthorizedAsync(HttpMethod method, string path, string accessToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new("Bearer", accessToken);
        return _client.SendAsync(request);
    }

    private static async Task<IssuedTokens> ReadTokensAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.True(body.IsAuthenticated);
        return new(body.AccessToken!, body.RefreshToken!);
    }

    private sealed record MePermissionsResponse(IReadOnlyCollection<MeResourceGrant> Grants);

    private sealed record MeResourceGrant(string Resource, IReadOnlyCollection<string> Verbs);

    private sealed class NameOnlyCredentialsValidator(IUserProvider userProvider) : IUserCredentialsValidator
    {
        public async ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default) =>
            await userProvider.FindByNameAsync(username, cancellationToken);
    }
}
