using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;

namespace Elsa.AI.Host.Context;

public class WorkflowInstanceContextProvider(IServiceProvider serviceProvider, RuntimeGroundingMapper mapper) : IAIContextProvider, IPlaceholderAIContextProvider
{
    public string Kind => AIContextAttachmentKinds.WorkflowInstance;

    public async ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var attachment = request.Attachment;
        var store = serviceProvider.GetService(typeof(IWorkflowInstanceStore)) as IWorkflowInstanceStore;
        if (store == null)
            return Unavailable(attachment);

        var instance = await store.FindAsync(new WorkflowInstanceFilter { Id = attachment.ReferenceId }, cancellationToken);
        if (instance == null || !string.Equals(NormalizeTenant(instance.TenantId), NormalizeTenant(request.TenantId), StringComparison.Ordinal))
            return NotFound(attachment);

        var data = AIGroundingJson.ToJsonObject(mapper.Map(instance));
        data["state"] = mapper.MapState(instance);
        data["incidents"] = AIGroundingJson.ToJsonArray(instance.WorkflowState.Incidents.Select(x => mapper.MapIncident(instance.Id, x)));

        return new AIResolvedContext
        {
            Kind = Kind,
            ReferenceId = attachment.ReferenceId,
            Summary = $"Workflow instance {instance.Id} is {instance.Status}/{instance.SubStatus} with {instance.IncidentCount} incidents.",
            Data = data,
            Metadata = new JsonObject
            {
                ["activityId"] = attachment.ActivityId
            }
        };
    }

    private AIResolvedContext Unavailable(AIContextAttachment attachment) =>
        new() { Kind = Kind, ReferenceId = attachment.ReferenceId, Summary = "Workflow instance store is not available.", Data = [] };

    private AIResolvedContext NotFound(AIContextAttachment attachment) =>
        new() { Kind = Kind, ReferenceId = attachment.ReferenceId, Summary = "Workflow instance was not found or is not authorized.", Data = [] };

    private static string NormalizeTenant(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? "" : tenantId;
}
