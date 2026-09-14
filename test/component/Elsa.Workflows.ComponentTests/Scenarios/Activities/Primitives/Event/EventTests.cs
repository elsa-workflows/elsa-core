using Elsa.Common.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event;

public class EventTests : AppComponentTest
{
    private AsyncWorkflowRunner _workflowRunner = null!;
    private IEventPublisher _eventPublisher = null!;

    public EventTests(App app) : base(app)
    {
    }

    protected override ValueTask OnInitializeAsync()
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        _eventPublisher = Scope.ServiceProvider.GetRequiredService<IEventPublisher>();
        return ValueTask.CompletedTask;
    }

    [Test]
    public async Task PublishingEventToBlockingEventWorkflow_ShouldCompleteWorkflow()
    {
        // Start the workflow - it will block at the Event activity
        var workflowTask = _workflowRunner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(BlockingEventWorkflow.DefinitionId, VersionOptions.Published));

        // Publish the event to resume the workflow
        await _eventPublisher.PublishAsync("Order Shipped");

        // Wait for the workflow to complete
        var result = await workflowTask;

        // Assert the workflow completed successfully
        await Assert.That(result.WorkflowExecutionContext.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }

    [Test]
    public async Task PublishingEventToEventAsTriggerWorkflow_ShouldStartAndCompleteWorkflow()
    {
        // Publish the event - this should trigger the workflow to start
        var correlationId = Guid.NewGuid().ToString();
        await _eventPublisher.PublishAsync("Order Shipped", correlationId);

        // The workflow should have been triggered and completed
        // We can verify this by checking that the workflow was executed
        var workflowInstances = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = TriggerEventWorkflow.DefinitionId,
            CorrelationId = correlationId
        };

        var instances = await workflowInstances.FindManyAsync(filter);
        var instance = await Assert.That(instances).HasSingleItem();
        await Assert.That(instance.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(instance.SubStatus).IsEqualTo(WorkflowSubStatus.Finished);
    }
}
