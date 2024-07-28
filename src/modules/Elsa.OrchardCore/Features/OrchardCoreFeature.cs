using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.OrchardCore.ActivityProviders;
using Elsa.OrchardCore.Client.Extensions;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Webhooks.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.OrchardCore.Features;

[DependsOn(typeof(WebhooksFeature))]
public class OrchardCoreFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.AddVariableTypeAndAlias<ContentItemEventPayload>("ContentItemPublished", "Orchard");
        Module.AddActivitiesFrom<OrchardCoreFeature>();
        Services
            .AddActivityProvider<OrchardContentItemsEventActivityProvider>()
            .AddHandlersFrom<OrchardCoreFeature>()
            ;
    }

    public override void Apply()
    {
        Services.AddOrchardCoreClient();
        Services.AddOptions<OrchardCoreOptions>();
    }
}