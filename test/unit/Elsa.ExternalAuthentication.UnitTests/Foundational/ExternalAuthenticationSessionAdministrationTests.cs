using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationSessionAdministrationTests
{
    [Test]
    public async Task ListsOnlyRequestedTenantAndSafeStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var clock = new TestSystemClock(now);
        var store = new InMemoryExternalAuthenticationSessionStore(clock);
        var active = ExternalAuthenticationTestData.CreateSession(now);
        await store.SaveAsync(active);
        var other = ExternalAuthenticationTestData.CreateSession(now);
        other.Id = "session-b";
        other.TenantId = "tenant-b";
        other.CurrentRefreshTokenHash = "refresh-2";
        await store.SaveAsync(other);
        await store.RevokeAsync(active.Id, "test", now);

        var revoked = await store.FindAsync(new ExternalAuthenticationSessionFilter { TenantId = "tenant-a", Status = "revoked" });
        var activeSessions = await store.FindAsync(new ExternalAuthenticationSessionFilter { TenantId = "tenant-a", Status = "active" });

        await Assert.That(revoked).HasSingleItem();
        await Assert.That(activeSessions).IsEmpty();
        await Assert.That(revoked.Single().Id).IsEqualTo("session-a");
    }
}
