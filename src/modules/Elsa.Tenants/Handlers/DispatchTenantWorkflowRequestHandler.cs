using Elsa.Common.Contracts;
using Elsa.Mediator.Models;
using Elsa.Tenants.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Commands;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Handlers;

namespace Elsa.Tenants.Handlers;

// ReSharper disable once UnusedType.Global
internal class DispatchTenantWorkflowRequestHandler : DispatchWorkflowRequestHandler
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    public DispatchTenantWorkflowRequestHandler(
        IWorkflowRuntime workflowRuntime,
        ITenantAccessor tenantAccessor,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowInstanceStore workflowInstanceStore) : base(workflowRuntime)
    {
        _tenantAccessor = tenantAccessor;
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowInstanceStore = workflowInstanceStore;
    }

    public override async Task<Unit> HandleAsync(DispatchWorkflowDefinitionCommand command, CancellationToken cancellationToken)
    {
        string? tenantId = await _workflowDefinitionStore.GetTenantId(command.DefinitionId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        return await base.HandleAsync(command, cancellationToken);
    }

    public override async Task<Unit> HandleAsync(DispatchWorkflowInstanceCommand command, CancellationToken cancellationToken)
    {
        var tenantId = await _workflowInstanceStore.GetTenantId(command.InstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        return await base.HandleAsync(command, cancellationToken);
    }

    public override async Task<Unit> HandleAsync(DispatchTriggerWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var tenantId = await _workflowInstanceStore.GetTenantId(command.WorkflowInstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        return await base.HandleAsync(command, cancellationToken);
    }

    public override async Task<Unit> HandleAsync(DispatchResumeWorkflowsCommand command, CancellationToken cancellationToken)
    {
        var tenantId = await _workflowInstanceStore.GetTenantId(command.WorkflowInstanceId, cancellationToken);
        _tenantAccessor.SetCurrentTenantId(tenantId);

        return await base.HandleAsync(command, cancellationToken);
    }
}