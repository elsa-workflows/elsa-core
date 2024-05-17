using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.Workflows.ComponentTests;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.AzureServiceBus.ComponentTests.Extensions;

public static class AzureServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddAzureServiceBusTestServices(this IServiceCollection services)
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

        services.AddSingleton(serviceBusClient);
        services.AddSingleton(Substitute.For<ServiceBusAdministrationClient>());
        return services;
    }

    private static ProcessMessageEventArgs CreateMessageArgs(ServiceBusMessage transportMessage, int deliveryCount = 1)
    {
        var payloadJson = JsonSerializer.Serialize(transportMessage);
        var props = new Dictionary<string, object>();

        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(payloadJson),
            deliveryCount: deliveryCount,
            correlationId: transportMessage.CorrelationId,
            properties: props
        );

        return new ProcessMessageEventArgs(message, null, new CancellationToken());
    }
}