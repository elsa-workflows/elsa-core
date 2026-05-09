using Elsa.ServerLogs.Features;
using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using Elsa.ServerLogs.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    [Fact]
    public void ShellServerLogStreamingFeature_CopiesBindablePropertiesToOptions()
    {
        var feature = new ShellServerLogStreamingFeature
        {
            RecentLogCapacity = 123,
            SubscriberChannelCapacity = 45,
            MaxRecentLogQuerySize = 67,
            SourceHeartbeatTimeout = TimeSpan.FromSeconds(89),
            IncludeServerLogsInternalLogs = true,
            SensitiveNames = ["credential"],
            SensitiveTextPatterns = ["(?i)credential=([^\\s]+)"]
        };
        var services = new ServiceCollection();

        feature.ConfigureServices(services);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<ServerLogStreamingOptions>>().Value;
        Assert.Equal(feature.RecentLogCapacity, options.RecentLogCapacity);
        Assert.Equal(feature.SubscriberChannelCapacity, options.SubscriberChannelCapacity);
        Assert.Equal(feature.MaxRecentLogQuerySize, options.MaxRecentLogQuerySize);
        Assert.Equal(feature.SourceHeartbeatTimeout, options.SourceHeartbeatTimeout);
        Assert.Equal(feature.IncludeServerLogsInternalLogs, options.IncludeServerLogsInternalLogs);
        Assert.Equal(feature.SensitiveNames, options.SensitiveNames);
        Assert.Equal(feature.SensitiveTextPatterns, options.SensitiveTextPatterns);
    }
}
