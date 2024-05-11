using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Identity.Providers;
using Elsa.MassTransit.Extensions;
using Elsa.Workflows.ComponentTests.Helpers.Mocks;
using Elsa.Workflows.ComponentTests.Services;
using Elsa.Workflows.Runtime.Stores;
using FluentStorage;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.Extensions;
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
                    //runtime.UseEntityFrameworkCore(ef => ef.UsePostgreSql(dbConnectionString));
                    //runtime.UseCache();
                    runtime.TriggerStore = sp => sp.GetRequiredService<MemoryTriggerStore>();
                    runtime.BookmarkStore = sp => sp.GetRequiredService<MemoryBookmarkStore>();
                    runtime.UseMassTransitDispatcher();
                });
                elsa.UseHttp(http =>
                {
                    http.UseCache();
                });
            };
        }

        builder.ConfigureTestServices(services =>
        {
            var serviceBusClient = Substitute.For<ServiceBusClient>();
            var senders = new Dictionary<string, ServiceBusSender>();
            var processors = new Dictionary<string, MockServiceBusProcessor>();
            serviceBusClient.CreateSender(Arg.Any<string>()).Returns(createSenderCall =>
            {
                var queueOrTopicName = createSenderCall.Arg<string>();

                return senders.GetOrAdd(queueOrTopicName, () =>
                {
                    var serviceBusSender = Substitute.For<ServiceBusSender>();
                    serviceBusSender.SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>()).Returns(sendMessageCall =>
                    {
                        var message = sendMessageCall.Arg<ServiceBusMessage>();
                        var args = CreateMessageArgs(message);
                        var serviceBusProcessor = processors.Where(x => x.Key.StartsWith(queueOrTopicName, StringComparison.OrdinalIgnoreCase)).ToList();
                        foreach (var (_, processor) in serviceBusProcessor)
                            processor.RaiseProcessMessageAsync(args);
                        return Task.CompletedTask;
                    });
                    return serviceBusSender;
                });
            });
            serviceBusClient.CreateProcessor(Arg.Any<string>(), Arg.Any<ServiceBusProcessorOptions>()).Returns(createProcessorCall =>
            {
                var queueOrTopicName = createProcessorCall.Arg<string>();
                var key = queueOrTopicName;
                return processors.GetOrAdd(key, () =>
                {
                    return new MockServiceBusProcessor(() =>
                    {
                        processors.Remove(key);
                    });
                });
            });
            serviceBusClient.CreateProcessor(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<ServiceBusProcessorOptions>()).Returns(createProcessorCall =>
            {
                var queueOrTopicName = createProcessorCall.ArgAt<string>(0);
                var subscription = createProcessorCall.ArgAt<string>(1);
                var key = $"{queueOrTopicName}:{subscription}";
                return processors.GetOrAdd(key, () =>
                {
                    return new MockServiceBusProcessor(() =>
                    {
                        processors.Remove(key);
                    });
                });
            });

            services.AddSingleton<ISignalManager, SignalManager>();
            services.AddSingleton<IWorkflowEvents, WorkflowEvents>();
            services.AddNotificationHandlersFrom<WorkflowServer>();
            services.AddSingleton(serviceBusClient);
            services.AddSingleton(Substitute.For<ServiceBusAdministrationClient>());
        });
    }

    private ProcessMessageEventArgs CreateMessageArgs(object payload, string? correlationId = null, int deliveryCount = 1)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var props = new Dictionary<string, object>();

        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(payloadJson),
            deliveryCount: deliveryCount,
            correlationId: correlationId,
            properties: props
        );

        return new ProcessMessageEventArgs(message, null, new CancellationToken());
    }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", AdminApiKeyProvider.DefaultApiKey);
    }
}