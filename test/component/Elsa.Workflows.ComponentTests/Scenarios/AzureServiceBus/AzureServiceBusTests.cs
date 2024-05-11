using Azure.Messaging.ServiceBus;
using Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus.Workflows;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.AzureServiceBus;

public class AzureServiceBusTests(App app) : AppComponentTest(app)
{
    [Fact]
    public async Task WorkflowReceivesMessage_WhenSendingMessageToTopic()
    {
        var client = Scope.ServiceProvider.GetRequiredService<ServiceBusClient>();
        var signalManager = Scope.ServiceProvider.GetRequiredService<ISignalManager>();
        var topic = MessageReceivedTriggerWorkflow.Topic;

        // Generate a correlation ID so that we can find the workflow instance later.
        var correlationId = Guid.NewGuid().ToString();

        await using var sender = client.CreateSender(topic);

        // Send a message to the topic. This should trigger the workflow.
        await sender.SendMessageAsync(new ServiceBusMessage("Message 1")
        {
            CorrelationId = correlationId
        });

        // Wait for the workflow to trigger the first signal.
        await signalManager.WaitAsync(MessageReceivedTriggerWorkflow.Signal1, 500000);

        // Send another message to the topic. This should resume the workflow.
        await sender.SendMessageAsync(new ServiceBusMessage("Message 2"));

        // Wait for the workflow to trigger the second signal.
        await signalManager.WaitAsync(MessageReceivedTriggerWorkflow.Signal2, 500000);

        // Find the workflow instance by correlation ID.
        var workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var workflowInstanceFilter = new WorkflowInstanceFilter
        {
            CorrelationId = correlationId
        };
        var workflowInstance = await workflowInstanceStore.FindAsync(workflowInstanceFilter);

        Assert.NotNull(workflowInstance);

        // Assert that the workflow is finished.
        Assert.Equal(WorkflowStatus.Finished, workflowInstance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, workflowInstance.SubStatus);
    }
}