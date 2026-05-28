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
        var availableProviders = providers.Where(x => providerOptions.IsProviderEnabled(x.Name)).ToList();
        var hasSelectableProvider = HasSelectableProvider(optionsValue.DefaultProviderName, providerOptions, availableProviders);
        var hasDurableConversationStore = conversationStores.Any(x => x is not IAITransientConversationStore);

        return Task.FromResult(new Response(
            optionsValue.StreamingEnabled && hasSelectableProvider,
            optionsValue.ConversationPersistenceEnabled && hasDurableConversationStore,
            optionsValue.ProposalReviewEnabled && proposalStores.Any(),
            optionsValue.SupportedAttachmentKinds.ToList(),
            optionsValue.Agents.Select(x => new AIAgentCapability(x.Name, x.DisplayName, x.Description)).ToList()));
    }

    private static bool HasSelectableProvider(string? providerName, IReadOnlyCollection<AIProviderOptions> providerOptions, IReadOnlyCollection<IAIProvider> availableProviders)
    {
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            var configuredProviders = providerOptions.Where(x => x.Enabled).ToList();
            var configuredProvider = configuredProviders.FirstOrDefault(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));
            return configuredProvider != null
                ? availableProviders.Any(x => string.Equals(x.Name, configuredProvider.Name, StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(x.Name, configuredProvider.Provider, StringComparison.OrdinalIgnoreCase))
                : availableProviders.Any(x => string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));
        }

        return availableProviders.Count == 1;
    }
}

public record Response(
    bool Streaming,
    bool ConversationPersistence,
    bool ProposalReview,
    IReadOnlyCollection<string> SupportedAttachmentKinds,
    IReadOnlyCollection<AIAgentCapability> Agents);

public record AIAgentCapability(string Name, string DisplayName, string Description);
