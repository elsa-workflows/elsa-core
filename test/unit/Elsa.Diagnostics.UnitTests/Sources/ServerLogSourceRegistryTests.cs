using Elsa.Diagnostics.Models;
using Elsa.Diagnostics.Options;
using Elsa.Diagnostics.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.UnitTests.Sources;

public class ServerLogSourceRegistryTests : IDisposable
{
    private readonly Dictionary<string, string?> _originalEnvironment = new();
    private readonly ServerLogStreamingOptions _options = new()
    {
        SourceHeartbeatTimeout = TimeSpan.FromSeconds(30)
    };

    public ServerLogSourceRegistryTests()
    {
        CaptureEnvironment("HOSTNAME");
        CaptureEnvironment("OTEL_SERVICE_NAME");
        CaptureEnvironment("POD_NAMESPACE");
        CaptureEnvironment("CONTAINER_NAME");
        CaptureEnvironment("NODE_NAME");
    }

    [Fact]
    public void Current_WhenKubernetesMetadataExists_UsesEnvironmentMetadata()
    {
        SetEnvironment("HOSTNAME", "elsa-pod-7");
        SetEnvironment("OTEL_SERVICE_NAME", "elsa-api");
        SetEnvironment("POD_NAMESPACE", "workflows");
        SetEnvironment("CONTAINER_NAME", "server");
        SetEnvironment("NODE_NAME", "node-a");

        var registry = CreateRegistry();

        Assert.Equal("elsa-pod-7", registry.Current.DisplayName);
        Assert.Equal("elsa-api", registry.Current.ServiceName);
        Assert.Equal("workflows", registry.Current.Namespace);
        Assert.Equal("server", registry.Current.ContainerName);
        Assert.Equal("node-a", registry.Current.NodeName);
    }

    [Fact]
    public void MarkSeen_WhenSourceIsUnknown_AddsSourceWithMatchingId()
    {
        var registry = CreateRegistry();

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);

        var source = Assert.Single(registry.List(), x => x.Id == "pod-b");
        Assert.Equal("pod-b", source.DisplayName);
        Assert.Equal(ServerLogSourceStatus.Connected, source.Status);
    }

    [Fact]
    public void MarkSeen_WhenSourceIsUnknown_DoesNotCopyLocalContainerMetadata()
    {
        SetEnvironment("HOSTNAME", "local-pod");
        SetEnvironment("OTEL_SERVICE_NAME", "local-service");
        SetEnvironment("POD_NAMESPACE", "local-namespace");
        SetEnvironment("CONTAINER_NAME", "local-container");
        SetEnvironment("NODE_NAME", "local-node");
        var registry = CreateRegistry();

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);

        var source = Assert.Single(registry.List(), x => x.Id == "pod-b");
        Assert.Equal("pod-b", source.MachineName);
        Assert.Equal(0, source.ProcessId);
        Assert.Null(source.ServiceName);
        Assert.Null(source.PodName);
        Assert.Null(source.Namespace);
        Assert.Null(source.ContainerName);
        Assert.Null(source.NodeName);
    }

    [Fact]
    public void MarkSeen_WhenSourceIsUnknown_RaisesSourceChanged()
    {
        var registry = CreateRegistry();
        ServerLogSource? changedSource = null;
        registry.SourceChanged += source => changedSource = source;

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);

        Assert.NotNull(changedSource);
        Assert.Equal("pod-b", changedSource.Id);
        Assert.Equal(ServerLogSourceStatus.Connected, changedSource.Status);
    }

    [Fact]
    public void MarkSeen_WhenSourceIsKnown_DoesNotRaiseSourceChanged()
    {
        var registry = CreateRegistry();
        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);
        ServerLogSource? changedSource = null;
        registry.SourceChanged += source => changedSource = source;

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.Null(changedSource);
    }

    [Fact]
    public void List_WhenSourceHasNotBeenSeenRecently_MarksSourceAsStale()
    {
        _options.SourceHeartbeatTimeout = TimeSpan.FromSeconds(5);
        var registry = CreateRegistry();

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow.AddMinutes(-1));

        var source = Assert.Single(registry.List(), x => x.Id == "pod-b");
        Assert.Equal(ServerLogSourceStatus.Stale, source.Status);
    }

    public void Dispose()
    {
        foreach (var item in _originalEnvironment)
            Environment.SetEnvironmentVariable(item.Key, item.Value);
    }

    private ServerLogSourceRegistry CreateRegistry()
    {
        return new(MicrosoftOptions.Create(_options));
    }

    private void CaptureEnvironment(string name)
    {
        _originalEnvironment[name] = Environment.GetEnvironmentVariable(name);
    }

    private static void SetEnvironment(string name, string? value)
    {
        Environment.SetEnvironmentVariable(name, value);
    }
}
