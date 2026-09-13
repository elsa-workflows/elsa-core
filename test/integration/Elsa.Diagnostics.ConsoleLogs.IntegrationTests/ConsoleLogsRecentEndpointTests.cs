namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsRecentEndpointTests
{
    [Test]
    public async Task RecentEndpoint_ExistsInConsoleLogsAssembly()
    {
        var endpointType = typeof(Features.ConsoleLogsFeature)
            .Assembly
            .GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint");

        await Assert.That(endpointType).IsNotNull();
    }
}
