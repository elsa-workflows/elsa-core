using System.Security.Claims;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Services;
using Elsa.ExternalAuthentication.Stores.InMemory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

public class RecoveryAndRevocationTests
{
    [Fact]
    public async Task FinalNormalConnectionRequiresBreakGlassOrConfirmedPrivilegedOverride()
    {
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.GetAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new EffectiveConnectionRegistry([], [], "1")));
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions { LocalLogin = new LocalLoginMethodOptions { IsEnabled = false } });
        var guard = new FinalLoginPathGuard(registry, options);
        var existing = Connection(enabled: true);
        var disabled = Connection(enabled: false);

        Assert.Equal(FinalLoginPathGuardResult.Denied, await guard.AuthorizeAsync(existing, disabled, "tenant-a", new ClaimsPrincipal(new ClaimsIdentity()), false));
        var privileged = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Elsa.PermissionNames.ClaimType, Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationPermissions.ProviderTrustUnsafe)]));
        Assert.Equal(FinalLoginPathGuardResult.Allowed, await guard.AuthorizeAsync(existing, disabled, "tenant-a", privileged, true));
        options.Value.FinalLoginPathGuard.HasBreakGlassAuthentication = true;
        Assert.Equal(FinalLoginPathGuardResult.Allowed, await guard.AuthorizeAsync(existing, disabled, "tenant-a", new ClaimsPrincipal(), false));
    }

    [Fact]
    public async Task RevokedExternalSessionCannotRemainActive()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new InMemoryExternalAuthenticationSessionStore(new TestClock(now));
        var session = new ExternalAuthenticationSession { Id = "s", TenantId = "tenant-a", UserId = "u", ConnectionId = "connection-a", AuthenticationClientId = "client", ConnectionMaterialRevision = "r", Issuer = "https://issuer", SubjectHash = "h", StartedAt = now, LastRefreshedAt = now, ExpiresAt = now.AddHours(1), RefreshExpiresAt = now.AddHours(1), CurrentRefreshTokenHash = "refresh" };
        await store.SaveAsync(session);
        Assert.True(await store.RevokeAsync(session.Id, "administrator_revoked", now));
        Assert.Single(await store.FindAsync(new ExternalAuthenticationSessionFilter { TenantId = "tenant-a", Status = "revoked" }));
    }

    private static IdentityProviderConnection Connection(bool enabled) => new() { Id = "connection-a", TenantId = "tenant-a", Key = "idp", AdapterType = "test", DisplayName = "IdP", IsEnabled = enabled };
    private sealed class TestClock(DateTimeOffset now) : Elsa.Common.ISystemClock { public DateTimeOffset UtcNow { get; set; } = now; }
}
