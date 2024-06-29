using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Webhooks.Features;

namespace Elsa.OrchardCore.Features;

[DependsOn(typeof(WebhooksFeature))]
public class OrchardCoreFeature(IModule module) : FeatureBase(module)
{
    public override void Configure()
    {
    }
}