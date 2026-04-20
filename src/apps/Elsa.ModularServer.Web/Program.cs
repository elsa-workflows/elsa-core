using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.DependencyInjection;
using Elsa.ModularServer.Web;
using Elsa.ModularServer.Web.Catalog;
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
    .WithHostAssemblies()
    .WithAssemblyProvider<NuplaneAssemblyProvider>()
    .WithConfigurationProvider(configuration)
    .WithWebRouting(options => options.EnablePathRouting = true)
    .WithAuthenticationAndAuthorization());


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