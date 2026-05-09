using Elsa.ServerLogs.Features;

namespace Elsa.ServerLogs.IntegrationTests;

public class ServerLogsModuleTests
{
    [Fact]
    public void ServerLogStreamingFeature_BelongsToServerLogsAssembly()
    {
        Assert.Equal("Elsa.ServerLogs", typeof(ServerLogStreamingFeature).Assembly.GetName().Name);
    }
}
