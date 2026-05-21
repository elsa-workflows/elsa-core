using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Endpoints.Ai.Capabilities;

[PublicAPI]
public class Endpoint(
    IOptions<AiHostOptions> options,
    IEnumerable<IAiProvider> providers,
    IEnumerable<IAiConversationStore> conversationStores,
    IEnumerable<IAiProposalStore> proposalStores) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/ai/capabilities");
        ConfigurePermissions(AiPermissions.ViewCapabilities);
    }

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var optionsValue = options.Value;
        var providerOptions = optionsValue.Providers.ToList();
        var hasEnabledProvider = providers.Any(x => providerOptions.IsProviderEnabled(x.Name));

        return Task.FromResult(new Response(
            optionsValue.StreamingEnabled && hasEnabledProvider,
            optionsValue.ConversationPersistenceEnabled && conversationStores.Any(),
            optionsValue.ProposalReviewEnabled && proposalStores.Any(),
            optionsValue.SupportedAttachmentKinds.ToList(),
            optionsValue.Agents.Select(x => new AiAgentCapability(x.Name, x.DisplayName, x.Description)).ToList()));
    }
}

public record Response(
    bool Streaming,
    bool ConversationPersistence,
    bool ProposalReview,
    IReadOnlyCollection<string> SupportedAttachmentKinds,
    IReadOnlyCollection<AiAgentCapability> Agents);

public record AiAgentCapability(string Name, string DisplayName, string Description);
