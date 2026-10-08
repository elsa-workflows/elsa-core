namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsSourcesEndpointTests
{
    [Fact]
    public void SourcesEndpoint_ExistsInConsoleLogsAssembly()
    {
        var endpointType = typeof(Features.ConsoleLogsFeature)
            .Assembly
            .GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Sources.Endpoint");

        Assert.NotNull(endpointType);
    }
}
