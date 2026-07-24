using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Stores.InMemory;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class InMemoryOperationalStoresTests
{
    private readonly DateTimeOffset _now = new(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ObservationStoreKeepsTheMostRecentResultPerConnection()
    {
        var store = new InMemoryConnectionObservationStore();
        var latest = CreateObservation(_now.AddMinutes(2), "latest");

        await store.SaveLatestAsync(latest);
        await store.SaveLatestAsync(CreateObservation(_now.AddMinutes(1), "older"));

        var result = await store.FindLatestAsync("connection-a");

        Assert.Same(latest, result);
    }

    [Fact]
    public async Task RegistryVersionStoreCreatesAMonotonicBarrier()
    {
        var store = new InMemoryConnectionRegistryVersionStore();
        var initial = await store.GetVersionAsync();
        var advanced = await store.AdvanceAsync();

        Assert.Equal(initial + 1, advanced);
        Assert.False(await store.IsCurrentAsync(initial));
        Assert.True(await store.IsCurrentAsync(advanced));
    }

    private static ConnectionObservation CreateObservation(DateTimeOffset observedAt, string summary) => new(
        "connection-a",
        "revision-a",
        observedAt,
        ConnectionObservationStatus.Succeeded,
        "reachable",
        TimeSpan.FromMilliseconds(1),
        summary,
        [],
        "correlation-a");
}
