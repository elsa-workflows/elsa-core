using Elsa.Common.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Abstractions;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Scenarios.Activities.Primitives.Event;

public class PublishEventTests : AppComponentTest
{
    private readonly AsyncWorkflowRunner _workflowRunner;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IWorkflowRuntime _workflowRuntime;

    public PublishEventTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        _workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
    }

    [Fact]
    public async Task PublishEvent_LocalEvent_CompletesWorkflow()
    {
        // Act
        var result = await _workflowRunner.RunAndAwaitWorkflowCompletionAsync(WorkflowDefinitionHandle.ByDefinitionId(PublishAndConsumeEventWorkflow.DefinitionId, VersionOptions.Published));

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowExecutionContext.SubStatus);
    }

    [Fact]
    public async Task PublishEvent_GlobalEvent_TriggersConsumerWorkflow()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act - Publish global event
        await RunWorkflowAsync(PublishGlobalEventWorkflow.DefinitionId, correlationId);

        // Assert - Consumer workflow was triggered and completed
        var consumerInstance = await GetSingleWorkflowInstanceAsync(ConsumerWorkflow.DefinitionId, correlationId);
        Assert.Equal(WorkflowStatus.Finished, consumerInstance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, consumerInstance.SubStatus);
    }

    [Fact]
    public async Task PublishEvent_WithPayload_CompletesSuccessfully()
    {
        // Act
        var result = await _workflowRunner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(PublishGlobalEventWorkflow.DefinitionId, VersionOptions.Published));

        // Assert
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowExecutionContext.SubStatus);
    }

    private async Task RunWorkflowAsync(string definitionId, string? correlationId = null)
    {
        var workflowClient = await _workflowRuntime.CreateClientAsync();
        await workflowClient.CreateInstanceAsync(new()
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published),
            CorrelationId = correlationId
        });
        await workflowClient.RunInstanceAsync(RunWorkflowInstanceRequest.Empty);
    }

    private async Task<WorkflowInstance> GetSingleWorkflowInstanceAsync(string definitionId, string correlationId)
    {
        var instances = await _workflowInstanceStore.FindManyAsync(new()
        {
            DefinitionId = definitionId,
            CorrelationId = correlationId
        });
        return Assert.Single(instances);
    }
}
