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
    private readonly AsyncWorkflowRunner _workflowRunner;
    private readonly IEventPublisher _eventPublisher;

    public EventTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        _eventPublisher = Scope.ServiceProvider.GetRequiredService<IEventPublisher>();
    }

    [Fact]
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
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowExecutionContext.SubStatus);
    }

    [Fact]
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
        var instance = Assert.Single(instances);
        Assert.Equal(WorkflowStatus.Finished, instance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, instance.SubStatus);
    }
}