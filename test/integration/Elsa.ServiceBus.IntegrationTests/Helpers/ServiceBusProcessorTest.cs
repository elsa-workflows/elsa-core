using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Xunit.Abstractions;

namespace Elsa.ServiceBus.IntegrationTests.Helpers;

public class ServiceBusProcessorTest(ITestOutputHelper testOutputHelper) : ServiceBusProcessor
{
    public Task SendMessage<T>(T payload, string correlationId, int attempt = 1)
    {
        var args = CreateMessageArgs(payload, correlationId, attempt);
        return base.OnProcessMessageAsync(args);
    }
        
    public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
    {
        testOutputHelper.WriteLine("Receiving Service Bus Message");
        return Task.CompletedTask;
    }
        
    private ProcessMessageEventArgs CreateMessageArgs<T>(T payload, string correlationId, int deliveryCount = 1)
    {
        var payloadJson = JsonSerializer.Serialize(payload);
        var props = new Dictionary<string, object>() { };
         
        var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(payloadJson),
            deliveryCount: deliveryCount,
            correlationId: correlationId,
            properties: props
        );
            
        return new ProcessMessageEventArgs(message, null, new CancellationToken());
    }
}