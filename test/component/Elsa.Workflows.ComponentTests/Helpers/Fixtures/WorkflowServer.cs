﻿using System.Net.Http.Headers;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.MassTransit.Extensions;
using Elsa.Workflows.ComponentTests.Services;
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
                elsa.UseWorkflowManagement(management =>
                {
                    management.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
                    management.UseMassTransitDispatcher();
                    management.UseCache();
                });
                elsa.UseWorkflowRuntime(runtime =>
                {
                    runtime.UseMassTransitDispatcher();
                });
            };
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ISignalManager, SignalManager>();
            services.AddSingleton<IWorkflowEvents, WorkflowEvents>();
            services.AddNotificationHandlersFrom<WorkflowServer>();
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}