using CShells.AspNetCore.Features;
using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Diagnostics.OpenTelemetry.Extensions;
using Elsa.Diagnostics.OpenTelemetry.Options;
using Elsa.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.OpenTelemetry.ShellFeatures;

[ManifestFeatureCategory(ManifestFeatureCategories.Diagnostics)]
[ShellFeature(
    DisplayName = "OpenTelemetry Diagnostics",
    Description = "Provides OpenTelemetry diagnostics collection, query services, and live updates",
    DependsOn = [typeof(ElsaFastEndpointsFeature)])]
[UsedImplicitly]
public class OpenTelemetryFeature : IFastEndpointsShellFeature, IWebShellFeature
{
    private static readonly OpenTelemetryDiagnosticsOptions DefaultOptions = new();

    [ManifestSetting(
        DisplayName = "Trace Capacity",
        Description = "Maximum number of recent traces retained in memory.",
        Category = "Diagnostics",
        DefaultValue = "5000",
        RestartRequired = true)]
    public int TraceCapacity { get; set; } = DefaultOptions.TraceCapacity;

    [ManifestSetting(
        DisplayName = "Span Capacity",
        Description = "Maximum number of recent spans retained in memory.",
        Category = "Diagnostics",
        DefaultValue = "25000",
        RestartRequired = true)]
    public int SpanCapacity { get; set; } = DefaultOptions.SpanCapacity;

    [ManifestSetting(
        DisplayName = "Metric Point Capacity",
        Description = "Maximum number of recent metric points retained in memory.",
        Category = "Diagnostics",
        DefaultValue = "25000",
        RestartRequired = true)]
    public int MetricPointCapacity { get; set; } = DefaultOptions.MetricPointCapacity;

    [ManifestSetting(
        DisplayName = "Log Record Capacity",
        Description = "Maximum number of recent OTLP log records retained in memory.",
        Category = "Diagnostics",
        DefaultValue = "10000",
        RestartRequired = true)]
    public int LogRecordCapacity { get; set; } = DefaultOptions.LogRecordCapacity;

    [ManifestSetting(
        DisplayName = "Resource Capacity",
        Description = "Maximum number of recent telemetry resources retained in memory.",
        Category = "Diagnostics",
        DefaultValue = "500",
        RestartRequired = true)]
    public int ResourceCapacity { get; set; } = DefaultOptions.ResourceCapacity;

    [ManifestSetting(
        DisplayName = "Subscriber Channel Capacity",
        Description = "Maximum queued live updates per subscriber before updates are dropped.",
        Category = "Diagnostics",
        DefaultValue = "1000",
        RestartRequired = true)]
    public int SubscriberChannelCapacity { get; set; } = DefaultOptions.SubscriberChannelCapacity;

    [ManifestSetting(
        DisplayName = "Max HTTP Request Body Size",
        Description = "Maximum OTLP HTTP/protobuf request body size in bytes.",
        Category = "Diagnostics",
        DefaultValue = "10485760",
        RestartRequired = true)]
    public long MaxHttpRequestBodySize { get; set; } = DefaultOptions.MaxHttpRequestBodySize;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddOpenTelemetryDiagnosticsServices(ConfigureOptions);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapOpenTelemetryHttpProtobufCollector();
        endpoints.MapOpenTelemetryGrpcCollector();
        endpoints.MapOpenTelemetryHub();
    }

    private void ConfigureOptions(OpenTelemetryDiagnosticsOptions options)
    {
        options.TraceCapacity = TraceCapacity;
        options.SpanCapacity = SpanCapacity;
        options.MetricPointCapacity = MetricPointCapacity;
        options.LogRecordCapacity = LogRecordCapacity;
        options.ResourceCapacity = ResourceCapacity;
        options.SubscriberChannelCapacity = SubscriberChannelCapacity;
        options.MaxHttpRequestBodySize = MaxHttpRequestBodySize;
    }
}
