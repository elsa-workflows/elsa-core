using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Context;

/// <summary>
/// Placeholder workflow instance context provider. Replace this implementation with a runtime-backed resolver before using instance context for production AI reasoning.
/// </summary>
public class WorkflowInstanceContextProvider : IAIContextProvider, IPlaceholderAIContextProvider
{
    public string Kind => "WorkflowInstance";

    public ValueTask<AIResolvedContext> ResolveAsync(AIContextResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var attachment = request.Attachment;

        return ValueTask.FromResult(new AIResolvedContext
        {
            Kind = Kind,
            ReferenceId = attachment.ReferenceId,
            Summary = $"Workflow instance context provider is not implemented; only reference {attachment.ReferenceId} was received.",
            Data = [],
            Metadata = new JsonObject
            {
                ["activityId"] = attachment.ActivityId
            }
        });
    }
}
