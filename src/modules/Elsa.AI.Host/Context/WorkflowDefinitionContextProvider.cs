using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Context;

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
            Summary = $"Workflow definition reference {attachment.ReferenceId}",
            Metadata = new JsonObject
            {
                ["activityId"] = attachment.ActivityId
            }
        });
    }
}
