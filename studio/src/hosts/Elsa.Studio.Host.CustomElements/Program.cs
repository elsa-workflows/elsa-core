using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Shell.Extensions;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Core.BlazorWasm.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Host.CustomElements.Components;
using Elsa.Studio.Host.CustomElements.HttpMessageHandlers;
using Elsa.Studio.Host.CustomElements.Services;
using Elsa.Studio.Localization.Time;
using Elsa.Studio.Localization.Time.Providers;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.BlazorWasm.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Secrets.Extensions;
using Elsa.Studio.UserTasks.Extensions;
using Elsa.Studio.Workflows.Designer.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Build the host.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuration = builder.Configuration;

// Register the custom elements.
builder.RootComponents.RegisterCustomElsaStudioElements();
builder.RootComponents.RegisterCustomElement<BackendProvider>("elsa-backend-provider");
builder.RootComponents.RegisterCustomElement<WorkflowDefinitionEditorWrapper>("elsa-workflow-definition-editor");
builder.RootComponents.RegisterCustomElement<WorkflowInstanceViewerWrapper>("elsa-workflow-instance-viewer");
builder.RootComponents.RegisterCustomElement<WorkflowInstanceListWrapper>("elsa-workflow-instance-list");
builder.RootComponents.RegisterCustomElement<WorkflowDefinitionListWrapper>("elsa-workflow-definition-list");

// Register local services.
builder.Services.AddSingleton<BackendService>();

// Register the modules.
var backendApiConfig = new BackendApiConfig
{
    ConfigureBackendOptions = options => configuration.GetSection("Backend").Bind(options),
    ConfigureHttpClientBuilder = options =>
    {
        options.ApiKey = configuration["Backend:ApiKey"];
        options.AuthenticationHandler = typeof(AuthHttpMessageHandler);
    }, 
};

var localizationConfig = new LocalizationConfig
{
    ConfigureLocalizationOptions = options => configuration.GetSection("Localization").Bind(options),
};

builder.Services.AddCore();
builder.Services.AddShell();
builder.Services.AddRemoteBackend(backendApiConfig);
builder.Services.Replace(ServiceDescriptor.Scoped<IRemoteBackendAccessor, ComponentRemoteBackendAccessor>());
builder.Services.AddScoped<IHttpConnectionOptionsConfigurator, BackendServiceHttpConnectionOptionsConfigurator>();
builder.Services.AddWorkflowsModule(backendApiConfig);
builder.Services.AddSecretsModule(backendApiConfig);
builder.Services.AddUserTasksModule(backendApiConfig);
builder.Services.AddLocalizationModule(localizationConfig);
builder.Services.AddScoped<ITimeZoneProvider, LocalTimeZoneProvider>();

// Build the application.
var app = builder.Build();

await app.UseElsaLocalization();

// Run each startup task.
var startupTask = app.Services.GetServices<IStartupTask>();
foreach (var task in startupTask) await task.ExecuteAsync();

// Run the application.
await app.RunAsync();
