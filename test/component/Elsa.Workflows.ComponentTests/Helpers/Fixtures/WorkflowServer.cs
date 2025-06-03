using System.Reflection;
using Elsa.Caching;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Decorators;
using Elsa.Workflows.ComponentTests.Materializers;
using Elsa.Workflows.ComponentTests.WorkflowProviders;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using FluentStorage;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static Elsa.Api.Client.RefitSettingsHelper;

namespace Elsa.Workflows.ComponentTests.Fixtures;

[UsedImplicitly]
public class WorkflowServer(Infrastructure infrastructure, string url) : WebApplicationFactory<Program>
{
    public TClient CreateApiClient<TClient>()
    {
        var client = CreateClient();
        client.BaseAddress = new(client.BaseAddress!, "/elsa/api");
        client.Timeout = TimeSpan.FromMinutes(1);
        return RestService.For<TClient>(client, CreateRefitSettings(Services));
    }

    public HttpClient CreateHttpWorkflowClient()
    {
        var client = CreateClient();
        client.BaseAddress = new(client.BaseAddress!, "/workflows/");
        client.Timeout = TimeSpan.FromMinutes(1);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseUrls(url);

        if (Program.ConfigureForTest == null)
        {
            Program.ConfigureForTest = elsa =>
            {
                elsa.AddWorkflowsFrom<WorkflowServer>();
                elsa.AddActivitiesFrom<WorkflowServer>();
                elsa.UseDefaultAuthentication(defaultAuthentication => defaultAuthentication.UseAdminApiKey());
                elsa.UseFluentStorageProvider(sp =>
                {
                    var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)!;
                    var workflowsDirectorySegments = new[]
                    {
                        assemblyDirectory, "Scenarios"
                    };
                    var workflowsDirectory = Path.Join(workflowsDirectorySegments);
                    return StorageFactory.Blobs.DirectoryFiles(workflowsDirectory);
                });
                elsa.UseIdentity();
                elsa.UseWorkflowManagement();
                elsa.UseWorkflowRuntime(runtime => runtime.UseDistributedRuntime());
                elsa.UseJavaScript(options =>
                {
                    options.AllowClrAccess = true;
                    options.ConfigureEngine(engine =>
                    {
                        engine.SetValue("getStaticValue", () => StaticValueHolder.Value);
                    });
                });
                elsa.UseHttp();
            };
        }

        builder.ConfigureTestServices(services =>
        {
            services
                .AddSingleton<SignalManager>()
                .AddScoped<WorkflowEvents>()
                .AddScoped<WorkflowDefinitionEvents>()
                .AddSingleton<TriggerChangeTokenSignalEvents>()
                .AddScoped<IWorkflowMaterializer, TestWorkflowMaterializer>()
                .AddNotificationHandlersFrom<WorkflowServer>()
                .AddWorkflowDefinitionProvider<TestWorkflowProvider>()
                .Decorate<IChangeTokenSignaler, EventPublishingChangeTokenSignaler>()
                ;
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}