using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Permissions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Endpoints.AI.Capabilities;

[PublicAPI]
public class Endpoint(
    IOptions<AIHostOptions> options,
    IEnumerable<IAIProvider> providers,
    IEnumerable<IAIConversationStore> conversationStores,
    IEnumerable<IAIProposalStore> proposalStores) : ElsaEndpointWithoutRequest<Response>
{
    public override void Configure()
    {
        Get("/ai/capabilities");
        ConfigurePermissions(AIPermissions.ViewCapabilities);
    }

    public override Task<Response> ExecuteAsync(CancellationToken cancellationToken)
    {
        var optionsValue = options.Value;
        var providerOptions = optionsValue.Providers.ToList();
        var hasEnabledProvider = providers.Any(x => providerOptions.IsProviderEnabled(x.Name));
        var hasDurableConversationStore = conversationStores.Any(x => x is not IAITransientConversationStore);

        return Task.FromResult(new Response(
            optionsValue.StreamingEnabled && hasEnabledProvider,
            optionsValue.ConversationPersistenceEnabled && hasDurableConversationStore,
            optionsValue.ProposalReviewEnabled && proposalStores.Any(),
            optionsValue.SupportedAttachmentKinds.ToList(),
            optionsValue.Agents.Select(x => new AIAgentCapability(x.Name, x.DisplayName, x.Description)).ToList()));
    }
}

public record Response(
    bool Streaming,
    bool ConversationPersistence,
    bool ProposalReview,
    IReadOnlyCollection<string> SupportedAttachmentKinds,
    IReadOnlyCollection<AIAgentCapability> Agents);

public record AIAgentCapability(string Name, string DisplayName, string Description);
