using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Tenants.Handlers;

// ReSharper disable once UnusedType.Global
internal class DispatchTenantWorkflowRequestHandler :
    ICommandHandler<DispatchTriggerWorkflowsCommand>,
    ICommandHandler<DispatchWorkflowDefinitionCommand>,
    ICommandHandler<DispatchWorkflowInstanceCommand>,
    ICommandHandler<DispatchResumeWorkflowsCommand>
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    public DispatchTenantWorkflowRequestHandler(
        IWorkflowRuntime workflowRuntime,
        ITenantAccessor tenantAccessor,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowInstanceStore workflowInstanceStore)
    {
        _workflowRuntime = workflowRuntime;
        _tenantAccessor = tenantAccessor;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowInstanceStore = workflowInstanceStore;
    }

    public async Task<Unit> HandleAsync(DispatchWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        var options = new StartWorkflowRuntimeOptions(
            command.CorrelationId,
            command.Input,
            command.VersionOptions,
            InstanceId: command.InstanceId,
            TriggerActivityId: command.TriggerActivityId,
            CancellationTokens: cancellationToken);

        string? tenantId = await _workflowDefinitionStore.GetTenantId(command.DefinitionId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _workflowRuntime.TryStartWorkflowAsync(command.DefinitionId, options);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchWorkflowInstanceCommand command, CancellationToken cancellationToken)
    {
        var options = new ResumeWorkflowRuntimeOptions(
            command.CorrelationId,
            command.BookmarkId,
            command.ActivityId,
            command.ActivityNodeId,
            command.ActivityInstanceId,
            command.ActivityHash,
            command.Input,
            cancellationToken);

        var tenantId = await _workflowInstanceStore.GetTenantId(command.InstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _workflowRuntime.ResumeWorkflowAsync(command.InstanceId, options);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchTriggerWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsOptions(
            command.CorrelationId,
            command.WorkflowInstanceId,
            command.ActivityInstanceId,
            command.Input,
            cancellationToken);

        var tenantId = await _workflowInstanceStore.GetTenantId(command.WorkflowInstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _workflowRuntime.TriggerWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options);

        return Unit.Instance;
    }

    public async Task<Unit> HandleAsync(DispatchResumeWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var options = new TriggerWorkflowsOptions(
            correlationId: command.CorrelationId,
            input: command.Input,
            workflowInstanceId: command.WorkflowInstanceId,
            activityInstanceId: command.ActivityInstanceId,
            cancellationTokens: cancellationToken);

        var tenantId = await _workflowInstanceStore.GetTenantId(command.WorkflowInstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await _workflowRuntime.ResumeWorkflowsAsync(command.ActivityTypeName, command.BookmarkPayload, options);

        return Unit.Instance;
    }
}