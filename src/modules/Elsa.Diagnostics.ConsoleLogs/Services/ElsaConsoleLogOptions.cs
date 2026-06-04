using ConsoleLogStreaming.Core.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal static class ElsaConsoleLogOptions
{
    public static void ConfigureDefaults(ConsoleLogOptions options)
    {
        var sourceId = $"{Environment.MachineName}-{Environment.ProcessId}";
        var podName = Environment.GetEnvironmentVariable("HOSTNAME");

        options.SourceId = sourceId;
        options.SourceDisplayName = !string.IsNullOrWhiteSpace(podName) ? podName : sourceId;
        options.ServiceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? AppDomain.CurrentDomain.FriendlyName;
        options.PreserveAnsi = true;
        options.RecentCapacity = 2_000;
        options.MaxRecentQuerySize = 250;

        SetMetadata(options, "kubernetes.pod.name", podName);
        SetMetadata(options, "kubernetes.namespace.name", Environment.GetEnvironmentVariable("POD_NAMESPACE"));
        SetMetadata(options, "container.name", Environment.GetEnvironmentVariable("CONTAINER_NAME"));
        SetMetadata(options, "kubernetes.node.name", Environment.GetEnvironmentVariable("NODE_NAME"));
        SetMetadata(options, "process.started_at", DateTimeOffset.UtcNow.ToString("O"));
    }

    private static void SetMetadata(ConsoleLogOptions options, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            options.SourceMetadata[key] = value;
    }
}
