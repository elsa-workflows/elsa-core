namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsSourcesEndpointTests
{
    [Test]
    public async Task SourcesEndpoint_ExistsInConsoleLogsAssembly()
    {
        var endpointType = typeof(Features.ConsoleLogsFeature)
            .Assembly
            .GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Sources.Endpoint");

        await Assert.That(endpointType).IsNotNull();
    }
}
