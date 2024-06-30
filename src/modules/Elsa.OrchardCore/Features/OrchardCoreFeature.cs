using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.OrchardCore.WebhookPayloads;
using Elsa.Webhooks.Features;

namespace Elsa.OrchardCore.Features;

[DependsOn(typeof(WebhooksFeature))]
public class OrchardCoreFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
        Module.AddVariableTypeAndAlias<ContentItemPublished>("ContentItemPublished", "Orchard");
    }
}