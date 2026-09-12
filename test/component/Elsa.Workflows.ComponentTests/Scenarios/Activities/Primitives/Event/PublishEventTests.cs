using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Testing.Shared;
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
    private readonly WorkflowEvents _workflowEvents;
    private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

    public PublishEventTests(App app) : base(app)
    {
        _workflowRunner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        _workflowInstanceStore = Scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
        _workflowRuntime = Scope.ServiceProvider.GetRequiredService<IWorkflowRuntime>();
        _workflowEvents = Scope.ServiceProvider.GetRequiredService<WorkflowEvents>();
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
        await _workflowRunner.RunWorkflowAsync(PublishGlobalEventWorkflow.DefinitionId, correlationId);

        // Assert - Consumer workflow was triggered and completed
        var consumerInstance = await GetSingleWorkflowInstanceAsync(ConsumerWorkflow.DefinitionId, correlationId);
        Assert.Equal(WorkflowStatus.Finished, consumerInstance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, consumerInstance.SubStatus);
    }

    [Fact]
    public async Task PublishEvent_WithPayload_TransmitsPayloadToConsumer()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act - Publish global event with payload
        await _workflowRunner.RunWorkflowAsync(PublishGlobalEventWorkflow.DefinitionId, correlationId);

        // Assert - Consumer workflow received the payload
        var consumerInstance = await GetSingleWorkflowInstanceAsync(ConsumerWorkflow.DefinitionId, correlationId);
        Assert.Equal(WorkflowStatus.Finished, consumerInstance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, consumerInstance.SubStatus);

        // Verify the payload was captured in the output
        Assert.True(consumerInstance.WorkflowState.Output.TryGetValue("ReceivedPayload", out var receivedPayload), "Consumer workflow should have ReceivedPayload output");
        Assert.NotNull(receivedPayload);

        // Verify the payload content. The payload's runtime representation is not stable: while it is still the
        // original CLR object its properties are PascalCase, but once it has been through the workflow state
        // serializer it is an ExpandoObject whose keys have been camelCased by that serializer's naming policy.
        // Which one this test observes depends on whether the instance was read back from the store, so match
        // the property name case-insensitively rather than asserting one of the two representations.
        var payload = JsonSerializer.Deserialize<ReceivedEventPayload>(JsonSerializer.Serialize(receivedPayload), CaseInsensitive);
        Assert.Equal("Shipped", payload?.Status);
    }

    private record ReceivedEventPayload(string? Status);

    private async Task<WorkflowInstance> GetSingleWorkflowInstanceAsync(string definitionId, string correlationId, int timeoutMs = 5000)
    {
        var tcs = new TaskCompletionSource<WorkflowInstance>();
        var cts = new CancellationTokenSource(timeoutMs);

        // Register cancellation to fail the task on timeout
        cts.Token.Register(() => tcs.TrySetException(new TimeoutException($"Workflow instance with DefinitionId '{definitionId}' and CorrelationId '{correlationId}' was not saved within {timeoutMs}ms")));

        // Subscribe to the WorkflowInstanceSaved event
        // A workflow instance is saved several times over its lifetime, so only accept a terminal one: both callers
        // assert on the finished state, and an intermediate save would hand them an instance that is still running.
        void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs args)
        {
            if (args.WorkflowInstance.DefinitionId == definitionId && args.WorkflowInstance.CorrelationId == correlationId && args.WorkflowInstance.Status == WorkflowStatus.Finished)
            {
                tcs.TrySetResult(args.WorkflowInstance);
            }
        }

        _workflowEvents.WorkflowInstanceSaved += OnWorkflowInstanceSaved;

        try
        {
            // Check if the instance already exists in the database
            var existingInstances = (await _workflowInstanceStore.FindManyAsync(new()
            {
                DefinitionId = definitionId,
                CorrelationId = correlationId
            }, cts.Token)).ToList();

            if (existingInstances.Any(x => x.Status == WorkflowStatus.Finished))
                return Assert.Single(existingInstances);

            // Wait for the event to be raised
            return await tcs.Task;
        }
        finally
        {
            _workflowEvents.WorkflowInstanceSaved -= OnWorkflowInstanceSaved;
            cts.Dispose();
        }
    }
}
