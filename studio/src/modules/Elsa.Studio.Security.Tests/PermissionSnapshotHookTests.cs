using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Authentication.ElsaIdentity;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Services;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Models;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elsa.Studio.Security.Tests;

/// <summary>
/// Permission-dependent UI must re-fetch after the snapshot is dropped, regardless of which
/// object constructed the cache first. Removing the sign-in subscription, the environment
/// <see cref="IPermissionRefreshSignal"/> raise, or the JWT refresh raise fails these tests.
/// </summary>
public sealed class PermissionSnapshotHookTests : BunitContext, IAsyncLifetime
{
    private const string AdminAction = "delete-user";
    private readonly SwitchableMePermissionsApi _api = new();
    private readonly NotifyingAuthenticationStateProvider _authentication = new();
    private readonly PermissionRefreshSignal _signal = new();
    private readonly IdentityPermissionContext _context;

    public PermissionSnapshotHookTests()
    {
        _api.Grants = [new CurrentCallerResourceGrant { Resource = "identity/users", Verbs = ["view"] }];
        _context = new IdentityPermissionContext(
            new StaticBackendApiClientProvider(_api),
            NullLogger<IdentityPermissionContext>.Instance,
            [_authentication],
            [_signal]);

        Services.AddSingleton<IPermissionRefreshSignal>(_signal);
        Services.AddSingleton<AuthenticationStateProvider>(_authentication);
        Services.AddSingleton<IPermissionSnapshotCache>(_context);
        Services.AddSingleton<IPermissionService>(new IdentityPermissionService(_context));
    }

    [Fact]
    public void PermissionView_AfterSignInChangesToZeroGrant_HidesTheAdminAction()
    {
        var cut = RenderAdminAction();
        cut.WaitForAssertion(() => Assert.Contains(AdminAction, cut.Markup));

        _api.Grants = [];
        _authentication.Notify();

        cut.WaitForAssertion(() => Assert.DoesNotContain(AdminAction, cut.Markup));
        Assert.Equal(2, _api.Calls);
    }

    [Fact]
    public void PermissionView_AfterEnvironmentSwitch_ReloadsGrants()
    {
        var environments = new DefaultEnvironmentService([_signal]);
        environments.SetEnvironments(
        [
            new ServerEnvironment { Name = "Dev", Url = new Uri("https://dev.example/") },
            new ServerEnvironment { Name = "Prod", Url = new Uri("https://prod.example/") }
        ], "Dev");

        var cut = RenderAdminAction();
        cut.WaitForAssertion(() => Assert.Contains(AdminAction, cut.Markup));

        _api.Grants = [];
        environments.SetCurrentEnvironment("Prod");

        cut.WaitForAssertion(() => Assert.DoesNotContain(AdminAction, cut.Markup));
        Assert.Equal(2, _api.Calls);
    }

    [Fact]
    public async Task PermissionView_AfterSilentTokenRefresh_ReloadsGrants()
    {
        var tokens = new MemoryJwtAccessor();
        tokens.Tokens[TokenNames.AccessToken] = CreateJwt(TimeSpan.FromMinutes(-5), "user-1", "identity/users:view");
        tokens.Tokens[TokenNames.RefreshToken] = "refresh-token";
        var provider = new JwtTokenProvider(
            tokens,
            new JwtParser(),
            new SingleFlightCoordinator(),
            new WritingRefreshTokenService(tokens, CreateJwt(TimeSpan.FromMinutes(15), "user-1")),
            [_signal]);

        var cut = RenderAdminAction();
        cut.WaitForAssertion(() => Assert.Contains(AdminAction, cut.Markup));

        _api.Grants = [];
        await provider.GetAccessTokenAsync();

        cut.WaitForAssertion(() => Assert.DoesNotContain(AdminAction, cut.Markup));
        Assert.Equal(2, _api.Calls);
    }

    [Fact]
    public async Task GetAsync_WhenNearExpiryRefreshKeepsSubAndPermissions_DoesNotLoop()
    {
        var tokens = new MemoryJwtAccessor();
        var nearExpiry = CreateJwt(TimeSpan.FromSeconds(90), "user-1", "identity/users:view");
        tokens.Tokens[TokenNames.AccessToken] = nearExpiry;
        tokens.Tokens[TokenNames.RefreshToken] = "refresh-token";
        var raised = 0;
        _signal.Raised += () => raised++;
        var provider = new JwtTokenProvider(
            tokens,
            new JwtParser(),
            new SingleFlightCoordinator(),
            new WritingRefreshTokenService(tokens, CreateJwt(TimeSpan.FromSeconds(90), "user-1", "identity/users:view")),
            [_signal]);
        _api.OnGet = () => provider.GetAccessTokenAsync();

        var snapshot = await _context.GetAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(IdentityPermissionSnapshotState.Ready, snapshot.State);
        Assert.True(snapshot.HasPermission("identity/users", "view"));
        Assert.Equal(1, _api.Calls);
        Assert.Equal(0, raised);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        _context.Dispose();
        await base.DisposeAsync();
    }

    private IRenderedComponent<PermissionView> RenderAdminAction() =>
        Render<PermissionView>(parameters => parameters
            .Add(x => x.Resource, "identity/users")
            .Add(x => x.Verb, "view")
            .AddChildContent(AdminAction));

    private static string CreateJwt(TimeSpan expiresIn, string? sub = null, params string[] permissions)
    {
        var payload = new Dictionary<string, object?>
        {
            ["exp"] = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeSeconds()
        };
        if (sub != null)
            payload["sub"] = sub;
        if (permissions.Length == 1)
            payload["permissions"] = permissions[0];
        else if (permissions.Length > 1)
            payload["permissions"] = permissions;

        return $"{Base64Url("{\"alg\":\"none\"}")}.{Base64Url(JsonSerializer.Serialize(payload))}.signature";
    }

    private static string Base64Url(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class SwitchableMePermissionsApi : IMePermissionsApi
    {
        public IReadOnlyList<CurrentCallerResourceGrant> Grants { get; set; } = [];
        public int Calls { get; private set; }
        public Func<Task>? OnGet { get; set; }

        public async Task<CurrentCallerPermissionsResponse> GetAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            if (OnGet != null)
                await OnGet();
            return new CurrentCallerPermissionsResponse { Grants = Grants.ToArray() };
        }
    }

    private sealed class NotifyingAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state = new(new(new System.Security.Claims.ClaimsIdentity("test")));

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);

        public void Notify() => NotifyAuthenticationStateChanged(Task.FromResult(_state));
    }

    private sealed class MemoryJwtAccessor : IJwtAccessor
    {
        public Dictionary<string, string> Tokens { get; } = new();

        public ValueTask<string?> ReadTokenAsync(string name) => ValueTask.FromResult(Tokens.GetValueOrDefault(name));

        public ValueTask WriteTokenAsync(string name, string token)
        {
            Tokens[name] = token;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearTokenAsync(string name)
        {
            Tokens.Remove(name);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class WritingRefreshTokenService(MemoryJwtAccessor tokens, string accessToken) : IRefreshTokenService
    {
        public async Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken)
        {
            await tokens.WriteTokenAsync(TokenNames.AccessToken, accessToken);
            await tokens.WriteTokenAsync(TokenNames.RefreshToken, "new-refresh");
            return new LoginResponse(true, accessToken, "new-refresh");
        }
    }
}
