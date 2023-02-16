using Elsa.MassTransit.Messages;
using Elsa.Workflows.Runtime.Services;
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
    /// Constructor.
    /// </summary>
    public DispatchWorkflowRequestConsumer(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchWorkflowDefinition> context)
    {
        var message = context.Message;
        var options = new StartWorkflowRuntimeOptions(message.CorrelationId, message.Input, message.VersionOptions, InstanceId: message.InstanceId);
            
        await _workflowRuntime.StartWorkflowAsync(message.DefinitionId, options, context.CancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchWorkflowInstance> context)
    {
        var message = context.Message;
        var options = new ResumeWorkflowRuntimeOptions(message.CorrelationId, message.BookmarkId, message.ActivityId, message.Input);
        await _workflowRuntime.ResumeWorkflowAsync(message.InstanceId, options, context.CancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchTriggerWorkflows> context)
    {
        var message = context.Message;
        var options = new TriggerWorkflowsRuntimeOptions(message.CorrelationId, message.Input);
        await _workflowRuntime.TriggerWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options, context.CancellationToken);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<DispatchResumeWorkflows> context)
    {
        var message = context.Message;
        var options = new ResumeWorkflowRuntimeOptions(CorrelationId: message.CorrelationId, Input: message.Input);
        await _workflowRuntime.ResumeWorkflowsAsync(message.ActivityTypeName, message.BookmarkPayload, options, context.CancellationToken);
    }
}