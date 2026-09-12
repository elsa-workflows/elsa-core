using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

public class ConnectionTestTests
{
    [Fact]
    public async Task LatestObservationIsSharedAndBecomesStaleWhenMaterialRevisionChanges()
    {
        var store = new InMemoryConnectionObservationStore();
        await store.SaveLatestAsync(new ConnectionObservation("connection-a", "revision-1", DateTimeOffset.UtcNow, ConnectionObservationStatus.Succeeded, "reachable", TimeSpan.FromMilliseconds(10), "Provider metadata was resolved.", [], "correlation"));

        var latest = await store.FindLatestAsync("connection-a");

        Assert.NotNull(latest);
        Assert.Equal("revision-1", latest!.TestedMaterialRevision);
        Assert.NotEqual("revision-2", latest.TestedMaterialRevision); // Management projections mark this revision mismatch stale.
        Assert.DoesNotContain("secret", latest.Summary, StringComparison.OrdinalIgnoreCase);
    }
}
