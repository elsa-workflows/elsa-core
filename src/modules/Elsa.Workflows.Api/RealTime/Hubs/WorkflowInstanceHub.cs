using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Workflows.Api.RealTime.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using FastEndpoints.Security;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Elsa.Workflows.Api.RealTime.Hubs;

/// <summary>
/// Represents a SignalR hub for receiving workflow events on the client.
/// </summary>
[PublicAPI]
[Authorize]
public class WorkflowInstanceHub : Hub<IWorkflowInstanceClient>
{
    private const string ReadWorkflowInstancesPermission = "read:workflow-instances";
    private const string ReadAllPermission = "read:*";
    private static readonly string[] ReadPermissions = [PermissionNames.All, ReadAllPermission, ReadWorkflowInstancesPermission];
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <inheritdoc />
    public WorkflowInstanceHub(IWorkflowInstanceStore workflowInstanceStore, ITenantAccessor? tenantAccessor = null)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _tenantAccessor = tenantAccessor;
    }
    
    /// <summary>
    /// Observes a workflow instance.
    /// </summary>
    /// <param name="instanceId">The ID of the workflow instance to observe.</param>
    public async Task ObserveInstanceAsync(string instanceId)
    {
        if (!CanReadWorkflowInstances())
            throw new HubException("Access denied.");

        var workflowInstance = await _workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = instanceId }, Context.ConnectionAborted);

        if (!CanAccessTenant(workflowInstance, _tenantAccessor))
            throw new HubException("Access denied.");

        // Join the user to the workflow instance group.
        await Groups.AddToGroupAsync(Context.ConnectionId, instanceId, Context.ConnectionAborted);
    }

    private bool CanReadWorkflowInstances()
    {
        var user = Context.User;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return ReadPermissions.Any(user.HasPermission);
    }

    private static bool CanAccessTenant(WorkflowInstance? workflowInstance, ITenantAccessor? tenantAccessor)
    {
        if (workflowInstance == null)
            return false;

        if (tenantAccessor == null)
            return true;

        var workflowInstanceTenantId = workflowInstance.TenantId.NormalizeTenantId();
        var currentTenantId = tenantAccessor.TenantId.NormalizeTenantId();

        return workflowInstanceTenantId == Tenant.AgnosticTenantId || workflowInstanceTenantId == currentTenantId;
    }
}
