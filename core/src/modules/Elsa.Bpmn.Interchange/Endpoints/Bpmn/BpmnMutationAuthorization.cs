using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

internal static class BpmnMutationAuthorization
{
    public static bool OwnsWorkflowDefinition(this HttpContext context, WorkflowDefinition definition) =>
        TenantVisibility.CanReplace(definition.TenantId, context.RequestServices.GetRequiredService<ITenantAccessor>().TenantId);
}
