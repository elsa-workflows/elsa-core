using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;

namespace Elsa.AI.Host.Context;

public class WorkflowDefinitionContextProvider(IServiceProvider serviceProvider, WorkflowGroundingMapper mapper) : IAIContextProvider, IPlaceholderAIContextProvider
{
    public string Kind => AIContextAttachmentKinds.WorkflowDefinition;

    public async ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var attachment = request.Attachment;
        var store = serviceProvider.GetService(typeof(IWorkflowDefinitionStore)) as IWorkflowDefinitionStore;
        if (store == null)
            return Unavailable(attachment);

        var definition = await store.FindAsync(new WorkflowDefinitionFilter { Id = attachment.ReferenceId }, cancellationToken)
            ?? await store.FindAsync(new WorkflowDefinitionFilter
            {
                DefinitionId = attachment.ReferenceId,
                VersionOptions = Elsa.Common.Models.VersionOptions.Latest
            }, cancellationToken);
        if (definition == null || !string.Equals(NormalizeTenant(definition.TenantId), NormalizeTenant(request.TenantId), StringComparison.Ordinal))
            return NotFound(attachment);

        return new AIResolvedContext
        {
            Kind = Kind,
            ReferenceId = attachment.ReferenceId,
            Summary = $"Workflow definition {definition.DefinitionId} v{definition.Version}: {definition.Name}",
            Data = AIGroundingJson.ToJsonObject(mapper.Map(definition)),
            Metadata = new JsonObject
            {
                ["activityId"] = attachment.ActivityId
            }
        };
    }

    private AIResolvedContext Unavailable(AIContextAttachment attachment) =>
        new() { Kind = Kind, ReferenceId = attachment.ReferenceId, Summary = "Workflow definition store is not available.", Data = [] };

    private AIResolvedContext NotFound(AIContextAttachment attachment) =>
        new() { Kind = Kind, ReferenceId = attachment.ReferenceId, Summary = "Workflow definition was not found or is not authorized.", Data = [] };

    private static string NormalizeTenant(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? "" : tenantId;
}
