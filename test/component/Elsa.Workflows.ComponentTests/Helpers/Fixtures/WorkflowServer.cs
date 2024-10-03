using System.Net.Http.Headers;
using System.Reflection;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.MassTransit.Extensions;
using Elsa.Workflows.ComponentTests.Consumers;
using Elsa.Workflows.ComponentTests.Helpers.Materializers;
using Elsa.Workflows.ComponentTests.Helpers.Services;
using Elsa.Workflows.ComponentTests.Helpers.WorkflowProviders;
using Elsa.Workflows.ComponentTests.Services;
using Elsa.Workflows.Management.Contracts;
using FluentStorage;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static Elsa.Api.Client.RefitSettingsHelper;

namespace Elsa.Workflows.ComponentTests;

[UsedImplicitly]
public class WorkflowServer(Infrastructure infrastructure, string url) : WebApplicationFactory<Program>
{
    public TClient CreateApiClient<TClient>()
    {
        var client = CreateClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/elsa/api");
        client.Timeout = TimeSpan.FromMinutes(1);
        return RestService.For<TClient>(client, CreateRefitSettings(Services));
    }

    public HttpClient CreateHttpWorkflowClient()
    {
        var client = CreateClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/workflows/");
        client.Timeout = TimeSpan.FromMinutes(1);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var dbConnectionString = infrastructure.DbContainer.GetConnectionString();
        var rabbitMqConnectionString = infrastructure.RabbitMqContainer.GetConnectionString();

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
                elsa.UseMassTransit(massTransit =>
                {
                    massTransit.UseRabbitMq(rabbitMqConnectionString);
                    massTransit.AddConsumer<WorkflowDefinitionEventConsumer>("elsa-test-workflow-definition-updates", true);
                    massTransit.AddConsumer<TriggerChangeTokenSignalConsumer>("elsa-test-change-token-signal", true);
                });
                elsa.UseWorkflowManagement(management =>
                {
                    management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
                    management.UseMassTransitDispatcher();
                    management.UseCache();
                });
                elsa.UseWorkflowRuntime(runtime =>
                {
                    runtime.UseMassTransitDispatcher();
                    runtime.UseCache();
                });
                elsa.UseJavaScript(options =>
                {
                    options.AllowClrAccess = true;
                    options.ConfigureEngine(engine =>
                    {
                        engine.SetValue("getStaticValue", () => StaticValueHolder.Value);
                    });
                });
            };
        }

        builder.ConfigureTestServices(services =>
        {
            services
                .AddSingleton<ISignalManager, SignalManager>()
                .AddSingleton<IWorkflowEvents, WorkflowEvents>()
                .AddSingleton<IWorkflowDefinitionEvents, WorkflowDefinitionEvents>()
                .AddSingleton<ITriggerChangeTokenSignalEvents, TriggerChangeTokenSignalEvents>()
                .AddScoped<IWorkflowMaterializer, TestWorkflowMaterializer>()
                .AddNotificationHandlersFrom<WorkflowServer>()
                .AddWorkflowDefinitionProvider<TestWorkflowProvider>()
                ;
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}