using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.OrchardCore.ActivityProviders;
using Elsa.OrchardCore.Client.Extensions;
using Elsa.OrchardCore.Options;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Http.Webhooks.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.OrchardCore.Features;

/// <summary>
/// Represents the Orchard Core Feature for integrating with Orchard Core.
/// This feature allows the addition of activities, events, and services related to Orchard Core.
/// </summary>
[DependsOn(typeof(WebhooksFeature))]
[UsedImplicitly]
public class OrchardCoreFeature(IModule module) : FeatureBase(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddVariableTypeAndAlias<ContentItemEventPayload>("ContentItemPublished", "Orchard");
        Module.AddActivitiesFrom<OrchardCoreFeature>();
        Services
            .AddActivityProvider<OrchardContentItemsEventActivityProvider>()
            .AddHandlersFrom<OrchardCoreFeature>()
            ;
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddOrchardCoreClient();
        Services.AddOptions<OrchardCoreOptions>();
    }
}