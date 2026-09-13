using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.IntegrationTests.Operations;

public class ConnectionTestTests
{
    [Test]
    public async Task LatestObservationIsSharedAndBecomesStaleWhenMaterialRevisionChanges()
    {
        var store = new InMemoryConnectionObservationStore();
        await store.SaveLatestAsync(new ConnectionObservation("connection-a", "revision-1", DateTimeOffset.UtcNow, ConnectionObservationStatus.Succeeded, "reachable", TimeSpan.FromMilliseconds(10), "Provider metadata was resolved.", [], "correlation"));

        var latest = await store.FindLatestAsync("connection-a");

        await Assert.That(latest).IsNotNull();
        await Assert.That(latest!.TestedMaterialRevision).IsEqualTo("revision-1");
        await Assert.That(latest.TestedMaterialRevision).IsNotEqualTo("revision-2"); // Management projections mark this revision mismatch stale.
        await Assert.That(latest.Summary).DoesNotContain("secret").WithComparison(StringComparison.OrdinalIgnoreCase);
    }
}
