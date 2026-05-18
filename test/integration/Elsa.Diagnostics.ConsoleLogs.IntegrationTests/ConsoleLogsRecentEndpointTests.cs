namespace Elsa.Diagnostics.ConsoleLogs.IntegrationTests;

public class ConsoleLogsRecentEndpointTests
{
    [Fact]
    public void RecentEndpoint_ExistsInConsoleLogsAssembly()
    {
        var endpointType = typeof(Elsa.Diagnostics.ConsoleLogs.Features.ConsoleLogsFeature)
            .Assembly
            .GetType("Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent.Endpoint");

        Assert.NotNull(endpointType);
    }
}
