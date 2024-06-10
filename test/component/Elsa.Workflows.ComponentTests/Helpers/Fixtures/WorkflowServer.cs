using System.Net.Http.Headers;
using System.Reflection;
using Elsa.Common.Contracts;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.MassTransit.Extensions;
using Elsa.Tenants.Extensions;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Handlers;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Consumers;
using Elsa.Workflows.ComponentTests.Helpers.Services;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using FluentStorage;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static Elsa.Api.Client.RefitSettingsHelper;

namespace Elsa.Workflows.ComponentTests.Helpers.Fixtures;

[UsedImplicitly]
public class WorkflowServer(Infrastructure infrastructure, string url) : WebApplicationFactory<Program>
{
    public TClient CreateApiClient<TClient>()
    {
        var client = CreateClient();
        client.BaseAddress = new Uri(client.BaseAddress!, "/elsa/api");
        client.Timeout = TimeSpan.FromMinutes(1);
        return RestService.For<TClient>(client, CreateRefitSettings());
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
                    //runtime.UseProtoActor();
                    runtime.UseDistributedRuntime();
                });
                elsa.UseHttp(http =>
                {
                    http.UseCache();
                });
                elsa.UseTenants(tenants =>
                {
                    tenants.UseTenantsProvider(_ => new TestTenantsProvider("Tenant1", "Tenant2"));
                    tenants.TenantsOptions = options => options.TenantResolutionPipelineBuilder.Append<TestTenantResolutionStrategy>();
                });
            };
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ISignalManager, SignalManager>();
            services.AddSingleton<IWorkflowEvents, WorkflowEvents>();
            services.AddSingleton<IWorkflowDefinitionEvents, WorkflowDefinitionEvents>();
            services.AddSingleton<ITriggerChangeTokenSignalEvents, TriggerChangeTokenSignalEvents>();
            services.AddScoped<ITenantResolutionStrategy, TestTenantResolutionStrategy>();
            services.AddNotificationHandlersFrom<WorkflowServer>();
            services.AddNotificationHandlersFrom<WorkflowEventHandlers>();
        });
    }
    
    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}