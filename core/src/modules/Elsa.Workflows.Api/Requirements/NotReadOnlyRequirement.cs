using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Api.Requirements;

public record NotReadOnlyResource(WorkflowDefinition? WorkflowDefinition = default);


public record NotReadOnlyRequirement() : IAuthorizationRequirement;


/// <inheritdoc />
[PublicAPI]
public class NotReadOnlyRequirementHandler : AuthorizationHandler<NotReadOnlyRequirement, NotReadOnlyResource>
{
    private readonly IOptions<ManagementOptions> _managementOptions;
    private readonly ITenantAccessor? _tenantAccessor;

    /// <inheritdoc />
    public NotReadOnlyRequirementHandler(
        IOptions<ManagementOptions> managementOptions)
        : this(managementOptions, null)
    {
    }

    /// <inheritdoc />
    public NotReadOnlyRequirementHandler(IOptions<ManagementOptions> managementOptions, ITenantAccessor? tenantAccessor)
    {
        _managementOptions = managementOptions;
        _tenantAccessor = tenantAccessor;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, NotReadOnlyRequirement requirement, NotReadOnlyResource resource)
    {
        if (_managementOptions.Value.IsReadOnlyMode)
        {
            context.Fail(new(this, "Workflow edit is not allowed when the read-only mode is enabled."));
        }

        if (resource.WorkflowDefinition != null && (resource.WorkflowDefinition.IsReadonly || resource.WorkflowDefinition.IsSystem))
        {
            context.Fail(new(this, "Workflow edit is not allowed for a readonly or system workflow."));
        }

        if (_tenantAccessor != null && resource.WorkflowDefinition is { } workflow &&
            !TenantVisibility.CanReplace(workflow.TenantId, _tenantAccessor.TenantId))
        {
            context.Fail(new(this, "Workflow edit requires ownership of the workflow definition."));
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
