using System.Diagnostics;
using Elsa.Diagnostics.StructuredLogs.Models;
using Elsa.Diagnostics.StructuredLogs.Options;
using Elsa.Diagnostics.StructuredLogs.Services;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Diagnostics.StructuredLogs.UnitTests.Sources;

public class StructuredLogSourceRegistryTests
{
    private const string IsolatedTestMarker = "ELSA_STRUCTURED_LOG_SOURCE_REGISTRY_ISOLATED_TEST";
    private readonly StructuredLogsOptions _options = new()
    {
        SourceHeartbeatTimeout = TimeSpan.FromSeconds(30)
    };

    [Test]
    public async Task Current_WhenKubernetesMetadataExists_UsesEnvironmentMetadata()
    {
        const string testName = nameof(Current_WhenKubernetesMetadataExists_UsesEnvironmentMetadata);
        if (!IsIsolatedTestProcess(testName))
        {
            await RunIsolatedTestAsync(testName, new Dictionary<string, string>
            {
                ["HOSTNAME"] = "elsa-pod-7",
                ["OTEL_SERVICE_NAME"] = "elsa-api",
                ["POD_NAMESPACE"] = "workflows",
                ["CONTAINER_NAME"] = "server",
                ["NODE_NAME"] = "node-a"
            });
            return;
        }

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
        const string testName = nameof(MarkSeen_WhenSourceIsUnknown_DoesNotCopyLocalContainerMetadata);
        if (!IsIsolatedTestProcess(testName))
        {
            await RunIsolatedTestAsync(testName, new Dictionary<string, string>
            {
                ["HOSTNAME"] = "local-pod",
                ["OTEL_SERVICE_NAME"] = "local-service",
                ["POD_NAMESPACE"] = "local-namespace",
                ["CONTAINER_NAME"] = "local-container",
                ["NODE_NAME"] = "local-node"
            });
            return;
        }

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

        await Assert.That(changedSource).IsNotNull();
        await Assert.That(changedSource.Id).IsEqualTo("pod-b");
        await Assert.That(changedSource.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
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
    public async Task MarkSeen_WhenTimestampIsOlder_DoesNotRegressLastSeen()
    {
        var registry = CreateRegistry();
        var newer = DateTimeOffset.UtcNow;
        var older = newer.AddMinutes(-1);

        registry.MarkSeen("pod-b", newer);
        registry.MarkSeen("pod-b", older);

        var source = await Assert.That(registry.List()).HasSingleItem(x => x.Id == "pod-b");
        await Assert.That(source.LastSeen).IsEqualTo(newer);
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
    }

    [Test]
    public async Task MarkSeen_WhenTimestampIsEqual_KeepsLastSeen()
    {
        var registry = CreateRegistry();
        var timestamp = DateTimeOffset.UtcNow;

        registry.MarkSeen("pod-b", timestamp);
        registry.MarkSeen("pod-b", timestamp);

        var source = await Assert.That(registry.List()).HasSingleItem(x => x.Id == "pod-b");
        await Assert.That(source.LastSeen).IsEqualTo(timestamp);
        await Assert.That(source.Status).IsEqualTo(StructuredLogSourceStatus.Connected);
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

    private static bool IsIsolatedTestProcess(string testName)
    {
        return string.Equals(Environment.GetEnvironmentVariable(IsolatedTestMarker), testName, StringComparison.Ordinal);
    }

    private static async Task RunIsolatedTestAsync(string testName, IReadOnlyDictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(typeof(StructuredLogSourceRegistryTests).Assembly.Location);
        startInfo.ArgumentList.Add("--treenode-filter");
        startInfo.ArgumentList.Add($"/*/*/{nameof(StructuredLogSourceRegistryTests)}/{testName}");
        startInfo.Environment[IsolatedTestMarker] = testName;

        foreach (var (name, value) in environment)
            startInfo.Environment[name] = value;

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
            throw new InvalidOperationException($"Could not start isolated test process for {testName}.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        await Assert.That(process.ExitCode)
            .IsEqualTo(0)
            .Because($"Isolated test process for {testName} failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
    }

    private StructuredLogSourceRegistry CreateRegistry()
    {
        return new(MicrosoftOptions.Create(_options));
    }
}
