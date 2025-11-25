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
        await RunWorkflowAsync(PublishGlobalEventWorkflow.DefinitionId, correlationId);

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
        await RunWorkflowAsync(PublishGlobalEventWorkflow.DefinitionId, correlationId);

        // Assert - Consumer workflow received the payload
        var consumerInstance = await GetSingleWorkflowInstanceAsync(ConsumerWorkflow.DefinitionId, correlationId);
        Assert.Equal(WorkflowStatus.Finished, consumerInstance.Status);
        Assert.Equal(WorkflowSubStatus.Finished, consumerInstance.SubStatus);

        // Verify the payload was captured in the output
        Assert.True(consumerInstance.WorkflowState.Output.ContainsKey("ReceivedPayload"), "Consumer workflow should have ReceivedPayload output");
        var receivedPayload = consumerInstance.WorkflowState.Output["ReceivedPayload"];
        Assert.NotNull(receivedPayload);

        // Verify the payload structure and content
        var payloadJson = JsonSerializer.Serialize(receivedPayload);
        Assert.Contains("\"Status\"", payloadJson);
        Assert.Contains("\"Shipped\"", payloadJson);
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

    private async Task<WorkflowInstance> GetSingleWorkflowInstanceAsync(string definitionId, string correlationId, int timeoutMs = 5000)
    {
        var tcs = new TaskCompletionSource<WorkflowInstance>();
        var cts = new CancellationTokenSource(timeoutMs);

        // Register cancellation to fail the task on timeout
        cts.Token.Register(() => tcs.TrySetException(new TimeoutException($"Workflow instance with DefinitionId '{definitionId}' and CorrelationId '{correlationId}' was not saved within {timeoutMs}ms")));

        // Subscribe to the WorkflowInstanceSaved event
        void OnWorkflowInstanceSaved(object? sender, WorkflowInstanceSavedEventArgs args)
        {
            if (args.WorkflowInstance.DefinitionId == definitionId && args.WorkflowInstance.CorrelationId == correlationId)
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

            if (existingInstances.Any())
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
