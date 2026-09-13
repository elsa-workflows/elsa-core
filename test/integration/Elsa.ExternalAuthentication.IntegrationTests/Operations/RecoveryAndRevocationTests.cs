using Elsa.Authorization;
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
    [Test]
    public async Task FinalNormalConnectionRequiresBreakGlassOrConfirmedPrivilegedOverride()
    {
        var registry = Substitute.For<IIdentityProviderConnectionRegistry>();
        registry.GetAsync("tenant-a", Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new EffectiveConnectionRegistry([], [], "1")));
        var options = Microsoft.Extensions.Options.Options.Create(new ExternalAuthenticationOptions { LocalLogin = new LocalLoginMethodOptions { IsEnabled = false } });
        var guard = new FinalLoginPathGuard(registry, options, new PermissionEvaluator());
        var existing = Connection(enabled: true);
        var disabled = Connection(enabled: false);

        await Assert.That(await guard.AuthorizeAsync(existing, disabled, "tenant-a", new ClaimsPrincipal(new ClaimsIdentity()), false)).IsEqualTo(FinalLoginPathGuardResult.Denied);
        var privileged = new ClaimsPrincipal(new ClaimsIdentity([new Claim(Elsa.PermissionNames.ClaimType, options.Value.FinalLoginPathGuard.PrivilegedOverridePermission)]));
        await Assert.That(await guard.AuthorizeAsync(existing, disabled, "tenant-a", privileged, true)).IsEqualTo(FinalLoginPathGuardResult.Allowed);
        options.Value.FinalLoginPathGuard.HasBreakGlassAuthentication = true;
        await Assert.That(await guard.AuthorizeAsync(existing, disabled, "tenant-a", new ClaimsPrincipal(), false)).IsEqualTo(FinalLoginPathGuardResult.Allowed);
    }

    [Test]
    public async Task RevokedExternalSessionCannotRemainActive()
    {
        var now = DateTimeOffset.UtcNow;
        var store = new InMemoryExternalAuthenticationSessionStore(new TestClock(now));
        var session = new ExternalAuthenticationSession { Id = "s", TenantId = "tenant-a", UserId = "u", ConnectionKey = "idp", AuthenticationClientId = "client", ConnectionMaterialRevision = "r", Issuer = "https://issuer", SubjectHash = "h", StartedAt = now, LastRefreshedAt = now, ExpiresAt = now.AddHours(1), RefreshExpiresAt = now.AddHours(1), CurrentRefreshTokenHash = "refresh" };
        await store.SaveAsync(session);
        await Assert.That(await store.RevokeAsync(session.Id, "administrator_revoked", now)).IsTrue();
        await Assert.That(await store.FindAsync(new ExternalAuthenticationSessionFilter { TenantId = "tenant-a", Status = "revoked" })).HasSingleItem();
    }

    private static IdentityProviderConnection Connection(bool enabled) => new() { Id = "connection-a", TenantId = "tenant-a", Key = "idp", AdapterType = "test", DisplayName = "IdP", IsEnabled = enabled };
    private sealed class TestClock(DateTimeOffset now) : Elsa.Common.ISystemClock { public DateTimeOffset UtcNow { get; set; } = now; }
}
