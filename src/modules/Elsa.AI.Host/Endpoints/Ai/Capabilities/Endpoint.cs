using Elsa.Abstractions;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;

namespace Elsa.AI.Host.Endpoints.Ai.Capabilities;

[PublicAPI]
public class Endpoint : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/ai/capabilities");
        ConfigurePermissions(AiPermissions.ViewCapabilities);
    }

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new Response(
            true,
            true,
            true,
            ["WorkflowDefinition", "WorkflowInstance", "ActivitySelection", "DiagnosticsScope", "TimeRange"],
            [new AiAgentCapability("workflow-author", "Workflow author", "Creates safe workflow proposals")]));
    }
}

public record Response(
    bool Streaming,
    bool ConversationPersistence,
    bool ProposalReview,
    IReadOnlyCollection<string> SupportedAttachmentKinds,
    IReadOnlyCollection<AiAgentCapability> Agents);

public record AiAgentCapability(string Name, string DisplayName, string Description);
