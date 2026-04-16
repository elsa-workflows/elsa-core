using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using CShells.Notifications;
using Elsa.ModularServer.Web;
using Elsa.ModularServer.Web.Catalog;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nuplane;
using Nuplane.Loading.Hosting.Builder;
using Nuplane.Sources.Directory.Configuration;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

var nuplaneConfiguration = configuration.GetSection("Nuplane");

services.AddNuplane(nuplaneConfiguration, nuplane =>
{
    nuplane.AddDirectoryFeedsFromConfiguration(nuplaneConfiguration);
    nuplane.AutoloadPackages(nuplaneConfiguration.GetSection("Loading"));
    nuplane.OnPackagesChanged<MyPackageObserver>();
});

services.AddSingleton<NuplaneAssemblyProvider>();

builder.AddShells(shells => shells
    .FromHostAssemblies()
    .WithAssemblyProvider<NuplaneAssemblyProvider>()
    .WithConfigurationProvider(configuration)
    .WithAuthenticationAndAuthorization());

// Work around the current CShells package publishing a duplicate aggregate endpoint registration pass
// on ShellsReloaded, which causes FastEndpoints to be mapped a second time during startup.
services.RemoveAll<INotificationHandler<ShellsReloaded>>();

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