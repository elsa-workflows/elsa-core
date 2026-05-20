using System.Diagnostics.CodeAnalysis;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Workflows.Api.RealTime.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

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
    private const string PermissionsClaimType = "permissions";
    private readonly IWorkflowInstanceStore? _workflowInstanceStore;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <inheritdoc />
    [ActivatorUtilitiesConstructor]
    public WorkflowInstanceHub(IWorkflowInstanceStore workflowInstanceStore, ITenantAccessor tenantAccessor)
    {
        _workflowInstanceStore = workflowInstanceStore;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    public WorkflowInstanceHub(IWorkflowRuntime workflowRuntime)
    {
    }
    
    /// <summary>
    /// Observes a workflow instance.
    /// </summary>
    /// <param name="instanceId">The ID of the workflow instance to observe.</param>
    public async Task ObserveInstanceAsync(string instanceId)
    {
        if (!CanReadWorkflowInstances())
            throw new HubException("Access denied.");

        if (!TryGetRequiredServices(out var workflowInstanceStore, out var tenantAccessor))
            throw new HubException("Access denied.");

        var workflowInstance = await workflowInstanceStore.FindAsync(new WorkflowInstanceFilter { Id = instanceId }, Context.ConnectionAborted);

        if (!CanAccessTenant(workflowInstance, tenantAccessor))
            throw new HubException("Access denied.");

        // Join the user to the workflow instance group.
        await Groups.AddToGroupAsync(Context.ConnectionId, instanceId);
    }

    private bool CanReadWorkflowInstances()
    {
        var user = Context.User;

        if (user?.Identity?.IsAuthenticated != true)
            return false;

        return user.FindAll(PermissionsClaimType).Any(x => x.Value is PermissionNames.All or ReadAllPermission or ReadWorkflowInstancesPermission);
    }

    private bool TryGetRequiredServices([NotNullWhen(true)] out IWorkflowInstanceStore? workflowInstanceStore, [NotNullWhen(true)] out ITenantAccessor? tenantAccessor)
    {
        var services = Context.GetHttpContext()?.RequestServices;
        workflowInstanceStore = _workflowInstanceStore ?? services?.GetService<IWorkflowInstanceStore>();
        tenantAccessor = _tenantAccessor ?? services?.GetService<ITenantAccessor>();

        return workflowInstanceStore != null && tenantAccessor != null;
    }

    private static bool CanAccessTenant(WorkflowInstance? workflowInstance, ITenantAccessor tenantAccessor)
    {
        if (workflowInstance == null)
            return false;

        var workflowInstanceTenantId = workflowInstance.TenantId.NormalizeTenantId();

        return workflowInstanceTenantId == Tenant.AgnosticTenantId || workflowInstanceTenantId == tenantAccessor.TenantId;
    }
}
