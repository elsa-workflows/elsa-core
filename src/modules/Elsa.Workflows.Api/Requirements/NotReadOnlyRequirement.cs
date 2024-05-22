using Elsa.Common.Models;
using Elsa.Workflows.Api.Options;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Api.Requirements;

public class NotReadOnlyRequirement : IAuthorizationRequirement
{
}


/// <inheritdoc />
[PublicAPI]
public class NotReadOnlyRequirementHandler : AuthorizationHandler<NotReadOnlyRequirement>
{
    private readonly IOptions<ApiOptions> _apiOptions;
    private readonly IWorkflowDefinitionStore _store;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <inheritdoc />
    public NotReadOnlyRequirementHandler(
        IOptions<ApiOptions> apiOptions,
        IWorkflowDefinitionStore store,
        IHttpContextAccessor httpContextAccessor)
    {
        _apiOptions = apiOptions;
        _store = store;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, NotReadOnlyRequirement requirement)
    {
        if (_apiOptions.Value.IsReadOnlyMode)
        {
            context.Fail(new AuthorizationFailureReason(this, "Workflow edit is not allowed when the read-only mode is enabled."));
        }

        var definitionId = _httpContextAccessor.HttpContext?.Request.RouteValues["definitionId"]?.ToString();

        if (definitionId != null)
        {
            var filter = new WorkflowDefinitionFilter
            {
                DefinitionId = definitionId,
                VersionOptions = VersionOptions.Latest
            };

            var definition = await _store.FindAsync(filter, _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None);

            if (definition != null && (definition.IsReadonly || definition.IsSystem))
            {
                context.Fail(new AuthorizationFailureReason(this, "Workflow edit is not allowed for a readonly or system workflow."));
            }
        }

        context.Succeed(requirement);
    }
}