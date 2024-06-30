using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.OrchardCore.ActivityProviders;
using Elsa.OrchardCore.Options;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Webhooks.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.OrchardCore.Features;

[DependsOn(typeof(WebhooksFeature))]
public class OrchardCoreFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.AddVariableTypeAndAlias<ContentItemPublishedPayload>("ContentItemPublished", "Orchard");
        Services
            .AddActivityProvider<OrchardActivityProvider>()
            .AddHandlersFrom<OrchardCoreFeature>()
            ;
    }

    public override void Apply()
    {
        Services.AddOptions<OrchardOptions>();
    }
}