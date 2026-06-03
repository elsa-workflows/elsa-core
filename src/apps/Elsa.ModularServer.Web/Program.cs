using ConsoleLogStreaming.Core.Capture;
using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using Elsa.Dashboard.Api.ShellFeatures;
using Elsa.ModularServer.Web;
using Elsa.ModularServer.Web.Catalog;
using Elsa.ShellFeatures;
using Elsa.Workflows.Api.ShellFeatures;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Workflows.Runtime.Dashboard.ShellFeatures;
using Elsa.Workflows.Runtime.Distributed.ShellFeatures;
using Elsa.Workflows.Runtime.ShellFeatures;
using Elsa.Workflows.ShellFeatures;
using Elsa.Workflows.Telemetry;
using Nuplane;
using Nuplane.Loading.Hosting.Builder;
using Nuplane.Sources.Directory.Configuration;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

ConsoleStreamHook.Install();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.SetResourceBuilder(CreateOpenTelemetryResource(builder.Environment.ApplicationName, serviceVersion));
    logging.AddOtlpExporter(options => ConfigureDiagnosticsOtlpExporter(options, configuration, "logs"));
});

services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(WorkflowInstrumentation.ActivitySourceName)
        .AddOtlpExporter(options => ConfigureDiagnosticsOtlpExporter(options, configuration, "traces")))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(WorkflowInstrumentation.MeterName)
        .AddOtlpExporter(options => ConfigureDiagnosticsOtlpExporter(options, configuration, "metrics")));
var nuplaneConfiguration = configuration.GetSection("Nuplane");

services.AddNuplane(nuplaneConfiguration, nuplane =>
{
    nuplane.AddDirectoryFeedsFromConfiguration(nuplaneConfiguration);
    nuplane.AutoloadPackages(nuplaneConfiguration.GetSection("Loading"));
    nuplane.OnPackagesChanged<MyPackageObserver>();
});

services.AddSingleton<NuplaneAssemblyProvider>();

builder.AddShells(shells => shells
    .WithHostAssemblies()
    .WithAssemblyProvider<NuplaneAssemblyProvider>()
    .WithConfigurationProvider(configuration)
    .WithWebRouting(options => options.EnablePathRouting = true)
    .WithAuthenticationAndAuthorization()
    .ConfigureAllShells(shell =>
    {
        shell.WithFeatures(
            typeof(ElsaFeature),
            typeof(WorkflowManagementFeature),
            typeof(WorkflowRuntimeFeature),
            typeof(WorkflowsFeature),
            typeof(DistributedRuntimeFeature),
            typeof(DashboardApiFeature),
            typeof(WorkflowRuntimeDashboardFeature),
            typeof(WorkflowsApiFeature));
    }));

services.AddSingleton<PluginCatalog>();
services.AddHealthChecks();

services.AddAuthentication();
services.AddAuthorization();

var app = builder.Build();

app.MapHealthChecks("/");
app.MapShells();
app.UseAuthentication();
app.UseAuthorization();
app.MapSampleCatalog();
app.Run();

static ResourceBuilder CreateOpenTelemetryResource(string serviceName, string? serviceVersion)
{
    return ResourceBuilder.CreateDefault().AddService(serviceName, serviceVersion: serviceVersion);
}

static void ConfigureDiagnosticsOtlpExporter(OtlpExporterOptions options, IConfiguration configuration, string signal)
{
    var protocol = configuration["Diagnostics:OpenTelemetry:Exporter:Protocol"];
    options.Protocol = protocol?.Trim().ToLowerInvariant() switch
    {
        "grpc" => OtlpExportProtocol.Grpc,
        "http/protobuf" or "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
        _ => options.Protocol
    };

    var endpoint = configuration["Diagnostics:OpenTelemetry:Exporter:Endpoint"];
    if (!string.IsNullOrWhiteSpace(endpoint))
        options.Endpoint = new Uri(GetSignalEndpoint(endpoint, signal, options.Protocol), UriKind.Absolute);
}

static string GetSignalEndpoint(string endpoint, string signal, OtlpExportProtocol protocol)
{
    if (protocol != OtlpExportProtocol.HttpProtobuf)
        return endpoint;

    var trimmed = endpoint.TrimEnd('/');

    if (trimmed.EndsWith($"/v1/{signal}", StringComparison.OrdinalIgnoreCase))
        return trimmed;

    if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        return $"{trimmed}/{signal}";

    return $"{trimmed}/v1/{signal}";
}
