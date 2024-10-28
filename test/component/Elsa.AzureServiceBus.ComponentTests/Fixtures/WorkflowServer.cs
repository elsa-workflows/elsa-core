using System.Net.Http.Headers;
using Elsa.Agents;
using Elsa.Alterations.Extensions;
using Elsa.AzureServiceBus.ComponentTests.Extensions;
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
using FluentStorage;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AzureServiceBus.ComponentTests;

[UsedImplicitly]
public class WorkflowServer(Infrastructure infrastructure, string url) : WebApplicationFactory<Program>
{
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
                    var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
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
                    runtime.UseProtoActor();
                });
                elsa.UseAlterations(alterations =>
                {
                    alterations.UseEntityFrameworkCore(e => e.UsePostgreSql(dbConnectionString));
                });
                elsa.UseHttp(http =>
                {
                    http.UseCache();
                });
                elsa.UseAzureServiceBus();
                elsa.UseAgents();
                elsa.UseAgentPersistence(feature => feature.UseEntityFrameworkCore(ef => ef.UsePostgreSql(typeof(AgentsPostgreSqlProvidersExtensions).Assembly, dbConnectionString)));
            };
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<SignalManager>();
            services.AddSingleton<WorkflowEvents>();
            services.AddNotificationHandlersFrom<WorkflowServer>();
            services.AddNotificationHandlersFrom<WorkflowEventHandlers>();
            services.AddAzureServiceBusTestServices();
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}