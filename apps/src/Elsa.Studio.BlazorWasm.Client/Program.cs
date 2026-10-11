using Elsa.Studio.BlazorWasm.Client.Bootstrap;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Providers;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var services = builder.Services;
var configuration = builder.Configuration;

builder.SetupCore();

var backendApiConfig = services.ConfigureBackend(configuration);
services.AddModules(configuration, backendApiConfig);
services.ConfigureLogin(configuration);
services.ConfigureDiagnostics(configuration);

builder.Services.AddScoped<ITimeZoneProvider, LocalTimeZoneProvider>();

var app = builder.Build();

var startupTaskRunner = app.Services.GetRequiredService<IStartupTaskRunner>();
await startupTaskRunner.RunStartupTasksAsync();

await app.RunAsync();