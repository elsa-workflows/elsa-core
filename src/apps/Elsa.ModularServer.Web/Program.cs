using ConsoleLogStreaming.Core.Capture;
using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using Elsa.Diagnostics.ConsoleLogs.Extensions;
using Elsa.Diagnostics.ConsoleLogs.Services;
using Elsa.ModularServer.Web;
using Elsa.ModularServer.Web.Catalog;
using Elsa.ShellFeatures;
using Elsa.Workflows.Api.ShellFeatures;
using Elsa.Workflows.Management.ShellFeatures;
using Elsa.Workflows.Runtime.Distributed.ShellFeatures;
using Elsa.Workflows.Runtime.ShellFeatures;
using Elsa.Workflows.ShellFeatures;
using Nuplane;
using Nuplane.Loading.Hosting.Builder;
using Nuplane.Sources.Directory.Configuration;

// Install the console stream tee as early as possible — before WebApplication.CreateBuilder and
// any logger provider construction — so that downstream loggers (Microsoft.Extensions.Logging.Console,
// Kestrel/Hosting loggers, etc.) capture our tee writer instead of the raw Console.Out / Console.Error.
ConsoleStreamHook.Install();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Console output is a single process-wide resource; the capture pipeline is owned by the root host so
// every shell's diagnostics endpoints (REST + SignalR) read from the same in-memory ring buffer.
services.AddConsoleLogsHost();

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
