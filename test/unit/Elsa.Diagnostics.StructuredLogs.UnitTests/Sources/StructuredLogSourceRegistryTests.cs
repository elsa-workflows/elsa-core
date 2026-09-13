using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using System.Threading.Tasks;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.Sources;

// Mutates fixed process-wide environment variables used for structured-log source discovery.
[NotInParallel("ProcessEnvironment")]
public class StructuredLogSourceRegistryTests : IDisposable
{
    private readonly Dictionary<string, string?> _originalEnvironment = new();
    private readonly StructuredLogsOptions _options = new()
    {
        SourceHeartbeatTimeout = TimeSpan.FromSeconds(30)
    };

    public StructuredLogSourceRegistryTests()
    {
        CaptureEnvironment("HOSTNAME");
        CaptureEnvironment("OTEL_SERVICE_NAME");
        CaptureEnvironment("POD_NAMESPACE");
        CaptureEnvironment("CONTAINER_NAME");
        CaptureEnvironment("NODE_NAME");
    }

    [Test]
    public async Task Current_WhenKubernetesMetadataExists_UsesEnvironmentMetadata()
    {
        SetEnvironment("HOSTNAME", "elsa-pod-7");
        SetEnvironment("OTEL_SERVICE_NAME", "elsa-api");
        SetEnvironment("POD_NAMESPACE", "workflows");
        SetEnvironment("CONTAINER_NAME", "server");
        SetEnvironment("NODE_NAME", "node-a");

        var registry = CreateRegistry();

        await Assert.That(registry.Current.DisplayName).IsEqualTo("elsa-pod-7");
        await Assert.That(registry.Current.ServiceName).IsEqualTo("elsa-api");
        await Assert.That(registry.Current.Namespace).IsEqualTo("workflows");
        await Assert.That(registry.Current.ContainerName).IsEqualTo("server");
        await Assert.That(registry.Current.NodeName).IsEqualTo("node-a");
    }

    [Test]
    public async Task MarkSeen_WhenSourceIsUnknown_AddsSourceWithMatchingId()
    {
        var registry = CreateRegistry();

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);

        var source = await Assert.That(registry.List()).HasSingleItem(x => x.Id == "pod-b");
        await Assert.That(source.DisplayName).IsEqualTo("pod-b");
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
    }

    [Test]
    public async Task MarkSeen_WhenSourceIsUnknown_DoesNotCopyLocalContainerMetadata()
    {
        SetEnvironment("HOSTNAME", "local-pod");
        SetEnvironment("OTEL_SERVICE_NAME", "local-service");
        SetEnvironment("POD_NAMESPACE", "local-namespace");
        SetEnvironment("CONTAINER_NAME", "local-container");
        SetEnvironment("NODE_NAME", "local-node");
        var registry = CreateRegistry();

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);

        var source = await Assert.That(registry.List()).HasSingleItem(x => x.Id == "pod-b");
        await Assert.That(source.MachineName).IsEqualTo("pod-b");
        await Assert.That(source.ProcessId).IsEqualTo(0);
        await Assert.That(source.ServiceName).IsNull();
        await Assert.That(source.PodName).IsNull();
        await Assert.That(source.Namespace).IsNull();
        await Assert.That(source.ContainerName).IsNull();
        await Assert.That(source.NodeName).IsNull();
    }

    [Test]
    public async Task MarkSeen_WhenSourceIsUnknown_RaisesSourceChanged()
    {
        var registry = CreateRegistry();
        StructuredLogSource? changedSource = null;
        registry.SourceChanged += source => changedSource = source;

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);

        var source = await Assert.That(changedSource).IsNotNull();
        await Assert.That(source.Id).IsEqualTo("pod-b");
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
    }

    [Test]
    public async Task MarkSeen_WhenSourceIsKnown_DoesNotRaiseSourceChanged()
    {
        var registry = CreateRegistry();
        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow);
        StructuredLogSource? changedSource = null;
        registry.SourceChanged += source => changedSource = source;

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow.AddSeconds(1));

        await Assert.That(changedSource).IsNull();
    }

    [Test]
    public async Task List_WhenSourceHasNotBeenSeenRecently_MarksSourceAsStale()
    {
        _options.SourceHeartbeatTimeout = TimeSpan.FromSeconds(5);
        var registry = CreateRegistry();

        registry.MarkSeen("pod-b", DateTimeOffset.UtcNow.AddMinutes(-1));

        var source = await Assert.That(registry.List()).HasSingleItem(x => x.Id == "pod-b");
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Stale);
    }

    public void Dispose()
    {
        foreach (var item in _originalEnvironment)
            Environment.SetEnvironmentVariable(item.Key, item.Value);
    }

    private StructuredLogSourceRegistry CreateRegistry()
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
