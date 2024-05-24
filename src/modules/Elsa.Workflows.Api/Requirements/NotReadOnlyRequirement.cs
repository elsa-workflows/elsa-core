using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Api.Requirements;

public class NotReadOnlyRequirement : IAuthorizationRequirement
{
}


/// <inheritdoc />
[PublicAPI]
public class NotReadOnlyRequirementHandler : AuthorizationHandler<NotReadOnlyRequirement, WorkflowDefinition>
{
    private readonly IOptions<ManagementOptions> _managementOptions;

    /// <inheritdoc />
    public NotReadOnlyRequirementHandler(
        IOptions<ManagementOptions> apiOptions)
    {
        _managementOptions = apiOptions;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotReadOnlyRequirement requirement, WorkflowDefinition resource)
    {
        if (_managementOptions.Value.IsReadOnlyMode)
        {
            context.Fail(new AuthorizationFailureReason(this, "Workflow edit is not allowed when the read-only mode is enabled."));
        }

        if (resource != null && (resource.IsReadonly || resource.IsSystem))
        {
            context.Fail(new AuthorizationFailureReason(this, "Workflow edit is not allowed for a readonly or system workflow."));
        }

        context.Succeed(requirement);
    }
}