using System.Reflection;
using Elsa.Alterations.Extensions;
using Elsa.Caching;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.MassTransit.Extensions;
using Elsa.Testing.Shared.Handlers;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Consumers;
using Elsa.Workflows.ComponentTests.Decorators;
using Elsa.Workflows.ComponentTests.Materializers;
using Elsa.Workflows.ComponentTests.WorkflowProviders;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using FluentStorage;
using Hangfire.Annotations;
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
                    //massTransit.UseRabbitMq(rabbitMqConnectionString);
                    massTransit.Services.AddSingleton<WorkflowDefinitionEvents>();
                    massTransit.AddConsumer<WorkflowDefinitionEventConsumer>("elsa-test-workflow-definition-updates", true);
                });
                elsa.UseIdentity(identity => identity.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString)));
                elsa.UseWorkflowManagement(management =>
                {
                    management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
                    management.UseMassTransitDispatcher();
                    management.UseCache();
                });
                elsa.UseWorkflowRuntime(runtime =>
                {
                    runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
                    runtime.UseCache();
                    runtime.UseMassTransitDispatcher();
                    runtime.UseDistributedRuntime();
                });
                elsa.UseDistributedCache(distributedCaching =>
                {
                    distributedCaching.UseMassTransit();
                });
                elsa.UseJavaScript(options =>
                {
                    options.AllowClrAccess = true;
                    options.ConfigureEngine(engine =>
                    {
                        engine.SetValue("getStaticValue", () => StaticValueHolder.Value);
                    });
                });
                elsa.UseAlterations(alterations =>
                {
                    alterations.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
                });
                elsa.UseHttp(http =>
                {
                    http.UseCache();
                });
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
                .AddNotificationHandlersFrom<WorkflowEventHandlers>()
                .Decorate<IChangeTokenSignaler, EventPublishingChangeTokenSignaler>()
                ;
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}