using Elsa.ServerLogs.Features;
using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using ShellServerLogStreamingFeature = Elsa.ServerLogs.ShellFeatures.ServerLogStreamingFeature;

namespace Elsa.ServerLogs.IntegrationTests;

public class ServerLogsModuleTests
{
    [Fact]
    public void ServerLogStreamingFeature_BelongsToServerLogsAssembly()
    {
        Assert.Equal("Elsa.ServerLogs", typeof(ServerLogStreamingFeature).Assembly.GetName().Name);
    }

    [Fact]
    public void ShellServerLogStreamingFeature_BelongsToServerLogsAssembly()
    {
        Assert.Equal("Elsa.ServerLogs", typeof(ShellServerLogStreamingFeature).Assembly.GetName().Name);
    }

    [Fact]
    public void ShellServerLogStreamingFeature_RegistersFastEndpointsAndWebEndpointMapping()
    {
        Assert.True(typeof(IFastEndpointsShellFeature).IsAssignableFrom(typeof(ShellServerLogStreamingFeature)));
        Assert.True(typeof(IWebShellFeature).IsAssignableFrom(typeof(ShellServerLogStreamingFeature)));
    }
}
