using Elsa.Common.Contracts;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Tenants.Services;

/// <summary>
/// A simple implementation that queues the specified request for workflow execution on a non-durable background worker.
/// </summary>
public class BackgroundTenantWorkflowDispatcher : IWorkflowDispatcher
{
    private readonly ICommandSender _commandSender;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BackgroundTenantWorkflowDispatcher(
        ICommandSender commandSender,
        ITenantAccessor tenantAccessor,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowInstanceStore workflowInstanceStore)
    {
        _commandSender = commandSender;
        _tenantAccessor = tenantAccessor;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowInstanceStore = workflowInstanceStore;
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowDefinitionResponse> DispatchAsync(DispatchWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowDefinitionCommand(
            request.DefinitionId,
            request.VersionOptions,
            request.Input,
            request.CorrelationId,
            request.InstanceId,
            request.TriggerActivityId);

        string? tenantId = await _workflowDefinitionStore.GetTenantId(request.DefinitionId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchWorkflowDefinitionResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchWorkflowInstanceResponse> DispatchAsync(DispatchWorkflowInstanceRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchWorkflowInstanceCommand(
            request.InstanceId,
            request.BookmarkId,
            request.ActivityId,
            request.ActivityNodeId,
            request.ActivityInstanceId,
            request.ActivityHash,
            request.Input,
            request.CorrelationId);

        var tenantId = await _workflowInstanceStore.GetTenantId(request.InstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchWorkflowInstanceResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchTriggerWorkflowsResponse> DispatchAsync(DispatchTriggerWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchTriggerWorkflowsCommand(
            request.ActivityTypeName,
            request.BookmarkPayload,
            request.CorrelationId,
            request.WorkflowInstanceId,
            request.ActivityInstanceId,
            request.Input);

        var tenantId = await _workflowInstanceStore.GetTenantId(request.WorkflowInstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchTriggerWorkflowsResponse();
    }

    /// <inheritdoc />
    public async Task<DispatchResumeWorkflowsResponse> DispatchAsync(DispatchResumeWorkflowsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new DispatchResumeWorkflowsCommand(
            request.ActivityTypeName,
            request.BookmarkPayload,
            request.CorrelationId,
            request.WorkflowInstanceId,
            request.ActivityInstanceId,
            request.Input);

        var tenantId = await _workflowInstanceStore.GetTenantId(request.WorkflowInstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _commandSender.SendAsync(command, CommandStrategy.Background, cancellationToken);
        return new DispatchResumeWorkflowsResponse();
    }
}