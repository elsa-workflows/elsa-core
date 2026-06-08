using Elsa.Abstractions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Options;
using Elsa.AI.Host.Permissions;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.AI.Host.Endpoints.AI.Capabilities;

[PublicAPI]
public class Endpoint(
    IOptions<AIHostOptions> options,
    IEnumerable<IAIProvider> providers,
    IEnumerable<IAIConversationStore> conversationStores,
    IEnumerable<IAIProposalStore> proposalStores,
    IServiceScopeFactory serviceScopeFactory) : ElsaEndpointWithoutRequest<Response>
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
            optionsValue.Agents.Select(x => new AIAgentCapability(x.Name, x.DisplayName, x.Description)).ToList(),
            CreateGroundingCapabilities(optionsValue, proposalStores.Any())));
    }

    private IReadOnlyCollection<AIGroundingCapabilityDescriptor> CreateGroundingCapabilities(AIHostOptions optionsValue, bool hasProposalStore)
    {
        var grounding = optionsValue.Grounding;
        using var scope = serviceScopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        return
        [
            CreateCapability(
                "activities",
                "Activity discovery",
                grounding.ActivityGroundingEnabled && services.GetService<IActivityRegistry>() != null,
                [CapabilityToolNames.ActivitiesSearchToolName, CapabilityToolNames.ActivityDescriptorToolName],
                [AIContextAttachmentKinds.Activity],
                grounding.ActivityGroundingEnabled,
                "Activity Registry is not registered."),
            CreateCapability(
                "workflows",
                "Workflow definitions",
                grounding.WorkflowGroundingEnabled && services.GetService<IWorkflowDefinitionStore>() != null,
                [CapabilityToolNames.WorkflowsSearchToolName, CapabilityToolNames.WorkflowDefinitionToolName, CapabilityToolNames.WorkflowDefinitionGraphToolName, CapabilityToolNames.WorkflowUsageSearchToolName],
                [AIContextAttachmentKinds.WorkflowDefinition],
                grounding.WorkflowGroundingEnabled,
                "Workflow definition store is not registered."),
            CreateCapability(
                "proposals",
                "Workflow proposals",
                grounding.ProposalGroundingEnabled && hasProposalStore,
                [CapabilityToolNames.WorkflowValidateDraftToolName, CapabilityToolNames.WorkflowProposeCreateToolName, CapabilityToolNames.WorkflowProposeUpdateToolName],
                [AIContextAttachmentKinds.WorkflowDefinition, AIContextAttachmentKinds.Activity],
                grounding.ProposalGroundingEnabled,
                "AI proposal store is not registered."),
            CreateCapability(
                "runtime",
                "Runtime inspection",
                grounding.RuntimeGroundingEnabled && services.GetService<IWorkflowInstanceStore>() != null,
                [CapabilityToolNames.InstancesSearchToolName, CapabilityToolNames.WorkflowInstanceToolName, CapabilityToolNames.WorkflowInstanceExecutionHistoryToolName, CapabilityToolNames.WorkflowInstanceActivityStateToolName, CapabilityToolNames.IncidentsSearchToolName, CapabilityToolNames.IncidentToolName],
                [AIContextAttachmentKinds.WorkflowInstance, AIContextAttachmentKinds.DiagnosticsScope, AIContextAttachmentKinds.TimeRange],
                grounding.RuntimeGroundingEnabled,
                "Workflow instance store is not registered.")
        ];
    }

    private static AIGroundingCapabilityDescriptor CreateCapability(
        string family,
        string displayName,
        bool available,
        IReadOnlyCollection<string> toolNames,
        IReadOnlyCollection<string> attachmentKinds,
        bool enabled,
        string unavailableReason) =>
        new()
        {
            Family = family,
            DisplayName = displayName,
            Available = available,
            ToolNames = toolNames,
            AttachmentKinds = attachmentKinds,
            DisabledReasons = available ? [] : [enabled ? unavailableReason : "Grounding family is disabled by configuration."]
        };

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
    IReadOnlyCollection<AIAgentCapability> Agents,
    IReadOnlyCollection<AIGroundingCapabilityDescriptor> Grounding);

public record AIAgentCapability(string Name, string DisplayName, string Description);

file static class CapabilityToolNames
{
    public const string ActivitiesSearchToolName = "activities.search";
    public const string ActivityDescriptorToolName = "activities.getDescriptor";
    public const string WorkflowsSearchToolName = "workflows.search";
    public const string WorkflowDefinitionToolName = "workflows.getDefinition";
    public const string WorkflowDefinitionGraphToolName = "workflows.getDefinitionGraph";
    public const string WorkflowUsageSearchToolName = "workflows.findUsages";
    public const string WorkflowValidateDraftToolName = "workflows.validateDraft";
    public const string WorkflowProposeCreateToolName = "workflows.proposeCreate";
    public const string WorkflowProposeUpdateToolName = "workflows.proposeUpdate";
    public const string InstancesSearchToolName = "instances.search";
    public const string WorkflowInstanceToolName = "instances.get";
    public const string WorkflowInstanceExecutionHistoryToolName = "instances.getExecutionHistory";
    public const string WorkflowInstanceActivityStateToolName = "instances.getActivityState";
    public const string IncidentsSearchToolName = "incidents.search";
    public const string IncidentToolName = "incidents.get";
}
