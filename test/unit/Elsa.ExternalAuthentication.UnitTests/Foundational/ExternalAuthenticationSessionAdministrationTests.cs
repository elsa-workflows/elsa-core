using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class ExternalAuthenticationSessionAdministrationTests
{
    [Fact]
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
        await store.SaveAsync(other);
        await store.RevokeAsync(active.Id, "test", now);

        var revoked = await store.FindAsync(new ExternalAuthenticationSessionFilter { TenantId = "tenant-a", Status = "revoked" });
        var activeSessions = await store.FindAsync(new ExternalAuthenticationSessionFilter { TenantId = "tenant-a", Status = "active" });

        Assert.Single(revoked);
        Assert.Empty(activeSessions);
        Assert.Equal("session-a", revoked.Single().Id);
    }
}
