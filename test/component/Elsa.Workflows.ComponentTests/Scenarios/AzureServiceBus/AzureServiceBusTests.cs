using Azure.Messaging.ServiceBus;
using Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus;

public class AzureServiceBusTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task WorkflowReceivesMessage_WhenSendingMessageToTopic()
    {
        var client = Scope.ServiceProvider.GetRequiredService<ServiceBusClient>();
        var signalManager = Scope.ServiceProvider.GetRequiredService<ISignalManager>();
        var topic = ReceiveMessageWorkflow.Topic;

        await using var sender = client.CreateSender(topic);
        await sender.SendMessageAsync(new ServiceBusMessage("Message 1"));
        await signalManager.WaitAsync(ReceiveMessageWorkflow.Signal1, 500000);
        await sender.SendMessageAsync(new ServiceBusMessage("Message 2"));
        await signalManager.WaitAsync(ReceiveMessageWorkflow.Signal2, 500000);
    }
}