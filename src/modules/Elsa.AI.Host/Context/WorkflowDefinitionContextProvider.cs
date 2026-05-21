using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Context;

/// <summary>
/// Placeholder workflow definition context provider. Replace this implementation with a workflow-management-backed resolver before using workflow context for production AI reasoning.
/// </summary>
public class WorkflowDefinitionContextProvider : IAiContextProvider
{
    public string Kind => "WorkflowDefinition";

    public ValueTask<AiResolvedContext> ResolveAsync(AiContextResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var attachment = request.Attachment;

        return ValueTask.FromResult(new AiResolvedContext
        {
            Kind = Kind,
            ReferenceId = attachment.ReferenceId,
            Summary = $"Workflow definition context provider is not implemented; only reference {attachment.ReferenceId} was received.",
            Data = new JsonObject
            {
                ["implementationStatus"] = "placeholder",
                ["message"] = "Replace WorkflowDefinitionContextProvider with a workflow-management-backed resolver before production use."
            },
            Metadata = new JsonObject
            {
                ["activityId"] = attachment.ActivityId
            }
        });
    }
}
