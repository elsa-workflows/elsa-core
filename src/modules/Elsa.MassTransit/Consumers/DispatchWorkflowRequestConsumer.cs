using Elsa.MassTransit.Messages;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
using MassTransit;

namespace Elsa.MassTransit.Consumers;

/// <summary>
/// A consumer of various dispatch message types to asynchronously execute workflows.
/// </summary>
public class DispatchWorkflowRequestConsumer :
    IConsumer<DispatchWorkflowDefinition>,
    IConsumer<DispatchWorkflowInstance>,
    IConsumer<DispatchTriggerWorkflows>,
    IConsumer<DispatchResumeWorkflows>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowRequestConsumer"/> class.
    /// </summary>
    public DispatchWorkflowRequestConsumer(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchWorkflowDefinition> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var options = new StartWorkflowRuntimeOptions(message.CorrelationId, message.Input, message.VersionOptions, message.TriggerActivityId, message.InstanceId, cancellationToken);

        await _workflowRuntime.TryStartWorkflowAsync(message.DefinitionId, options);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchWorkflowInstance> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var options = new ResumeWorkflowRuntimeOptions(
            message.CorrelationId,
            message.BookmarkId,
            message.ActivityId,
            message.ActivityNodeId,
            message.ActivityInstanceId,
            message.ActivityHash,
            message.Input,
            cancellationToken);

        await _workflowRuntime.ResumeWorkflowAsync(message.InstanceId, options);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchTriggerWorkflows> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        var options = new TriggerWorkflowsOptions(message.CorrelationId, message.WorkflowInstanceId, message.ActivityInstanceId, message.Input, cancellationToken);
        await _workflowRuntime.TriggerWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchResumeWorkflows> context)
    {
        var message = context.Message;
        var cancellationToken = context.CancellationToken;

        var options = new TriggerWorkflowsOptions(
            correlationId: message.CorrelationId,
            workflowInstanceId: message.WorkflowInstanceId,
            input: message.Input,
            cancellationTokens: cancellationToken);

        await _workflowRuntime.ResumeWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options);
    }
}