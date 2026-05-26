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

        SetMetadata(options, ConsoleLogStreamingApiMetadataKeys.KubernetesPodName, podName);
        SetMetadata(options, ConsoleLogStreamingApiMetadataKeys.KubernetesNamespace, Environment.GetEnvironmentVariable("POD_NAMESPACE"));
        SetMetadata(options, ConsoleLogStreamingApiMetadataKeys.ContainerName, Environment.GetEnvironmentVariable("CONTAINER_NAME"));
        SetMetadata(options, ConsoleLogStreamingApiMetadataKeys.KubernetesNodeName, Environment.GetEnvironmentVariable("NODE_NAME"));
        SetMetadata(options, ConsoleLogStreamingApiMetadataKeys.ProcessStartedAt, DateTimeOffset.UtcNow.ToString("O"));
    }

    private static void SetMetadata(ConsoleLogOptions options, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            options.SourceMetadata[key] = value;
    }
}
