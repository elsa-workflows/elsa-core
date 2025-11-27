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

    /// <inheritdoc />
    public NotReadOnlyRequirementHandler(
        IOptions<ManagementOptions> managementOptions)
    {
        _managementOptions = managementOptions;
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

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}