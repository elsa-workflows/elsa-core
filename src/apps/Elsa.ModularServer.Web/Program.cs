using ConsoleLogStreaming.Core.Capture;
using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.ModularServer.Web;
using Elsa.ModularServer.Web.Catalog;
using Elsa.ShellFeatures;
using Elsa.Workflows.Api.ShellFeatures;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Workflows.Runtime.Distributed.ShellFeatures;
using Elsa.Workflows.Runtime.ShellFeatures;
using Elsa.Workflows.ShellFeatures;
using Elsa.Workflows.Telemetry;
using Nuplane;
using Nuplane.Loading.Hosting.Builder;
using Nuplane.Sources.Directory.Configuration;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

ConsoleStreamHook.Install();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString();

// Console output is a single process-wide resource; the capture pipeline is owned by the root host so
// every shell's diagnostics endpoints (REST + SignalR) read from the same in-memory ring buffer.
services.AddConsoleLogsHost();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.SetResourceBuilder(CreateOpenTelemetryResource(builder.Environment.ApplicationName, serviceVersion));
    logging.AddOtlpExporter();
});

services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName, serviceVersion: serviceVersion))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource(WorkflowInstrumentation.ActivitySourceName)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(WorkflowInstrumentation.MeterName)
        .AddOtlpExporter());

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
