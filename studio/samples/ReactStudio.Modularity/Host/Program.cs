using CShells;
using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;
using CShells.AspNetCore.Resolution;
using CShells.DependencyInjection;
using Elsa.ReactStudio.Spike;
using Elsa.ReactStudio.Spike.Labels;
using Example.ReactStudio.Spike.Weather;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddShells(shells => shells
    .WithAssemblies(typeof(PreviewFeature).Assembly, typeof(LabelsFeature).Assembly, typeof(WeatherFeature).Assembly)
    .WithResolverStrategy(new FixedShellResolver(new ShellId("Default")))
    .AddShell("Default", shell =>
    {
        shell.WithFeatures(typeof(LabelsFeature));
        if (builder.Configuration.GetValue("Spike:ThirdPartyEnabled", true))
        {
            shell.WithFeatures(typeof(WeatherFeature), typeof(IncompatibleFeature));
        }
    }));

var viteUrl = builder.Configuration["Spike:ViteUrl"];
if (!string.IsNullOrEmpty(viteUrl))
{
    builder.Services.AddReverseProxy().LoadFromMemory(
        [new RouteConfig { RouteId = "vite", ClusterId = "vite", Match = new RouteMatch { Path = "/studio-preview/vite/{**path}" } }],
        [new ClusterConfig { ClusterId = "vite", Destinations = new Dictionary<string, DestinationConfig> { ["vite"] = new() { Address = viteUrl } } }]);
}

var app = builder.Build();
app.UseRouting();
if (!string.IsNullOrEmpty(viteUrl))
{
    app.UseWebSockets();
    app.MapReverseProxy();
}
app.MapShells();
app.Run();
