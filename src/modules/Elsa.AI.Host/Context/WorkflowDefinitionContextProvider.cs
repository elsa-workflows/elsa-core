using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Context;

/// <summary>
/// Placeholder workflow definition context provider. Replace this implementation with a workflow-management-backed resolver before using workflow context for production AI reasoning.
/// </summary>
public class WorkflowDefinitionContextProvider : IAIContextProvider, IPlaceholderAIContextProvider
{
    public string Kind => "WorkflowDefinition";

    public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var attachment = request.Attachment;

        return ValueTask.FromResult(new AIResolvedContext
        {
            Kind = Kind,
            ReferenceId = attachment.ReferenceId,
            Summary = $"Workflow definition context provider is not implemented; only reference {attachment.ReferenceId} was received.",
            Data = [],
            Metadata = new JsonObject
            {
                ["activityId"] = attachment.ActivityId
            }
        });
    }
}
