using Azure.Messaging.ServiceBus;
using Elsa.AzureServiceBus.ComponentTests.Workflows;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AzureServiceBus.ComponentTests;

public class AzureServiceBusTests : AppComponentTest
{
    private static readonly object WorkflowCompletedSignal = new();
    private readonly SignalManager _signalManager;
    private readonly WorkflowEvents _workflowEvents;

    public AzureServiceBusTests(App app) : base(app)
    {
        _signalManager = Scope.ServiceProvider.GetRequiredService<SignalManager>();
        _workflowEvents = Scope.ServiceProvider.GetRequiredService<WorkflowEvents>();
        _workflowEvents.WorkflowInstanceSaved += OnWorkflowInstanceSaved;
    }

    private void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs e)
    {
        if (e.WorkflowInstance.Status != WorkflowStatus.Finished)
            return;

        if (e.WorkflowInstance.DefinitionId == MessageReceivedTriggerWorkflow.DefinitionId)
            _signalManager.Trigger(WorkflowCompletedSignal, e);
    }

    [Fact]
    public async Task WorkflowReceivesMessage_WhenSendingMessageToTopic()
    {
        await using var client = Scope.ServiceProvider.GetRequiredService<ServiceBusClient>();

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
        await _signalManager.WaitAsync(MessageReceivedTriggerWorkflow.Signal1);

        // Send another message to the topic. This should resume the workflow.
        await sender.SendMessageAsync(new ServiceBusMessage("Message 2"));

        // Wait for the workflow to trigger the second signal.
        await _signalManager.WaitAsync(MessageReceivedTriggerWorkflow.Signal2);

        // Wait for the workflow to complete.
        await _signalManager.WaitAsync(WorkflowCompletedSignal);

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

    protected override void OnDispose()
    {
        _workflowEvents.WorkflowInstanceSaved -= OnWorkflowInstanceSaved;
    }
}